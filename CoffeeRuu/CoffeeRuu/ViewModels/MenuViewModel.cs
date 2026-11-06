using CoffeeRuu.Models;
using CoffeeRuu.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CoffeeRuu.ViewModels
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly MenuService _menuService;
        private readonly IMenuDialogService _dialogService;

        // Lưu toàn bộ danh sách món lấy từ database.
        private List<Product> _allProducts = new List<Product>();

        // Danh sách món đang hiển thị.
        public ObservableCollection<Product> Products { get; }
            = new ObservableCollection<Product>();

        // Danh sách danh mục.
        public ObservableCollection<string> Categories { get; }
            = new ObservableCollection<string>();

        // Các lựa chọn sắp xếp.
        public ObservableCollection<string> SortOptions { get; }
            = new ObservableCollection<string>
            {
                "Mặc định",
                "Giá thấp đến cao",
                "Giá cao đến thấp"
            };

        // Các lựa chọn lọc trạng thái.
        public ObservableCollection<string> AvailabilityOptions { get; }
            = new ObservableCollection<string>
            {
                "Tất cả trạng thái",
                "Đang bán",
                "Ngừng bán"
            };

        // Từ khóa tìm kiếm.
        private string _searchKeyword = "";

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value ?? "";
                    OnPropertyChanged();
                }
            }
        }

        // Danh mục đang chọn.
        private string _selectedCategory = "Tất cả";

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value ?? "Tất cả";
                    OnPropertyChanged();

                    ApplyFilters();
                }
            }
        }

        // Kiểu sắp xếp đang chọn.
        private string _selectedSort = "Mặc định";

        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value ?? "Mặc định";
                    OnPropertyChanged();

                    ApplyFilters();
                }
            }
        }

        // Trạng thái bán đang chọn.
        private string _selectedAvailability = "Tất cả trạng thái";

        public string SelectedAvailability
        {
            get => _selectedAvailability;
            set
            {
                if (_selectedAvailability != value)
                {
                    _selectedAvailability = value ?? "Tất cả trạng thái";
                    OnPropertyChanged();

                    ApplyFilters();
                }
            }
        }

        // Món đang được chọn trên DataGrid.
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
                    ToggleAvailabilityCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        // Các command liên kết với nút trên giao diện.
        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand ToggleAvailabilityCommand { get; }

        // Khởi tạo ViewModel và các command.
        public MenuViewModel(
            MenuService menuService,
            IMenuDialogService dialogService)
        {
            _menuService = menuService;
            _dialogService = dialogService;

            AddCommand = new AsyncRelayCommand(
                AddProductCommandAsync,
                onError: HandleUnexpectedError);

            EditCommand = new AsyncRelayCommand(
                EditProductAsync,
                () => SelectedProduct != null,
                HandleUnexpectedError);

            DeleteCommand = new AsyncRelayCommand(
                DeleteProductAsync,
                () => SelectedProduct != null,
                HandleUnexpectedError);

            SearchCommand = new AsyncRelayCommand(
                SearchProductsAsync,
                onError: HandleUnexpectedError);

            ToggleAvailabilityCommand = new AsyncRelayCommand(
                ToggleAvailabilityAsync,
                () => SelectedProduct != null,
                HandleUnexpectedError);
        }

        // Tải danh sách món và danh mục từ database.
        public async Task LoadProductsAsync()
        {
            try
            {
                _allProducts = await _menuService.GetAllProductsAsync();

                // Sắp xếp mặc định theo ProductId tăng dần.
                _allProducts = _allProducts
                    .OrderBy(p => p.ProductId)
                    .ToList();

                // Cập nhật danh sách danh mục.
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

                // Nếu danh mục cũ không còn tồn tại,
                // chuyển về "Tất cả".
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
                    DialogMessageType.Error);
            }
        }

        // Tìm kiếm món khi nhấn nút Tìm món.
        private Task SearchProductsAsync()
        {
            ApplyFilters();
            return Task.CompletedTask;
        }

        // Áp dụng tìm kiếm, lọc và sắp xếp.
        private void ApplyFilters()
        {
            string keyword = SearchKeyword.Trim();

            IEnumerable<Product> result = _allProducts;

            // 1. Tìm kiếm theo tên món.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                result = result.Where(p =>
                    (p.ProductName ?? "").IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 2. Lọc theo danh mục.
            if (SelectedCategory != "Tất cả")
            {
                result = result.Where(p =>
                    p.Category != null &&
                    p.Category.CategoryName == SelectedCategory);
            }

            // 3. Lọc theo trạng thái bán.
            if (SelectedAvailability == "Đang bán")
            {
                result = result.Where(p => p.IsAvailable);
            }
            else if (SelectedAvailability == "Ngừng bán")
            {
                result = result.Where(p => !p.IsAvailable);
            }

            // 4. Sắp xếp theo giá thấp nhất của các size đang bán.
            if (SelectedSort == "Giá thấp đến cao")
            {
                result = result
                    .OrderBy(GetLowestPrice)
                    .ThenBy(p => p.ProductId);
            }
            else if (SelectedSort == "Giá cao đến thấp")
            {
                result = result
                    .OrderByDescending(GetLowestPrice)
                    .ThenBy(p => p.ProductId);
            }
            else
            {
                // Mặc định sắp xếp theo ID tăng dần.
                result = result.OrderBy(p => p.ProductId);
            }

            // Cập nhật danh sách hiển thị.
            Products.Clear();

            foreach (Product product in result.ToList())
            {
                Products.Add(product);
            }

            // Bỏ chọn món cũ sau khi danh sách thay đổi.
            SelectedProduct = null;
        }

        // Lấy giá thấp nhất trong các size đang được bán.
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

        // Thêm món mới.
        private async Task AddProductCommandAsync()
        {
            Product? product = _dialogService.ShowProductDialog();

            if (product == null)
                return;

            try
            {
                await _menuService.AddProductAsync(product);

                _dialogService.ShowMessage(
                    "Thêm món uống thành công!",
                    "Thông báo",
                    DialogMessageType.Information);

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể thêm món: " + ex.Message,
                    "Lỗi",
                    DialogMessageType.Error);
            }
        }

        // Sửa món đang được chọn.
        private async Task EditProductAsync()
        {
            if (SelectedProduct == null)
                return;

            int productId = SelectedProduct.ProductId;

            // ProductDialog tạo đối tượng mới,
            // không sửa trực tiếp đối tượng đang hiển thị.
            Product? product =
                _dialogService.ShowProductDialog(SelectedProduct);

            if (product == null)
                return;

            product.ProductId = productId;

            try
            {
                await _menuService.UpdateProductAsync(product);

                _dialogService.ShowMessage(
                    "Cập nhật món uống thành công!",
                    "Thông báo",
                    DialogMessageType.Information);

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể sửa món: " + ex.Message,
                    "Lỗi",
                    DialogMessageType.Error);
            }
        }

        // Bật bán hoặc ngừng bán món.
        private async Task ToggleAvailabilityAsync()
        {
            if (SelectedProduct == null)
                return;

            Product product = SelectedProduct;

            bool oldStatus = product.IsAvailable;
            bool newStatus = !oldStatus;

            try
            {
                product.IsAvailable = newStatus;

                await _menuService.UpdateProductAsync(product);

                _dialogService.ShowMessage(
                    newStatus
                        ? "Đã bật bán món uống."
                        : "Đã ngừng bán món uống.",
                    "Thông báo",
                    DialogMessageType.Information);

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                // Khôi phục trạng thái nếu cập nhật thất bại.
                product.IsAvailable = oldStatus;

                _dialogService.ShowMessage(
                    "Không thể cập nhật trạng thái bán: " + ex.Message,
                    "Lỗi",
                    DialogMessageType.Error);

                await LoadProductsAsync();
            }
        }

        // Ngừng bán món sau khi người dùng xác nhận.
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

                _dialogService.ShowMessage(
                    "Đã ngừng bán món uống thành công.",
                    "Thông báo",
                    DialogMessageType.Information);

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể ngừng bán món:\n" + ex.Message,
                    "Lỗi",
                    DialogMessageType.Error);
            }
        }

        // Xử lý lỗi bất ngờ từ AsyncRelayCommand.
        private void HandleUnexpectedError(Exception ex)
        {
            _dialogService.ShowMessage(
                "Đã xảy ra lỗi khi thực hiện thao tác:\n" + ex.Message,
                "Lỗi",
                DialogMessageType.Error);
        }

        // Thông báo cho giao diện khi thuộc tính thay đổi.
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