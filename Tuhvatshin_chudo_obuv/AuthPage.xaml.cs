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
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private int failedAttempts = 0;
        public AuthPage()
        {
            InitializeComponent();
        }
        private void GuestAuthButton_Click(object sender, RoutedEventArgs e)
        {
            Users guestUser = new Users
            {
                UserID = 0,
                Login = "guest",
                FirstName = "Гость",
                LastName = "",
                MiddleName = "",
                RoleID = 0
            };

            LoginTextBox.Text = "";
            Manager.MainFrame.Navigate(new ProductPage(guestUser));
        }

        private void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Поиск пользователя в БД
            Users user = Tuhvatshin_chudo_obuvEntities.GetContext().Users
                .FirstOrDefault(p => p.Login == LoginTextBox.Text);

            if (user != null)
            {
                // Успешный вход
                LoginTextBox.Text = "";
                Manager.MainFrame.Navigate(new ProductPage(user));
            }
            else
            {
                // Неверный логин или пароль
                failedAttempts++;

                if (failedAttempts == 1)
                {
                    // Первая неудачная попытка — показываем капчу
                    MessageBox.Show("Введены неверные данные!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                LoginTextBox.Text = "";
            }
        }

        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
