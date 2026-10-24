
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
        private readonly IMenuDialogService _dialogService;

        // Lưu toàn bộ danh sách món lấy từ database.
        // Các thao tác tìm kiếm và lọc sẽ thực hiện trên danh sách này.
        private List<Product> _allProducts = new List<Product>();

        // Danh sách món đang hiển thị trên giao diện.
        public ObservableCollection<Product> Products { get; }
            = new ObservableCollection<Product>();

        // Danh sách tên danh mục để hiển thị trong ComboBox.
        public ObservableCollection<string> Categories { get; }
            = new ObservableCollection<string>();

        // Các lựa chọn sắp xếp danh sách món.
        public ObservableCollection<string> SortOptions { get; }
            = new ObservableCollection<string>
            {
                "Mặc định",
                "Giá thấp đến cao",
                "Giá cao đến thấp"
            };

        // Các lựa chọn lọc theo trạng thái bán.
        public ObservableCollection<string> AvailabilityOptions { get; }
            = new ObservableCollection<string>
            {
                "Tất cả trạng thái",
                "Đang bán",
                "Ngừng bán"
            };

        // Từ khóa người dùng nhập để tìm kiếm món.
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

        // Danh mục đang được chọn để lọc.
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

                    // Cập nhật danh sách khi đổi danh mục.
                    ApplyFilters();
                }
            }
        }

        // Kiểu sắp xếp đang được chọn.
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

                    // Sắp xếp lại khi người dùng đổi lựa chọn.
                    ApplyFilters();
                }
            }
        }

        // Trạng thái bán đang được chọn để lọc.
        private string _selectedAvailability = "Tất cả trạng thái";

        public string SelectedAvailability
        {
            get => _selectedAvailability;
            set
            {
                if (_selectedAvailability != value)
                {
                    _selectedAvailability = value;
                    OnPropertyChanged();

                    // Lọc lại danh sách theo trạng thái bán.
                    ApplyFilters();
                }
            }
        }

        // Món hiện đang được chọn trên DataGrid.
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

                    // Cập nhật trạng thái bật/tắt của các nút.
                    EditCommand?.RaiseCanExecuteChanged();
                    DeleteCommand?.RaiseCanExecuteChanged();
                    ToggleAvailabilityCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        // Các command được liên kết với nút trên giao diện.
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

            // Mở hộp thoại thêm món.
            AddCommand = new AsyncRelayCommand(
                AddProductCommandAsync);

            // Chỉ sửa khi đã chọn một món.
            EditCommand = new AsyncRelayCommand(
                EditProductAsync,
                () => SelectedProduct != null);

            // Chỉ xóa khi đã chọn một món.
            DeleteCommand = new AsyncRelayCommand(
                DeleteProductAsync,
                () => SelectedProduct != null);

            // Thực hiện tìm kiếm theo từ khóa.
            SearchCommand = new AsyncRelayCommand(
                SearchProductsAsync);

            // Bật hoặc ngừng bán món đang chọn.
            ToggleAvailabilityCommand = new AsyncRelayCommand(
                ToggleAvailabilityAsync,
                () => SelectedProduct != null);
        }

        // Tải danh sách món từ database và lấy danh mục.
        public async Task LoadProductsAsync()
        {
            try
            {
                _allProducts =
                    await _menuService.GetAllProductsAsync();

                // Sắp xếp dữ liệu gốc theo ID để có thứ tự ổn định.
                _allProducts = _allProducts
                    .OrderBy(p => p.ProductId)
                    .ToList();

                // Nạp lại danh mục.
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

                // Nếu danh mục đang chọn không còn tồn tại,
                // chuyển về lựa chọn "Tất cả".
                if (!Categories.Contains(SelectedCategory))
                {
                    _selectedCategory = "Tất cả";
                    OnPropertyChanged(nameof(SelectedCategory));
                }

                // Hiển thị danh sách sau khi tải xong.
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

        // Thực hiện tìm kiếm khi người dùng nhấn nút Tìm món.
        private Task SearchProductsAsync()
        {
            ApplyFilters();
            return Task.CompletedTask;
        }

        // Kết hợp tìm kiếm, lọc danh mục, lọc trạng thái
        // và sắp xếp danh sách món.
        private void ApplyFilters()
        {
            string keyword = (SearchKeyword ?? "").Trim();

            IEnumerable<Product> result = _allProducts;

            // 1. Tìm theo tên món, không phân biệt chữ hoa/thường.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                result = result.Where(p =>
                    p.ProductName.IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // 2. Lọc theo danh mục được chọn.
            if (SelectedCategory != "Tất cả")
            {
                result = result.Where(p =>
                    p.Category != null &&
                    p.Category.CategoryName == SelectedCategory);
            }

            // 3. Lọc theo trạng thái bán của món.
            if (SelectedAvailability == "Đang bán")
            {
                result = result.Where(p => p.IsAvailable);
            }
            else if (SelectedAvailability == "Ngừng bán")
            {
                result = result.Where(p => !p.IsAvailable);
            }

            // 4. Sắp xếp danh sách.
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
                // Mặc định luôn sắp xếp theo ID tăng dần.
                result = result.OrderBy(p => p.ProductId);
            }

            // Cập nhật danh sách hiển thị trên giao diện.
            Products.Clear();

            foreach (Product product in result.ToList())
            {
                Products.Add(product);
            }

            // Xóa lựa chọn cũ nếu danh sách đã thay đổi.
            SelectedProduct = null;
        }

        // Lấy giá thấp nhất trong các size đang được bán.
        // Dùng để sắp xếp giá từ thấp đến cao hoặc ngược lại.
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

        // Thêm món mới thông qua hộp thoại.
        private async Task AddProductCommandAsync()
        {
            Product? product = _dialogService.ShowProductDialog();

            // Người dùng đóng hộp thoại hoặc nhấn Hủy.
            if (product == null)
                return;

            try
            {
                await _menuService.AddProductAsync(product);

                // Tải lại danh sách sau khi thêm thành công.
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

        // Sửa thông tin món đang được chọn.
        private async Task EditProductAsync()
        {
            if (SelectedProduct == null)
                return;

            // Giữ lại ID để đảm bảo cập nhật đúng món.
            int productId = SelectedProduct.ProductId;

            Product? product =
                _dialogService.ShowProductDialog(SelectedProduct);

            // Người dùng nhấn Hủy.
            if (product == null)
                return;

            product.ProductId = productId;

            try
            {
                await _menuService.UpdateProductAsync(product);

                // Tải lại dữ liệu sau khi sửa.
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

        // Bật bán hoặc ngừng bán món đang được chọn.
        // Thao tác này không xóa món khỏi database.
        private async Task ToggleAvailabilityAsync()
        {
            if (SelectedProduct == null)
                return;

            Product product = SelectedProduct;

            // Lưu trạng thái cũ để phục hồi nếu lưu thất bại.
            bool oldStatus = product.IsAvailable;

            // Xác định trạng thái mới.
            bool newStatus = !oldStatus;

            try
            {
                product.IsAvailable = newStatus;

                // Lưu trạng thái mới xuống database.
                await _menuService.UpdateProductAsync(product);

                // Tải lại để giao diện phản ánh dữ liệu đã lưu.
                // Nếu đang lọc "Đang bán", món vừa ngừng bán
                // sẽ tự biến mất khỏi danh sách đó.
                await LoadProductsAsync();

                _dialogService.ShowMessage(
                    newStatus
                        ? "Đã bật bán món uống."
                        : "Đã ngừng bán món uống.",
                    "Thông báo",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Khôi phục trạng thái cũ trên đối tượng.
                product.IsAvailable = oldStatus;

                _dialogService.ShowMessage(
                    "Không thể cập nhật trạng thái bán: "
                    + ex.Message,
                    "Lỗi",
                    MessageBoxImage.Error);

                // Tải lại dữ liệu để giao diện đồng bộ.
                await LoadProductsAsync();
            }
        }

        // Xóa món đang được chọn sau khi xác nhận.
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

                // Cập nhật danh sách sau khi xóa.
                await LoadProductsAsync();

                _dialogService.ShowMessage(
                    "Đã xóa món uống thành công.",
                    "Thông báo",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(
                    "Không thể xóa món uống. Có thể món đang được "
                    + "sử dụng trong công thức hoặc đơn hàng.\n\n"
                    + ex.Message,
                    "Lỗi xóa",
                    MessageBoxImage.Error);
            }
        }

        // Thông báo cho giao diện biết thuộc tính đã thay đổi.
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
