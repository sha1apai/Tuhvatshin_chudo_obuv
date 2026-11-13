using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Tuhvatshin_chudo_obuv
{
    public partial class OrderPage : Page
    {
        private readonly Users _user;
        private readonly bool _isAdmin;
        private readonly List<OrderLineRow> _currentLines = new List<OrderLineRow>();
        private Orders _selectedOrder;

        public OrderPage(Users user)
        {
            InitializeComponent();

            _user = user;
            _isAdmin = user != null && user.RoleID == 3;

            if (user == null || (user.RoleID != 2 && user.RoleID != 3))
            {
                MessageBox.Show("У вас нет доступа к списку заказов.");
                Manager.MainFrame.Navigate(new AuthPage());
                return;
            }

            OrderDatePicker.SelectedDate = DateTime.Today;
            RefreshOrders(null);
            UpdateEditorState();
        }

        private void RefreshOrders(int? selectedOrderId)
        {
            var context = Tuhvatshin_chudo_obuvEntities.GetContext();

            var orders = context.Orders
                .Include("Users")
                .Include("OrderItems.ProductItems.Products")
                .Include("OrderItems.ProductItems.Sizes")
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderNumber)
                .ToList();

            var rows = orders.Select(order => new OrderRow
            {
                Order = order,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerName = GetFullName(order.Users),
                Total = order.OrderItems.Sum(item => item.UnitPrice * item.Quantity)
            }).ToList();

            OrdersGrid.ItemsSource = rows;

            if (selectedOrderId.HasValue)
            {
                var selectedRow = rows.FirstOrDefault(
                    row => row.Order.OrderID == selectedOrderId.Value);

                if (selectedRow != null)
                {
                    OrdersGrid.SelectedItem = selectedRow;
                    return;
                }
            }

            OrdersGrid.SelectedItem = null;
            _selectedOrder = null;
            _currentLines.Clear();
            OrderItemsGrid.ItemsSource = null;
            OrderDatePicker.SelectedDate = DateTime.Today;
            TotalTextBlock.Text = "Итого: 0,00 ₽";
            UpdateEditorState();
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null)
            {
                return;
            }

            _selectedOrder = row.Order;
            OrderDatePicker.SelectedDate = _selectedOrder.OrderDate;

            _currentLines.Clear();
            foreach (var item in _selectedOrder.OrderItems)
            {
                _currentLines.Add(CreateLineRow(item));
            }

            RefreshOrderItems();
            UpdateEditorState();
        }

        private void SaveOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Сначала выберите заказ.");
                return;
            }

            OrderItemsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            OrderItemsGrid.CommitEdit(DataGridEditingUnit.Row, true);

            SaveExistingOrder();
        }

        private void SaveExistingOrder()
        {
            if (_currentLines.Any(line => line.Quantity <= 0))
            {
                MessageBox.Show("Количество должно быть больше нуля.");
                return;
            }

            var context = Tuhvatshin_chudo_obuvEntities.GetContext();
            var orderId = _selectedOrder.OrderID;

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var order = context.Orders
                        .Include("OrderItems.ProductItems")
                        .SingleOrDefault(item => item.OrderID == orderId);

                    if (order == null)
                    {
                        transaction.Rollback();
                        RefreshOrders(null);
                        return;
                    }

                    var currentItemIds = new HashSet<int>(
                        _currentLines
                            .Where(line => line.Item.OrderItemID != 0)
                            .Select(line => line.Item.OrderItemID));

                    var removedItems = order.OrderItems
                        .Where(item => !currentItemIds.Contains(item.OrderItemID))
                        .ToList();

                    var addedLines = _currentLines
                        .Where(line => line.Item.OrderItemID == 0)
                        .ToList();

                    var modifiedLines = new List<KeyValuePair<OrderLineRow, OrderItems>>();
                    var stockDeltas = new Dictionary<int, int>();

                    foreach (var item in removedItems)
                    {
                        AddStockDelta(stockDeltas, item.ProductItemID, item.Quantity);
                    }

                    foreach (var line in _currentLines.Where(item => item.Item.OrderItemID != 0))
                    {
                        var savedItem = order.OrderItems
                            .SingleOrDefault(item => item.OrderItemID == line.Item.OrderItemID);

                        if (savedItem == null || line.Quantity == savedItem.Quantity)
                        {
                            continue;
                        }

                        modifiedLines.Add(
                            new KeyValuePair<OrderLineRow, OrderItems>(line, savedItem));

                        AddStockDelta(
                            stockDeltas,
                            savedItem.ProductItemID,
                            savedItem.Quantity - line.Quantity);
                    }

                    foreach (var line in addedLines)
                    {
                        AddStockDelta(stockDeltas, line.Item.ProductItemID, -line.Quantity);
                    }

                    var stocks = new Dictionary<int, ProductItems>();

                    foreach (var stockChange in stockDeltas)
                    {
                        var stock = context.ProductItems
                            .SingleOrDefault(item => item.ProductItemID == stockChange.Key);

                        if (stock == null)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Не удалось найти товар на складе.");
                            return;
                        }

                        if (stock.Quantity + stockChange.Value < 0)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Недостаточно товара на складе для указанного количества.");
                            return;
                        }

                        stocks.Add(stockChange.Key, stock);
                    }

                    foreach (var stockChange in stockDeltas)
                    {
                        stocks[stockChange.Key].Quantity += stockChange.Value;
                    }

                    foreach (var item in removedItems)
                    {
                        context.OrderItems.Remove(item);
                    }

                    foreach (var modifiedLine in modifiedLines)
                    {
                        modifiedLine.Value.Quantity = modifiedLine.Key.Quantity;
                        modifiedLine.Value.TotalPrice =
                            modifiedLine.Value.UnitPrice * modifiedLine.Key.Quantity;
                    }

                    foreach (var line in addedLines)
                    {
                        context.OrderItems.Add(new OrderItems
                        {
                            OrderID = order.OrderID,
                            ProductItemID = line.Item.ProductItemID,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            TotalPrice = line.UnitPrice * line.Quantity
                        });
                    }

                    if (_isAdmin && OrderDatePicker.SelectedDate.HasValue)
                    {
                        order.OrderDate = OrderDatePicker.SelectedDate.Value;
                    }

                    context.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show("Изменения заказа сохранены.");
                    RefreshOrders(orderId);
                }
                catch (Exception exception)
                {
                    transaction.Rollback();
                    MessageBox.Show("Не удалось сохранить заказ: " + exception.Message);
                }
            }
        }

        private static void AddStockDelta(
            Dictionary<int, int> stockDeltas,
            int productItemId,
            int quantityDelta)
        {
            if (stockDeltas.ContainsKey(productItemId))
            {
                stockDeltas[productItemId] += quantityDelta;
            }
            else
            {
                stockDeltas.Add(productItemId, quantityDelta);
            }
        }

        private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                return;
            }

            var selectedLine = OrderItemsGrid.SelectedItem as OrderLineRow;
            if (selectedLine == null)
            {
                MessageBox.Show("Выберите товарную позицию для удаления.");
                return;
            }

            _currentLines.Remove(selectedLine);
            RefreshOrderItems();
            UpdateEditorState();
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                return;
            }

            if (MessageBox.Show(
                    "Удалить выбранный заказ?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var orderId = _selectedOrder.OrderID;
            var context = Tuhvatshin_chudo_obuvEntities.GetContext();

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var order = context.Orders
                        .Include("OrderItems.ProductItems")
                        .SingleOrDefault(item => item.OrderID == orderId);

                    if (order == null)
                    {
                        transaction.Rollback();
                        RefreshOrders(null);
                        return;
                    }

                    foreach (var item in order.OrderItems.ToList())
                    {
                        item.ProductItems.Quantity += item.Quantity;
                        context.OrderItems.Remove(item);
                    }

                    context.Orders.Remove(order);
                    context.SaveChanges();
                    transaction.Commit();

                    RefreshOrders(null);
                }
                catch (Exception exception)
                {
                    transaction.Rollback();
                    MessageBox.Show("Не удалось удалить заказ: " + exception.Message);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new ProductPage(_user));
        }

        private void RefreshOrderItems()
        {
            OrderItemsGrid.ItemsSource = null;
            OrderItemsGrid.ItemsSource = _currentLines;
            TotalTextBlock.Text = "Итого: " +
                _currentLines.Sum(line => line.TotalPrice).ToString("N2") + " ₽";
        }

        private void UpdateEditorState()
        {
            OrderEditorPanel.IsEnabled = _selectedOrder != null;
            SaveOrderButton.Visibility = _selectedOrder != null
                ? Visibility.Visible
                : Visibility.Collapsed;
            DeleteOrderButton.Visibility = _isAdmin && _selectedOrder != null
                ? Visibility.Visible
                : Visibility.Collapsed;
            DeleteItemButton.Visibility = _isAdmin && _currentLines.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            OrderDatePicker.IsEnabled = _isAdmin && _selectedOrder != null;
        }

        private void OrderItemsGrid_CellEditEnding(
            object sender,
            DataGridCellEditEndingEventArgs e)
        {
            var editedLine = e.Row.Item as OrderLineRow;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (editedLine == null)
                {
                    return;
                }

                TotalTextBlock.Text = "Итого: " +
                    _currentLines.Sum(line => line.TotalPrice).ToString("N2") + " ₽";
            }));
        }

        private static OrderLineRow CreateLineRow(OrderItems item)
        {
            var product = item.ProductItems == null ? null : item.ProductItems.Products;
            var size = item.ProductItems == null || item.ProductItems.Sizes == null
                ? 0
                : item.ProductItems.Sizes.SizeValue;

            return new OrderLineRow
            {
                Item = item,
                ProductName = product == null
                    ? "Товар не найден"
                    : (product.Name ?? string.Empty) + " — " + (product.Manufacturer ?? string.Empty),
                Size = size,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
        }

        private static string GetFullName(Users user)
        {
            if (user == null)
            {
                return string.Empty;
            }

            return string.Join(" ", new[]
            {
                user.LastName,
                user.FirstName,
                user.MiddleName
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private sealed class OrderRow
        {
            public Orders Order { get; set; }
            public int OrderNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public string CustomerName { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class OrderLineRow : INotifyPropertyChanged
        {
            private int _quantity;

            public event PropertyChangedEventHandler PropertyChanged;

            public OrderItems Item { get; set; }
            public string ProductName { get; set; }
            public decimal Size { get; set; }
            public decimal UnitPrice { get; set; }

            public int Quantity
            {
                get { return _quantity; }
                set
                {
                    if (_quantity == value)
                    {
                        return;
                    }

                    _quantity = value;
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(Quantity)));
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(TotalPrice)));
                }
            }

            public decimal TotalPrice
            {
                get { return UnitPrice * Quantity; }
            }
        }
    }
}