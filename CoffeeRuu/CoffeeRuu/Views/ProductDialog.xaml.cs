
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using System.Windows;

namespace CoffeeRuu.Views
{
    public partial class ProductDialog : Window
    {
        private readonly ProductDialogViewModel _viewModel;

        // Trả kết quả sản phẩm cho MenuDialogService.
        public Product? ProductResult => _viewModel.ProductResult;

        public ProductDialog(
            MenuService menuService,
            Product? product = null)
        {
            InitializeComponent();

            // Khởi tạo ViewModel.
            _viewModel = new ProductDialogViewModel(
                menuService,
                product);

            DataContext = _viewModel;

            // Nhận yêu cầu đóng cửa sổ từ ViewModel.
            _viewModel.CloseRequested += result =>
            {
                DialogResult = result;
            };

            // Tải danh mục khi mở cửa sổ.
            Loaded += async (_, _) =>
            {
                await _viewModel.LoadCategoriesAsync();
            };
        }
    }
}
