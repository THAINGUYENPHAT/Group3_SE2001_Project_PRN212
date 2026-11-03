
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class OrderView : Page
    {
        private bool _loaded = false;

        public OrderView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_loaded)
                return;

            if (DataContext is not OrderViewModel vm)
                return;

            _loaded = true;

            // Chỉ hiển thị trang Danh sách đơn hàng
            OrderListFrame.Navigate(
                new OrderListView
                {
                    DataContext = vm
                });

            // Tải danh sách từ database
            await vm.InitializeAsync();
        }
    }
}
