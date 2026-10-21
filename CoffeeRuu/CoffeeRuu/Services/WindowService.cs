
using System.Windows;
using CoffeeRuu.Views;

namespace CoffeeRuu.Services
{
    public class WindowService
    {
        private readonly Window _currentWindow;

        public WindowService(Window currentWindow)
        {
            _currentWindow = currentWindow;
        }

        public void Logout()
        {
            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            LoginView loginView = new LoginView();

            Application.Current.MainWindow = loginView;

            loginView.Show();

            _currentWindow.Close();
        }
    }
}
