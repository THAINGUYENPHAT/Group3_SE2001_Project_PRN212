
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
                // 1. Khởi tạo cửa sổ Đăng nhập (Thay LoginWindow bằng tên file Cửa sổ Login của bạn)
                LoginView login = new LoginView();

                // 2. Gán làm cửa sổ chính mới của ứng dụng
                Application.Current.MainWindow = login;

                // 3. Hiển thị cửa sổ Đăng nhập
                login.Show();

                // 4. Đóng cửa sổ MainWindow hiện tại
                this.Close();
            }
        }   

        // Chuyển về trang chủ
        private void BtnHome_Click(
            object sender, RoutedEventArgs e)
        {
            HomeContent.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
        }

        // Chuyển sang quản lý kho

        private void BtnInventory_Click(
          object sender, RoutedEventArgs e)
        {
            if (MainContent.Content == null)
            {
                MainContent.Navigate(new InventoryView());
            }
            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

    }
}
