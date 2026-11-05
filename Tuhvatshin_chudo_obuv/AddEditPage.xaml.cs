using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace Tuhvatshin_chudo_obuv
{
    /// <summary>
    /// Логика взаимодействия для AddEditPage.xaml
    /// </summary>
    public partial class AddEditPage : Page
    {
        private readonly Users _user;
        private readonly Products _product;
        private readonly List<OrderItems> _draftItems;

        public AddEditPage()
        {
            InitializeComponent();
        }

        public AddEditPage(Users user, Products product, List<OrderItems> draftItems)
        {
            InitializeComponent();

            _user = user;
            _product = product;
            _draftItems = draftItems ?? new List<OrderItems>();

            DataContext = _product;
            CartListView.ItemsSource = _draftItems;

            var context = Tuhvatshin_chudo_obuvEntities.GetContext();
            var availableSizes = context.ProductItems
                .Where(item => item.ProductID == _product.ProductID && item.Quantity > 0)
                .ToList();

            ComboSize.ItemsSource = availableSizes;

            if (availableSizes.Count > 0)
            {
                ComboSize.SelectedIndex = 0;
            }
            else
            {
                AddItemButton.IsEnabled = false;
                MessageBox.Show("Для этого товара нет доступных размеров.");
            }

            if (_user.RoleID == 0)
            {
                AddItemButton.IsEnabled = false;
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null || _user.RoleID == 0)
            {
                MessageBox.Show("Для оформления заказа необходимо войти в систему.");
                return;
            }

            var selectedStock = ComboSize.SelectedItem as ProductItems;

            if (selectedStock == null)
            {
                MessageBox.Show("Выберите доступный размер.");
                return;
            }

            int quantity;
            if (!int.TryParse(QuantityTextBox.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество.");
                return;
            }

            int alreadyAdded = _draftItems
                .Where(item => item.ProductItemID == selectedStock.ProductItemID)
                .Sum(item => item.Quantity);

            if (quantity + alreadyAdded > selectedStock.Quantity)
            {
                MessageBox.Show("Запрошенное количество превышает остаток выбранного размера.");
                return;
            }

            var cartItem = _draftItems.FirstOrDefault(
                item => item.ProductItemID == selectedStock.ProductItemID);

            if (cartItem == null)
            {
                cartItem = new OrderItems
                {
                    ProductItemID = selectedStock.ProductItemID,
                    ProductItems = selectedStock,
                    Quantity = quantity,
                    UnitPrice = _product.Price,
                    TotalPrice = _product.Price * quantity
                };

                _draftItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
                cartItem.TotalPrice = cartItem.UnitPrice * cartItem.Quantity;
            }

            RefreshCart();
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CartListView.SelectedItem as OrderItems;

            if (selectedItem == null)
            {
                MessageBox.Show("Выберите позицию для удаления.");
                return;
            }

            _draftItems.Remove(selectedItem);
            RefreshCart();
        }

        private void ChooseMoreButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new ProductPage(_user, _draftItems));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new ProductPage(_user, _draftItems));
        }

        private void ConfirmOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null || _user.RoleID == 0)
            {
                MessageBox.Show("Для оформления заказа необходимо войти в систему.");
                return;
            }

            if (_draftItems.Count == 0)
            {
                MessageBox.Show("Добавьте в заказ хотя бы одну позицию.");
                return;
            }

            var context = Tuhvatshin_chudo_obuvEntities.GetContext();
            var changedStocks = new List<ProductItems>();
            var addedItems = new List<OrderItems>();
            Orders order = null;

            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var stocks = new Dictionary<int, ProductItems>();

                    foreach (var draftItem in _draftItems)
                    {
                        var stock = context.ProductItems
                            .SingleOrDefault(item => item.ProductItemID == draftItem.ProductItemID);

                        if (stock != null)
                        {
                            context.Entry(stock).Reload();
                        }

                        if (stock == null || stock.Quantity < draftItem.Quantity)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Остаток одного из товаров изменился. Проверьте заказ.");
                            return;
                        }

                        stocks[draftItem.ProductItemID] = stock;
                    }

                    int orderNumber = context.Orders.Any()
                        ? context.Orders.Max(item => item.OrderNumber) + 1
                        : 1;

                    order = new Orders
                    {
                        OrderNumber = orderNumber,
                        OrderDate = DateTime.Now,
                        UserID = _user.UserID
                    };

                    context.Orders.Add(order);

                    foreach (var draftItem in _draftItems)
                    {
                        var stock = stocks[draftItem.ProductItemID];
                        stock.Quantity -= draftItem.Quantity;
                        changedStocks.Add(stock);

                        var orderItem = new OrderItems
                        {
                            Orders = order,
                            ProductItems = stock,
                            ProductItemID = stock.ProductItemID,
                            Quantity = draftItem.Quantity,
                            UnitPrice = draftItem.UnitPrice,
                            TotalPrice = draftItem.UnitPrice * draftItem.Quantity
                        };

                        context.OrderItems.Add(orderItem);
                        addedItems.Add(orderItem);
                    }

                    context.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show("Заказ оформлен.");
                    Manager.MainFrame.Navigate(new ProductPage(_user, new List<OrderItems>()));
                }
                catch (Exception exception)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Транзакция могла завершиться до обработки исключения.
                    }

                    foreach (var item in addedItems)
                    {
                        if (context.Entry(item).State != EntityState.Detached)
                        {
                            context.Entry(item).State = EntityState.Detached;
                        }
                    }

                    if (order != null && context.Entry(order).State != EntityState.Detached)
                    {
                        context.Entry(order).State = EntityState.Detached;
                    }

                    foreach (var stock in changedStocks)
                    {
                        context.Entry(stock).Reload();
                    }

                    MessageBox.Show("Не удалось сохранить заказ: " + exception.Message);
                }
            }
        }

        private void RefreshCart()
        {
            CartListView.ItemsSource = null;
            CartListView.ItemsSource = _draftItems;
        }

        private void ComboSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
