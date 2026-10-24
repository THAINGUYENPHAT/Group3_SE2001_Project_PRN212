using System.Windows.Controls;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class LowStockAlertView : Page
    {
        public LowStockAlertView()
        {
            InitializeComponent();

            var repository =
                new IngredientRepository(App.CreateDbContext);

            var service =
                new LowStockAlertService(repository);

            var viewModel =
                new LowStockAlertViewModel(service);

            DataContext = viewModel;

            Loaded += async (_, _) =>
                await viewModel.LoadAsync();
        }
    }
}