
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class OrderView : Page
    {
        private OrderViewModel? _initializedVm;

        public OrderView()
        {
            InitializeComponent();

            DataContextChanged += OrderView_DataContextChanged;
        }

        private async void OrderView_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not OrderViewModel vm)
                return;

            if (ReferenceEquals(_initializedVm, vm))
                return;

            _initializedVm = vm;

            await vm.InitializeAsync();
        }
    }
}
