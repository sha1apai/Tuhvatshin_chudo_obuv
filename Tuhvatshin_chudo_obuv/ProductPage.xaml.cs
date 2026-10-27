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

        public ProductPage(Users users)
        {
            InitializeComponent();
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
                    break;
            }
            var currentProducts = Tuhvatshin_chudo_obuvEntities.GetContext().Products.ToList();
            ProductListView.ItemsSource = currentProducts;

            ComboSort.SelectedIndex = 0;

            Update();
        }
        private void Update()
        {
            var currentProducts = Tuhvatshin_chudo_obuvEntities.GetContext().Products.ToList();
            currentProducts = currentProducts.Where(p => p.Name.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList(); 

            ProductListView.ItemsSource = currentProducts.ToList();

            if (ComboSort.SelectedIndex == 1)
                currentProducts = currentProducts.OrderBy(p => p.Name).ToList();
            if (ComboSort.SelectedIndex == 2)
                currentProducts = currentProducts.OrderByDescending(p => p.Name).ToList();

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
