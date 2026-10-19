
using System.Windows;
using CoffeeRuu.Data;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using CoffeeRuu.Views;
using Microsoft.EntityFrameworkCore;
namespace CoffeeRuu
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenWarehouse_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page = new CoffeeRuu.Views.WarehousePage();

            ModuleHost.Navigate(page);

            HomeContent.Visibility = Visibility.Collapsed;

            ModuleHost.Visibility = Visibility.Visible;
        }


        private void OpenOrders_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Khởi tạo giao diện Order Processing
            var orderPage = new CoffeeRuu.Views.OrderView
            {
                DataContext = new CoffeeRuu.ViewModels.OrderViewModel(
                    GetOrderModuleService())
            };

            // Hiển thị trang Order Processing
            ModuleHost.Navigate(orderPage);

            // Ẩn trang chủ
            HomeContent.Visibility = Visibility.Collapsed;

            // Hiện Order Processing
            ModuleHost.Visibility = Visibility.Visible;
        }


        private OrderService? _orderService;

        private OrderService GetOrderModuleService()
        {
            if (_orderService != null)
                return _orderService;

            // Kết nối SQL Server
            string connectionString =
                @"Server=.\SQLEXPRESS;Database=CoffeeRuuDB;Trusted_Connection=True;TrustServerCertificate=True;";

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



    }
}
