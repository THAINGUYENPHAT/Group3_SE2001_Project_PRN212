using System.Windows;
using CoffeeRuu.Views;

namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginView login = new LoginView();
                Application.Current.MainWindow = login;
                login.Show();
                this.Close();
            }
        }

        // Chuyển về trang chủ
        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            HomeContent.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
        }

        // Chuyển sang quản lý kho (Đã sửa lại logic kiểm tra)
        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is InventoryView))
            {
                MainContent.Navigate(new InventoryView());
            }
            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

        // Chuyển sang quản lý Bàn
        private void BtnTable_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is TableView))
            {
                MainContent.Navigate(new TableView());
            }
            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }
    }
}