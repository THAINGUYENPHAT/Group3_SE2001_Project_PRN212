using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
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

            MainContent.Navigate(page);

            HomeContent.Visibility = Visibility.Collapsed;

            MainContent.Visibility = Visibility.Visible;
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
            MainContent.Navigate(orderPage);

            // Ẩn trang chủ
            HomeContent.Visibility = Visibility.Collapsed;

            // Hiện Order Processing
            MainContent.Visibility = Visibility.Visible;
        }

        // KDS - PHA CHẾ

        private void OpenKds_Click(
            object sender,
            RoutedEventArgs e)
        {
            var kdsPage = new KdsView
            {
                DataContext = new KdsViewModel(
                    GetOrderModuleService())
            };

            ShowPage(kdsPage);
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

        // HIỂN THỊ PAGE TRONG MAINWINDOW
        private void ShowPage(Page page)
        {
            MainContent.Navigate(page);

            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }



    }
}
