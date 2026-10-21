
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.Views;

namespace CoffeeRuu.ViewModels
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly MenuService _menuService;
        private readonly IMenuDialogService _dialogService;

        private List<Product> _allProducts = new List<Product>();

        public ObservableCollection<Product> Products { get; }
            = new ObservableCollection<Product>();

        public ObservableCollection<string> Categories { get; }
            = new ObservableCollection<string>();

        public ObservableCollection<string> SortOptions { get; }
            = new ObservableCollection<string>
            {
                "Mặc định",
                "Giá thấp đến cao",
                "Giá cao đến thấp"
            };

        private string _searchKeyword = "";

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _selectedCategory = "Tất cả";

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private string _selectedSort = "Mặc định";

        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private Product? _selectedProduct;

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged();

                    EditCommand?.RaiseCanExecuteChanged();
                    DeleteCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SearchCommand { get; }

        public MenuViewModel(
            MenuService menuService,
            IMenuDialogService dialogService)
        {
            _menuService = menuService;
            _dialogService = dialogService;

            AddCommand = new AsyncRelayCommand(
                AddProductCommandAsync);

            EditCommand = new AsyncRelayCommand(
                EditProductAsync,
                () => SelectedProduct != null);

            DeleteCommand = new AsyncRelayCommand(
                DeleteProductAsync,
                () => SelectedProduct != null);

            SearchCommand = new AsyncRelayCommand(
                SearchProductsAsync);
        }

        // Tải danh sách món và danh mục từ database
        public async Task LoadProductsAsync()
        {
            try
            {
                _allProducts =
                    await _menuService.GetAllProductsAsync();

                Categories.Clear();
                Categories.Add("Tất cả");

                foreach (string category in _allProducts
                    .Where(p => p.Category != null)
                    .Select(p => p.Category.CategoryName)
                    .Distinct()
                    .OrderBy(c => c))
                {
                    Categories.Add(category);
                }

                if (!Categories.Contains(SelectedCategory))
                {
                    _selectedCategory = "Tất cả";
                    OnPropertyChanged(nameof(SelectedCategory));
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể tải thực đơn: " + ex.Message,
                    "Lỗi",
                    MessageBoxImage.Error);
            }
        }

        // Xử lý tìm kiếm
        private Task SearchProductsAsync()
        {
            ApplyFilters();
            return Task.CompletedTask;
        }

        // Tìm kiếm, lọc danh mục và sắp xếp
        private void ApplyFilters()
        {
            string keyword = (SearchKeyword ?? "").Trim();

            IEnumerable<Product> result = _allProducts;

            // Tìm theo tên món, không phân biệt chữ hoa/thường
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                result = result.Where(p =>
                    p.ProductName.IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Lọc theo danh mục
            if (SelectedCategory != "Tất cả")
            {
                result = result.Where(p =>
                    p.Category != null &&
                    p.Category.CategoryName == SelectedCategory);
            }

            // Sắp xếp theo giá thấp nhất của size đang bán
            if (SelectedSort == "Giá thấp đến cao")
            {
                result = result.OrderBy(GetLowestPrice);
            }
            else if (SelectedSort == "Giá cao đến thấp")
            {
                result = result.OrderByDescending(GetLowestPrice);
            }

            Products.Clear();

            foreach (Product product in result.ToList())
            {
                Products.Add(product);
            }

            SelectedProduct = null;
        }

        // Lấy giá thấp nhất trong các size đang bán
        private static decimal GetLowestPrice(Product product)
        {
            var prices = product.ProductSizes
                .Where(s => s.IsAvailable)
                .Select(s => s.Price)
                .ToList();

            return prices.Count > 0
                ? prices.Min()
                : decimal.MaxValue;
        }

        // Thêm món mới
        private async Task AddProductCommandAsync()
        {
            Product? product =
                _dialogService.ShowProductDialog();

            if (product == null)
                return;

            try
            {
                await _menuService.AddProductAsync(product);
                await LoadProductsAsync();

                _dialogService.ShowMessage(
                    "Thêm món uống thành công!",
                    "Thông báo",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể thêm món: " + ex.Message,
                    "Lỗi",
                    MessageBoxImage.Error);
            }
        }

       
// Sửa món được chọn
private async Task EditProductAsync()
        {
            if (SelectedProduct == null)
            {
                _dialogService.ShowMessage(
                    "Vui lòng chọn món cần sửa.",
                    "Thông báo",
                    MessageBoxImage.Warning);
                return;
            }

            // Lưu mã món đang chọn
            int productId = SelectedProduct.ProductId;

            // Mở hộp thoại sửa món
            Product? product =
                _dialogService.ShowProductDialog(SelectedProduct);

            // Người dùng bấm Hủy
            if (product == null)
                return;

            // Đảm bảo cập nhật đúng món đã chọn
            product.ProductId = productId;

            try
            {
                await _menuService.UpdateProductAsync(product);

                await LoadProductsAsync();

                _dialogService.ShowMessage(
                    "Cập nhật món uống thành công!",
                    "Thông báo",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể sửa món: " + ex.Message,
                    "Lỗi",
                    MessageBoxImage.Error);
            }
        }


        // Xóa món được chọn
        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null)
                return;

            Product product = SelectedProduct;

            if (!_dialogService.ConfirmDelete(product.ProductName))
                return;

            try
            {
                await _menuService.DeleteProductAsync(
                    product.ProductId);

                SelectedProduct = null;

                await LoadProductsAsync();

                _dialogService.ShowMessage(
                    "Đã xóa món uống thành công.",
                    "Thông báo",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể xóa món uống. Có thể món đang được " +
                    "sử dụng trong công thức hoặc đơn hàng.\n\n" +
                    ex.Message,
                    "Lỗi xóa",
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
