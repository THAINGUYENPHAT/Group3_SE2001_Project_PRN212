
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class KdsView : Page
    {
        private bool _initialized = false;

        public KdsView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_initialized)
                return;

            if (DataContext is not KdsViewModel vm)
                return;

            _initialized = true;

            // MỞ TRANG KDS
            KdsOrdersFrame.Navigate(
                new KdsOrderListView
                {
                    DataContext = vm
                });

            // TẢI ĐƠN PHA CHẾ TỪ DATABASE
            await vm.InitializeAsync();
        }
    }
}
