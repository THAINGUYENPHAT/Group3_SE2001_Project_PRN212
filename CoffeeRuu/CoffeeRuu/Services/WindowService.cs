
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

        public void OpenMainWindowAfterLogin()
        {
            var home = new MainShellWindow();

            Application.Current.MainWindow = home;

            home.Show();

            _currentWindow.Close();
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

            // Xóa thông tin phiên đăng nhập cũ.
            UserSession.End();

            var loginWindow = new MainWindow();

            Application.Current.MainWindow = loginWindow;

            loginWindow.Show();

            _currentWindow.Close();
        }
    }
}
