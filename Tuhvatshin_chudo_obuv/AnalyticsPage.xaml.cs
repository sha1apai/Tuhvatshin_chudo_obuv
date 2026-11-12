using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Tuhvatshin_chudo_obuv
{
    public partial class AnalyticsPage : Page
    {
        private readonly bool _isAdmin;
        private bool _initializing;
        private DateTime _reportStart;
        private DateTime _reportEnd;
        private int _ordersCount;
        private decimal _salesTotal;
        private List<TopProductRow> _topProducts = new List<TopProductRow>();

        public AnalyticsPage(Users user)
        {
            InitializeComponent();

            _isAdmin = user != null && user.RoleID == 3;
            if (!_isAdmin)
            {
                IsEnabled = false;
                MessageBox.Show(
                    "Раздел доступен только администратору.",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _initializing = true;

            var today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;

            _initializing = false;
            RefreshReport();
        }

        private void Period_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_initializing && _isAdmin)
            {
                RefreshReport();
            }
        }

        private void RefreshReport()
        {
            if (!StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue)
            {
                ShowError("Укажите обе даты периода.");
                return;
            }

            var start = StartDatePicker.SelectedDate.Value.Date;
            var end = EndDatePicker.SelectedDate.Value.Date;

            if (start > end)
            {
                ShowError("Дата начала не может быть позже даты окончания.");
                return;
            }

            var endExclusive = end.AddDays(1);

            try
            {
                var context = Tuhvatshin_chudo_obuvEntities.GetContext();

                var orders = context.Orders
                    .AsNoTracking()
                    .Where(order => order.OrderDate >= start &&
                                    order.OrderDate < endExclusive);

                _ordersCount = orders.Count();

                var orderItems = (from order in orders
                                  join item in context.OrderItems.AsNoTracking()
                                      on order.OrderID equals item.OrderID
                                  where item.Quantity > 0
                                  select new
                                  {
                                      item.Quantity,
                                      item.UnitPrice,
                                      Product = item.ProductItems.Products
                                  }).ToList();

                _salesTotal = orderItems.Sum(item => item.UnitPrice * item.Quantity);

                _topProducts = orderItems
                    .Where(item => item.Product != null)
                    .GroupBy(item => new
                    {
                        item.Product.ProductID,
                        item.Product.Name,
                        item.Product.Manufacturer
                    })
                    .Select(group => new TopProductRow
                    {
                        Name = group.Key.Name,
                        Manufacturer = group.Key.Manufacturer,
                        Quantity = group.Sum(item => item.Quantity)
                    })
                    .OrderByDescending(product => product.Quantity)
                    .ThenBy(product => product.Name)
                    .Take(5)
                    .ToList();

                _reportStart = start;
                _reportEnd = end;

                ErrorText.Text = string.Empty;
                OrdersSummary.Text = "Количество заказов: " + _ordersCount;
                SalesSummary.Text = "Сумма продаж: " +
                    _salesTotal.ToString("N2") + " руб.";
                TopProductsGrid.ItemsSource = _topProducts;
            }
            catch (Exception exception)
            {
                ShowError("Ошибка БД: " + exception.GetBaseException().Message);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            OrdersSummary.Text = string.Empty;
            SalesSummary.Text = string.Empty;
            TopProductsGrid.ItemsSource = null;
            _topProducts = new List<TopProductRow>();
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show(
                    "Экспорт доступен только администратору.",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue ||
                StartDatePicker.SelectedDate.Value.Date >
                EndDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show(
                    "Проверьте выбранный период.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(ErrorText.Text))
            {
                MessageBox.Show(
                    "Сначала исправьте ошибку загрузки аналитики.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PDF-файл (*.pdf)|*.pdf",
                FileName = "Отчет_продаж.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {

                var document = new PdfDocument();
                document.Info.Title = "Отчет по продажам";

                var page = document.AddPage();
                var graphics = XGraphics.FromPdfPage(page);
                var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);

                var regularFont = new XFont("Arial", 11, XFontStyle.Regular, fontOptions);
                var boldFont = new XFont("Arial", 16, XFontStyle.Bold, fontOptions);
                var headerFont = new XFont("Arial", 11, XFontStyle.Bold, fontOptions);

                var left = 40.0;
                var y = 45.0;

                graphics.DrawString(
                    "Отчет по продажам",
                    boldFont,
                    XBrushes.Black,
                    new XPoint(left, y));
                y += 30;

                graphics.DrawString(
                    "Период: " + _reportStart.ToString("dd.MM.yyyy") +
                    " — " + _reportEnd.ToString("dd.MM.yyyy"),
                    regularFont,
                    XBrushes.Black,
                    new XPoint(left, y));
                y += 22;

                graphics.DrawString(
                    "Количество заказов: " + _ordersCount,
                    regularFont,
                    XBrushes.Black,
                    new XPoint(left, y));
                y += 22;

                graphics.DrawString(
                    "Сумма продаж: " + _salesTotal.ToString("N2") + " руб.",
                    regularFont,
                    XBrushes.Black,
                    new XPoint(left, y));
                y += 32;

                graphics.DrawString(
                    "Топ-5 моделей по количеству проданных пар:",
                    boldFont,
                    XBrushes.Black,
                    new XPoint(left, y));
                y += 28;

                if (_topProducts.Count == 0)
                {
                    graphics.DrawString(
                        "За выбранный период продаж нет.",
                        regularFont,
                        XBrushes.Black,
                        new XPoint(left, y));
                }
                else
                {
                    graphics.DrawString(
                        "Модель",
                        headerFont,
                        XBrushes.Black,
                        new XPoint(left, y));
                    graphics.DrawString(
                        "Производитель",
                        headerFont,
                        XBrushes.Black,
                        new XPoint(300, y));
                    graphics.DrawString(
                        "Продано пар",
                        headerFont,
                        XBrushes.Black,
                        new XPoint(470, y));
                    y += 20;

                    foreach (var product in _topProducts)
                    {
                        graphics.DrawString(
                            FitText(graphics, regularFont, product.Name, 250),
                            regularFont,
                            XBrushes.Black,
                            new XPoint(left, y));

                        graphics.DrawString(
                            FitText(graphics, regularFont, product.Manufacturer, 160),
                            regularFont,
                            XBrushes.Black,
                            new XPoint(300, y));

                        graphics.DrawString(
                            product.Quantity.ToString(),
                            regularFont,
                            XBrushes.Black,
                            new XPoint(470, y));

                        y += 24;
                    }
                }

                graphics.Dispose();
                document.Save(dialog.FileName);
                document.Close();

                MessageBox.Show(
                    "PDF-отчет сохранен:\n" + dialog.FileName,
                    "Готово",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Не удалось создать PDF: " + exception.GetBaseException().Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string FitText(
    XGraphics graphics,
    XFont font,
    string value,
    double maxWidth)
{
    var text = value ?? string.Empty;
    const string ellipsis = "…";

    if (graphics.MeasureString(text, font).Width <= maxWidth)
    {
        return text;
    }

    while (text.Length > 0 &&
           graphics.MeasureString(text + ellipsis, font).Width > maxWidth)
    {
        text = text.Substring(0, text.Length - 1);
    }

    return text.TrimEnd() + ellipsis;
}

        private sealed class TopProductRow
        {
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public int Quantity { get; set; }
        }
    }
}
