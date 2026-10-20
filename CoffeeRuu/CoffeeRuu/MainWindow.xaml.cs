
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
            MainContent.Navigate(new InventoryView());
            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }


        // Chuyển sang màn hình báo cáo (Analytics & Reports)
        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Navigate(new ReportView());
            HomeContent.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

    }
}
