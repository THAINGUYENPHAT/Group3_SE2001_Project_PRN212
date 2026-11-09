
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class KdsView : Page
    {
        private KdsViewModel? _initializedVm;

        public KdsView()
        {
            InitializeComponent();

            DataContextChanged += KdsView_DataContextChanged;
            IsVisibleChanged += KdsView_IsVisibleChanged;
            Unloaded += KdsView_Unloaded;
        }

        private async void KdsView_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is KdsViewModel oldVm)
            {
                oldVm.StopAutoRefresh();
            }

            if (e.NewValue is not KdsViewModel vm)
                return;

            if (ReferenceEquals(_initializedVm, vm))
                return;

            _initializedVm = vm;

            await vm.InitializeAsync();

            if (IsVisible && ReferenceEquals(DataContext, vm))
            {
                vm.StartAutoRefresh();
            }
        }

        private void KdsView_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is not KdsViewModel vm)
                return;

            if (IsVisible)
            {
                vm.StartAutoRefresh();
            }
            else
            {
                vm.StopAutoRefresh();
            }
        }

        private void KdsView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is KdsViewModel vm)
            {
                vm.StopAutoRefresh();
            }
        }
    }
}
