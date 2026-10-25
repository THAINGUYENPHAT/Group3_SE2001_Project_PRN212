
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class KdsView : Page
    {
        private bool _loaded;

        public KdsView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (_loaded)
                return;

            if (DataContext is not KdsViewModel vm)
                return;

            _loaded = true;

            KdsOrdersFrame.Navigate(
                new KdsOrderListView
                {
                    DataContext = vm
                });

            await vm.InitializeAsync();
        }
    }
}
