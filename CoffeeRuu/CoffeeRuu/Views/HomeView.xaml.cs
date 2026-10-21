using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Data;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class HomeView : UserControl
    {
        private readonly HomeViewModel _viewModel;

        public HomeView()
        {
            InitializeComponent();

            // Khởi tạo Service và gán ViewModel
            var repo = new TableRepository(() => App.CreateDbContext());
            var service = new TableService(repo);
            _viewModel = new HomeViewModel(service);

            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadTablesAsync();
        }
    }
}