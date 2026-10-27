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
        private void Update()
        {
            var currentProducts = Tuhvatshin_chudo_obuvEntities.GetContext().Products.ToList();
            currentProducts = currentProducts.Where(p => p.Name.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList(); //Tbox это само поле ввода, а text это переменная которая хранит то что написано внутри

            ProductListView.ItemsSource = currentProducts.ToList();

            if (ComboSort.SelectedIndex == 1)
                currentProducts = currentProducts.OrderBy(p => p.Name).ToList(); //сортировка по возрастанию
            if (ComboSort.SelectedIndex == 2)
                currentProducts = currentProducts.OrderByDescending(p => p.Name).ToList(); //сортировка по убыванию

            ProductListView.ItemsSource = currentProducts;
        }
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Update();
        }
    }


}
