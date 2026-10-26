using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tuhvatshin_chudo_obuv
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {

        public ProductPage()
        {
            InitializeComponent();
            var context = Tuhvatshin_chudo_obuvEntities.GetContext();
            var products = context.Products.ToList();

            var productViewModels = products.Select(product => new
            {
                Product = product,
                QuantityStatus = product.ProductItems.Sum(p => p.Quantity) > 5 ? "много" : "мало"
            }).ToList();

            ProductListView.ItemsSource = productViewModels;
            Update();
        }
        public void Update()
        {
            var context = Tuhvatshin_chudo_obuvEntities.GetContext();
            var products = context.Products.ToList();
            if (CategoryCombo.SelectedIndex > 0)
            {
                var selectedCategory = (string)CategoryCombo.SelectedItem;
                products = products.Where(p => p.Categories.CategoryName == selectedCategory).ToList();
            }

            // Фильтрация по производителю (пример: ManufacturerCombo)
            if (ManufacturerCombo.SelectedIndex > 0)
            {
                var selectedManufacturer = (string)ManufacturerCombo.SelectedItem;
                products = products.Where(p => p.Manufacturer == selectedManufacturer).ToList();
            }

            // Поиск по названию или составу
            string search = TBoxSearch.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Composition.ToLower().Contains(search)
                ).ToList();
            }

            // Сортировка
            if (SortCombo.SelectedIndex == 1)
                products = products.OrderBy(p => p.Name).ToList();
            if (SortCombo.SelectedIndex == 2)
                products = products.OrderByDescending(p => p.Price).ToList();

            // Формируем список для отображения (с "много"/"мало")
            var productViewModels = products.Select(product => new
            {
                Product = product,
                QuantityStatus = product.ProductItems.Sum(pi => pi.Quantity) > 5 ? "много" : "мало"
            }).ToList();

            ProductListView.ItemsSource = productViewModels;
        }
        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Update();
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }


        private void ManufacturerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }
    }


}
