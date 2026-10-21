using System.Windows;
using CoffeeRuu.Views;

namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 1. Tự động load trang chủ ngay khi mở cửa sổ chính
            MainContent.Navigate(new HomeView());
            MainContent.Visibility = Visibility.Visible;
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
            if (!(MainContent.Content is HomeView))
            {
                MainContent.Navigate(new HomeView());
            }
        }

        // Chuyển sang quản lý kho
        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is InventoryView))
            {
                MainContent.Navigate(new InventoryView());
            }
        }


        // Chuyển sang quản lý Bàn
        private void BtnTable_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is TableView))
            {
                MainContent.Navigate(new TableView());
            }
        }

        // Chuyển sang giao diện Menu gọi món (bán hàng)
        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is PosMenuView))
            {
                MainContent.Navigate(new PosMenuView());
            }
        }



        // Chuyển sang quản lý sản phẩm
        private void BtnProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!(MainContent.Content is ProductManagementView))
            {
                MainContent.Navigate(new ProductManagementView());
            }

            MainContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

    }
}