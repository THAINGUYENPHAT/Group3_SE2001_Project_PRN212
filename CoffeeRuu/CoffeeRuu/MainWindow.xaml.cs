using CoffeeRuu.ViewModels;
using CoffeeRuu.Views;
using System.Windows;
using System;
using System.Configuration;
using CoffeeRuu.Data;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using Microsoft.EntityFrameworkCore;
namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        private OrderService? _orderService;

        // khởi tạo order service
        private OrderService GetOrderModuleService()
        {
            if (_orderService != null)
            {
                return _orderService;
            }

            // Lấy connection string riêng của máy
            string? connectionString =
                Environment.GetEnvironmentVariable(
                    "COFFEERUU_CONNECTION");

            // Nếu không có thì lấy từ App.config
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString =
                    ConfigurationManager.ConnectionStrings[
                        "CoffeeRuuConnection"]?.ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy CoffeeRuuConnection.");
            }

            // Cấu hình Entity Framework Core
            var options =
                new DbContextOptionsBuilder<CoffeeRuuDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

            // Khởi tạo Repository
            var repository = new OrderRepository(options);

            // Khởi tạo Service
            _orderService = new OrderService(repository);

            return _orderService;
        }
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


        // chuyển sang order processing

        private void OpenOrders_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (MainContent.Content is OrderView)
            {
                return;
            }

            var orderViewModel = new OrderViewModel(
                GetOrderModuleService());

            var orderPage = new OrderView
            {
                DataContext = orderViewModel
            };

            MainContent.Navigate(orderPage);
        }


        // chuyển sang kds pha chế

        private void OpenKds_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (MainContent.Content is KdsView)
            {
                return;
            }

            var kdsViewModel = new KdsViewModel(
                GetOrderModuleService());

            var kdsPage = new KdsView
            {
                DataContext = kdsViewModel
            };

            MainContent.Navigate(kdsPage);
        }

    }
}