
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

            // Mo lai man hinh dang nhap
            MainWindow loginWindow = new MainWindow();

            Application.Current.MainWindow = loginWindow;

            loginWindow.Show();

            // Dong cua so chinh hien tai
            _currentWindow.Close();
        }
    }
}
