
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class OrderView : Page
    {
        private bool _loaded;

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

            OrderCreateFrame.Navigate(
                new OrderCreateView
                {
                    DataContext = vm
                });

            OrderListFrame.Navigate(
                new OrderListView
                {
                    DataContext = vm
                });

            await vm.InitializeAsync();
        }
    }
}
