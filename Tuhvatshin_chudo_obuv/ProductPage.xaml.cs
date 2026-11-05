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
        private readonly Users _user;
        private readonly List<OrderItems> _draftItems;
        private bool _isGuest;

        public ProductPage(Users users)
            : this(users, new List<OrderItems>())
        {
        }

        public ProductPage(Users users, List<OrderItems> draftItems)
        {
            InitializeComponent();

            _user = users;
            _draftItems = draftItems ?? new List<OrderItems>();
            _isGuest = users.RoleID == 0;

            FIOTB.Text = users.LastName + " " + users.FirstName + " " + users.MiddleName;

            switch (users.RoleID)
            {
                case 1:
                    RoleTB.Text = "Клиент";
                    break;
                case 2:
                    RoleTB.Text = "Менеджер";
                    break;
                case 3:
                    RoleTB.Text = "Администратор";
                    break;
                default:
                    RoleTB.Text = "Гость";
                    _isGuest = true;
                    break;
            }

            if (_isGuest)
            {
                ComboSort.Visibility = Visibility.Collapsed;
                ComboType.Visibility = Visibility.Collapsed;
                TBSearch.Visibility = Visibility.Collapsed;
            }

            ComboSort.SelectedIndex = 0;
            ComboType.SelectedIndex = 0;
            Update();
        }

        private void Update()
        {
            var products = Tuhvatshin_chudo_obuvEntities.GetContext()
                .Products
                .ToList();

            if (_isGuest)
            {
                ProductListView.ItemsSource = products;
                return;
            }

            if (ComboType.SelectedIndex == 1)
            {
                products = products.Where(p => p.CategoryID == 2).ToList();
            }
            else if (ComboType.SelectedIndex == 2)
            {
                products = products.Where(p => p.CategoryID == 3).ToList();
            }
            else if (ComboType.SelectedIndex == 3)
            {
                products = products.Where(p => p.CategoryID == 1).ToList();
            }

            string search = TBSearch.Text?.Trim().ToLower() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    (p.Name ?? string.Empty).ToLower().Contains(search) ||
                    (p.Manufacturer ?? string.Empty).ToLower().Contains(search) ||
                    (p.SubCategories?.SubcategoryName ?? string.Empty).ToLower().Contains(search) ||
                    (p.Composition ?? string.Empty).ToLower().Contains(search))
                    .ToList();
            }

            if (ComboSort.SelectedIndex == 1)
            {
                products = products.OrderBy(p => p.Price).ToList();
            }
            else if (ComboSort.SelectedIndex == 2)
            {
                products = products.OrderByDescending(p => p.Price).ToList();
            }

            ProductListView.ItemsSource = products;
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Update();
        }

        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            if (_isGuest)
            {
                MessageBox.Show("Для оформления заказа необходимо войти в систему.");
                return;
            }

            var button = sender as FrameworkElement;
            var product = button?.DataContext as Products;

            if (product == null)
            {
                MessageBox.Show("Не удалось определить выбранный товар.");
                return;
            }

            Manager.MainFrame.Navigate(new AddEditPage(_user, product, _draftItems));
        }
    }
}
