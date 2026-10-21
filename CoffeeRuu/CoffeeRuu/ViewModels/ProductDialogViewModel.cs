
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class ProductDialogViewModel : INotifyPropertyChanged
    {
        private readonly CategoryService _categoryService;
        private readonly Product? _editingProduct;

        public ObservableCollection<Category> Categories { get; }
            = new ObservableCollection<Category>();

        public ObservableCollection<ProductSize> ProductSizes { get; }
            = new ObservableCollection<ProductSize>();

        private string _dialogTitle = "Thêm món uống";
        public string DialogTitle
        {
            get => _dialogTitle;
            set
            {
                _dialogTitle = value;
                OnPropertyChanged();
            }
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged();
            }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                _isAvailable = value;
                OnPropertyChanged();
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public Product? ProductResult { get; private set; }

        public event Action<bool>? RequestClose;

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand AddSizeCommand { get; }

        public ProductDialogViewModel(
            CategoryService categoryService,
            Product? product = null)
        {
            _categoryService = categoryService;
            _editingProduct = product;

            if (product != null)
            {
                DialogTitle = "Sửa món uống";
                ProductName = product.ProductName;
                Description = product.Description ?? "";
                IsAvailable = product.IsAvailable;

                // Sao chép size để chỉnh sửa không làm thay đổi
                // trực tiếp đối tượng Product ban đầu.
                foreach (ProductSize size in product.ProductSizes)
                {
                    ProductSizes.Add(new ProductSize
                    {
                        ProductSizeId = size.ProductSizeId,
                        ProductId = size.ProductId,
                        SizeName = size.SizeName,
                        Price = size.Price,
                        IsAvailable = size.IsAvailable
                    });
                }
            }
            else
            {
                // Món mới mặc định có một size.
                ProductSizes.Add(new ProductSize
                {
                    SizeName = "Nhỏ (S)",
                    Price = 0,
                    IsAvailable = true
                });
            }

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            AddSizeCommand = new RelayCommand(AddSize);
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories =
                    await _categoryService.GetActiveCategoriesAsync();

                Categories.Clear();

                foreach (Category category in categories)
                {
                    Categories.Add(category);
                }

                if (_editingProduct != null)
                {
                    SelectedCategory = Categories.FirstOrDefault(
                        c => c.CategoryId == _editingProduct.CategoryId);
                }
                else if (Categories.Count > 0)
                {
                    SelectedCategory = Categories[0];
                }

                if (Categories.Count == 0)
                {
                    ErrorMessage =
                        "Không có danh mục đang hoạt động.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Không thể tải danh mục: " + ex.Message;
            }
        }

        private void AddSize()
        {
            ProductSizes.Add(new ProductSize
            {
                SizeName = "",
                Price = 0,
                IsAvailable = true
            });
        }

        private void Save()
        {
            ErrorMessage = "";

            // Kiểm tra tên món.
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ErrorMessage = "Vui lòng nhập tên món uống.";
                return;
            }

            if (SelectedCategory == null)
            {
                ErrorMessage = "Vui lòng chọn danh mục.";
                return;
            }

            // Bắt buộc phải có ít nhất một size.
            if (ProductSizes.Count == 0)
            {
                ErrorMessage = "Vui lòng thêm ít nhất một size.";
                return;
            }

            // Kiểm tra từng size.
            foreach (ProductSize size in ProductSizes)
            {
                if (string.IsNullOrWhiteSpace(size.SizeName))
                {
                    ErrorMessage = "Tên size không được để trống.";
                    return;
                }

                if (size.SizeName.Trim().Length > 30)
                {
                    ErrorMessage =
                        "Tên size không được vượt quá 30 ký tự.";
                    return;
                }

                if (size.Price < 0)
                {
                    ErrorMessage = "Giá size không được âm.";
                    return;
                }
            }

            // Không cho phép hai size trùng tên.
            bool hasDuplicateSize = ProductSizes
                .GroupBy(
                    s => s.SizeName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (hasDuplicateSize)
            {
                ErrorMessage = "Tên các size không được trùng nhau.";
                return;
            }

            // Tạo Product kèm danh sách ProductSizes.
            Product product = new Product
            {
                ProductId = _editingProduct?.ProductId ?? 0,
                ProductName = ProductName.Trim(),
                CategoryId = SelectedCategory.CategoryId,
                Description = string.IsNullOrWhiteSpace(Description)
                    ? null
                    : Description.Trim(),
                ImageUrl = _editingProduct?.ImageUrl,
                IsAvailable = IsAvailable,
                CreatedAt = _editingProduct?.CreatedAt ?? DateTime.Now
            };

            foreach (ProductSize size in ProductSizes)
            {
                product.ProductSizes.Add(new ProductSize
                {
                    ProductSizeId = size.ProductSizeId,
                    ProductId = product.ProductId,
                    SizeName = size.SizeName.Trim(),
                    Price = size.Price,
                    IsAvailable = size.IsAvailable
                });
            }

            ProductResult = product;
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            ProductResult = null;
            RequestClose?.Invoke(false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
