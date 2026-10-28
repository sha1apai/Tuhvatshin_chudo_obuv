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
        private bool _isGuest;

        public ProductPage(Users users)
        {
            InitializeComponent();
            _isGuest = users.RoleID == 0;

            if (_isGuest)
            {
                ComboSort.Visibility = Visibility.Collapsed;
                ComboType.Visibility = Visibility.Collapsed;
                TBSearch.Visibility = Visibility.Collapsed;
            }

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
            var currentProducts = Tuhvatshin_chudo_obuvEntities.GetContext().Products.ToList();
            ProductListView.ItemsSource = currentProducts;

            ComboSort.SelectedIndex = 0;
            ComboType.SelectedIndex = 0;

            Update();
        }
        private void Update()
        {
            var currentProducts = Tuhvatshin_chudo_obuvEntities.GetContext().Products.ToList();
            if (ComboType.SelectedIndex == 1)
            {
                currentProducts = currentProducts.Where(p => p.CategoryID == 2).ToList();
            }
            else if (ComboType.SelectedIndex == 2)
            {
                currentProducts = currentProducts.Where(p => p.CategoryID == 3).ToList();
            }
            else if (ComboType.SelectedIndex == 3)
            {
                currentProducts = currentProducts.Where(p => p.CategoryID == 1).ToList();
            }

            currentProducts = currentProducts.Where(p => p.Name.ToLower().Contains(TBSearch.Text.ToLower())
            || p.Manufacturer.ToLower().Contains(TBSearch.Text.ToLower())
            || p.SubCategories.SubcategoryName.ToLower().Contains(TBSearch.Text.ToLower())
            || p.Composition.ToLower().Contains(TBSearch.Text.ToLower())).ToList();


            if (ComboSort.SelectedIndex == 1)
            {
                currentProducts = currentProducts.OrderBy(p => p.Price).ToList();
            }
            if (ComboSort.SelectedIndex == 2)
            {
                currentProducts = currentProducts.OrderByDescending(p => p.Price).ToList();
            }
            ProductListView.ItemsSource = currentProducts.ToList();
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
    }


}
