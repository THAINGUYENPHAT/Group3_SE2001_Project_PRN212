
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace CoffeeRuu.ViewModels
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly MenuService _menuService;
        private readonly CategoryService _categoryService;
        private readonly IMenuDialogService _dialogService;

        // Lưu toàn bộ sản phẩm từ database
        private List<Product> _allProducts = new List<Product>();

        // Danh sách sản phẩm đang hiển thị
        public ObservableCollection<Product> Products { get; }
            = new ObservableCollection<Product>();

        // Danh sách danh mục
        public ObservableCollection<string> Categories { get; }
            = new ObservableCollection<string>();

        // Từ khóa tìm kiếm
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

        // Danh mục đang chọn
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
                    ApplySearch();
                }
            }
        }

        // Sản phẩm đang chọn
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

                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SearchCommand { get; }

        public MenuViewModel(
            MenuService menuService,
            CategoryService categoryService,
            IMenuDialogService dialogService)
        {
            _menuService = menuService;
            _categoryService = categoryService;
            _dialogService = dialogService;

            AddCommand = new AsyncRelayCommand(AddProductAsync);

            EditCommand = new AsyncRelayCommand(
                EditProductAsync,
                () => SelectedProduct != null);

            DeleteCommand = new AsyncRelayCommand(
                DeleteProductAsync,
                () => SelectedProduct != null);

            SearchCommand = new AsyncRelayCommand(SearchProductsAsync);

            Categories.Add("Tất cả");
        }

        // READ: Tải sản phẩm và danh mục
        public async Task LoadProductsAsync()
        {
            try
            {
                _allProducts =
                    await _menuService.GetAllProductsAsync();

                var categories =
                    await _categoryService.GetActiveCategoriesAsync();

                Categories.Clear();
                Categories.Add("Tất cả");

                foreach (var category in categories)
                {
                    if (!string.IsNullOrWhiteSpace(category.CategoryName)
                        && !Categories.Contains(category.CategoryName))
                    {
                        Categories.Add(category.CategoryName);
                    }
                }

                SelectedCategory = "Tất cả";
                ApplySearch();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể tải thực đơn: " + ex.Message,
                    "Lỗi",
                    MessageBoxImage.Error);
            }
        }

        // SEARCH: Tìm kiếm sản phẩm
        private Task SearchProductsAsync()
        {
            ApplySearch();
            return Task.CompletedTask;
        }

        // Lọc theo tên món và danh mục
        private void ApplySearch()
        {
            string keyword = (SearchKeyword ?? "").Trim();

            var result = _allProducts
                .Where(p =>
                    // Tìm theo tên món
                    (!string.IsNullOrWhiteSpace(p.ProductName)
                     && p.ProductName.IndexOf(
                         keyword,
                         StringComparison.OrdinalIgnoreCase) >= 0)

                    // Lọc theo danh mục
                    && (string.IsNullOrWhiteSpace(SelectedCategory)
                        || SelectedCategory == "Tất cả"
                        || (p.Category != null
                            && string.Equals(
                                p.Category.CategoryName,
                                SelectedCategory,
                                StringComparison.OrdinalIgnoreCase)))
                )
                .ToList();

            Products.Clear();

            foreach (Product product in result)
            {
                Products.Add(product);
            }

            SelectedProduct = null;
        }

        // CREATE: Thêm món
        private async Task AddProductAsync()
        {
            Product? product = _dialogService.ShowProductDialog();

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

        // UPDATE: Sửa món đang chọn
        private async Task EditProductAsync()
        {
            if (SelectedProduct == null)
                return;

            Product? product =
                _dialogService.ShowProductDialog(SelectedProduct);

            if (product == null)
                return;

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

        // DELETE: Xóa món khỏi database
        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null)
                return;

            Product product = SelectedProduct;

            if (!_dialogService.ConfirmDelete(product.ProductName))
                return;

            try
            {
                await _menuService.DeleteProductAsync(product.ProductId);

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
                    "sử dụng trong size, công thức hoặc đơn hàng.\n\n" +
                    "Chi tiết: " + ex.Message,
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
