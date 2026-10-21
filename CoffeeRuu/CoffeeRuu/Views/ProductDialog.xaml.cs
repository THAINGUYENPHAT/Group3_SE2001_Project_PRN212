
using System;
using System.Windows;
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class ProductDialog : Window
    {
        private readonly ProductDialogViewModel _viewModel;

        public Product? ProductResult { get; private set; }

        public ProductDialog(
            CategoryService categoryService,
            Product? product = null)
        {
            InitializeComponent();

            _viewModel = new ProductDialogViewModel(
                categoryService,
                product);

            DataContext = _viewModel;

            _viewModel.RequestClose += OnRequestClose;
            Loaded += ProductDialog_Loaded;
        }

        private async void ProductDialog_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= ProductDialog_Loaded;

            try
            {
                await _viewModel.LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh mục:\n" + ex.Message,
                    "CoffeeRuu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnRequestClose(bool result)
        {
            ProductResult = result
                ? _viewModel.ProductResult
                : null;

            DialogResult = result;
        }
    }
}
