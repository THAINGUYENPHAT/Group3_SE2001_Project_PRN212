
using System;
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class MenuView : UserControl
    {
        private readonly MenuViewModel _viewModel;

        public MenuView()
        {
            InitializeComponent();

            var menuService = new MenuService(
                new MenuRepository(App.CreateDbContext()));

            var categoryService = new CategoryService(
                new CategoryRepository(App.CreateDbContext()));

            var dialogService = new MenuDialogService(
                () => Window.GetWindow(this),
                categoryService);

            _viewModel = new MenuViewModel(
                menuService,
                categoryService,
                dialogService);

            DataContext = _viewModel;

            Loaded += MenuView_Loaded;
        }

        private async void MenuView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= MenuView_Loaded;

            try
            {
                await _viewModel.LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải menu:\n" + ex.Message,
                    "CoffeeRuu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
