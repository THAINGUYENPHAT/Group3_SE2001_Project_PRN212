
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class ProductDialogViewModel : INotifyPropertyChanged
    {
        private readonly MenuService _menuService;
        private readonly Product? _editingProduct;

        // Danh sách danh mục.
        public ObservableCollection<Category> Categories { get; }
            = new ObservableCollection<Category>();

        // Tiêu đề cửa sổ.
        public string DialogTitle =>
            _editingProduct == null ? "Thêm món uống" : "Sửa món uống";

        // Tên món.
        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        // Mô tả món.
        private string _description = "";
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        // Trạng thái món.
        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
        }

        // Danh mục được chọn.
        private int? _selectedCategoryId;
        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set => SetProperty(ref _selectedCategoryId, value);
        }

        // Giá size S.
        private string _priceS = "";
        public string PriceS
        {
            get => _priceS;
            set => SetProperty(ref _priceS, value);
        }

        // Giá size M.
        private string _priceM = "";
        public string PriceM
        {
            get => _priceM;
            set => SetProperty(ref _priceM, value);
        }

        // Giá size L.
        private string _priceL = "";
        public string PriceL
        {
            get => _priceL;
            set => SetProperty(ref _priceL, value);
        }

        // Nội dung thông báo lỗi.
        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Sản phẩm sau khi lưu.
        public Product? ProductResult { get; private set; }

        // Các lệnh của Dialog.
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Yêu cầu View đóng cửa sổ.
        public event Action<bool>? CloseRequested;

        // Constructor.
        public ProductDialogViewModel(
            MenuService menuService,
            Product? product = null)
        {
            _menuService = menuService
                ?? throw new ArgumentNullException(nameof(menuService));

            _editingProduct = product;

            SaveCommand = new RelayCommand(Save);

            CancelCommand = new RelayCommand(
                () => CloseRequested?.Invoke(false));

            // Nếu đang sửa thì hiển thị dữ liệu cũ.
            if (_editingProduct != null)
            {
                ProductName = _editingProduct.ProductName;
                Description = _editingProduct.Description ?? "";
                IsAvailable = _editingProduct.IsAvailable;
                SelectedCategoryId = _editingProduct.CategoryId;

                PriceS = GetPriceText("S");
                PriceM = GetPriceText("M");
                PriceL = GetPriceText("L");
            }
        }

        // Tải danh mục thông qua Service.
        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories =
                    await _menuService.GetAllCategoriesAsync();

                Categories.Clear();

                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Giữ danh mục cũ khi sửa món.
                if (SelectedCategoryId.HasValue &&
                    Categories.Any(c =>
                        c.CategoryId == SelectedCategoryId.Value))
                {
                    return;
                }

                // Chọn danh mục đầu tiên khi thêm mới.
                SelectedCategoryId = Categories.Count > 0
                    ? Categories[0].CategoryId
                    : null;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Không thể tải danh mục: " + ex.Message;
            }
        }

        // Lấy giá size cũ để hiển thị khi sửa.
        private string GetPriceText(string code)
        {
            var size = FindExistingSize(code);

            if (size == null || !size.IsAvailable)
                return "";

            return size.Price.ToString(
                "0.##",
                CultureInfo.CurrentCulture);
        }

        // Tìm size theo mã S, M hoặc L.
        private ProductSize? FindExistingSize(string code)
        {
            return _editingProduct?.ProductSizes?.FirstOrDefault(s =>
                s.SizeName.EndsWith(
                    $"({code})",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    s.SizeName,
                    code,
                    StringComparison.OrdinalIgnoreCase));
        }

        // Xử lý nút Lưu.
        private void Save()
        {
            ErrorMessage = "";

            string name = ProductName?.Trim() ?? "";
            string description = Description?.Trim() ?? "";

            // Kiểm tra tên món.
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Vui lòng nhập tên món uống.";
                return;
            }

            // Kiểm tra danh mục.
            if (!SelectedCategoryId.HasValue)
            {
                ErrorMessage = "Vui lòng chọn danh mục.";
                return;
            }

            // Kiểm tra giá từng size.
            if (!TryReadPrice(PriceS, true, out decimal priceS) ||
                !TryReadPrice(PriceM, false, out decimal priceM) ||
                !TryReadPrice(PriceL, false, out decimal priceL))
            {
                ErrorMessage =
                    "Giá phải là số lớn hơn 1.000đ. " +
                    "Size S bắt buộc nhập, size M và L có thể để trống.";
                return;
            }

            // Tạo sản phẩm mới từ dữ liệu trên giao diện.
            var product = new Product
            {
                ProductId = _editingProduct?.ProductId ?? 0,
                ProductName = name,
                CategoryId = SelectedCategoryId.Value,

                Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,

                ImageUrl = _editingProduct?.ImageUrl,
                IsAvailable = IsAvailable,
                CreatedAt = _editingProduct?.CreatedAt ?? DateTime.Now
            };

            // Thêm các size đang bán hoặc ngừng bán.
            AddOrUpdateSize(product, "S", "Nhỏ (S)", priceS);
            AddOrUpdateSize(product, "M", "Lớn (M)", priceM);
            AddOrUpdateSize(product, "L", "Rất lớn (L)", priceL);

            // Trả dữ liệu về MenuViewModel.
            ProductResult = product;

            // Yêu cầu đóng Dialog và báo lưu thành công.
            CloseRequested?.Invoke(true);
        }

        // Kiểm tra và chuyển giá từ chuỗi sang decimal.
        private bool TryReadPrice(
            string text,
            bool required,
            out decimal price)
        {
            text = text?.Trim() ?? "";

            // Không nhập giá.
            if (string.IsNullOrWhiteSpace(text))
            {
                price = 0;
                return !required;
            }

            // Giá phải là số và lớn hơn 1.000đ.
            return decimal.TryParse(
                       text,
                       NumberStyles.Number,
                       CultureInfo.CurrentCulture,
                       out price)
                   && price > 1000;
        }

        // Thêm hoặc cập nhật thông tin size.
        private void AddOrUpdateSize(
            Product product,
            string code,
            string sizeName,
            decimal price)
        {
            var oldSize = FindExistingSize(code);

            // Nếu size chưa tồn tại và không nhập giá thì bỏ qua.
            if (oldSize == null && price <= 0)
                return;

            // Nếu size đã tồn tại nhưng không nhập giá,
            // giữ bản ghi và chuyển trạng thái sang ngừng bán.
            product.ProductSizes.Add(new ProductSize
            {
                ProductSizeId = oldSize?.ProductSizeId ?? 0,
                ProductId = product.ProductId,
                SizeName = sizeName,
                Price = price > 0
                    ? price
                    : oldSize?.Price ?? 0,
                IsAvailable = price > 0
            });
        }

        // Cập nhật dữ liệu cho Binding.
        private bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Thông báo khi Property thay đổi.
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // ICommand cho nút Lưu và Hủy.
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;

            public RelayCommand(Action execute)
            {
                _execute = execute;
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                _execute();
            }

            public event EventHandler? CanExecuteChanged;
        }
    }
}
