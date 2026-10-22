
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new LoginViewModel();

            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
        }

<<<<<<< HEAD
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


        // chuyển sang order processing

        private void OpenOrders_Click(
=======
        private void PasswordInput_PasswordChanged(
>>>>>>> 3b7c2c98213907ade476d62654aff3f9beb04099
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordInput.Password;
            }
        }
    }
}
