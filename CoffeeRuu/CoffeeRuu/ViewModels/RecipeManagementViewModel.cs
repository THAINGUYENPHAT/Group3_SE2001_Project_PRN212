using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    // Lớp hiển thị chi tiết nguyên liệu trong công thức gắn với Size
    public class RecipeItemDisplayModel
    {
        public int ProductSizeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; } // Thay cho QuantityRequired
        public string Unit { get; set; } = string.Empty;
    } 

    public class RecipeManagementViewModel : ViewModelBase
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        private Product? _selectedProduct;
        private RecipeItemDisplayModel? _selectedRecipeItem;
        private Ingredient? _selectedIngredientInput;
        private string _quantityInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        // Binding trực tiếp với Products để khớp 100% với cách hiển thị của các View khác
        public ObservableCollection<Product> ProductsView { get; } = new ObservableCollection<Product>();
        public ObservableCollection<RecipeItemDisplayModel> RecipeItemsView { get; } = new ObservableCollection<RecipeItemDisplayModel>();
        public ObservableCollection<Ingredient> AllIngredientsList { get; } = new ObservableCollection<Ingredient>();

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    StatusMessage = string.Empty;
                    _ = LoadRecipeForSelectedProductAsync();
                }
            }
        }

        public RecipeItemDisplayModel? SelectedRecipeItem
        {
            get => _selectedRecipeItem;
            set
            {
                if (SetProperty(ref _selectedRecipeItem, value) && value != null)
                {
                    QuantityInput = value.QuantityUsed.ToString();
                    SelectedIngredientInput = AllIngredientsList.FirstOrDefault(i => i.IngredientId == value.IngredientId);
                }
            }
        }

        public Ingredient? SelectedIngredientInput
        {
            get => _selectedIngredientInput;
            set => SetProperty(ref _selectedIngredientInput, value);
        }

        public string QuantityInput
        {
            get => _quantityInput;
            set => SetProperty(ref _quantityInput, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public ICommand LoadRecipeCommand { get; }
        public ICommand SaveRecipeCommand { get; }
        public ICommand DeleteRecipeCommand { get; }

        public RecipeManagementViewModel(Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;

            LoadRecipeCommand = new RelayCommand(async () => await LoadRecipeForSelectedProductAsync());
            SaveRecipeCommand = new RelayCommand(async () => await SaveRecipeAsync());
            DeleteRecipeCommand = new RelayCommand(async () => await DeleteRecipeAsync(), () => SelectedRecipeItem != null);

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                using var db = _contextFactory();

                // 1. Tải danh sách nguyên liệu
                var ingredients = await db.Ingredients.AsNoTracking().OrderBy(i => i.IngredientName).ToListAsync();
                AllIngredientsList.Clear();
                foreach (var ing in ingredients)
                {
                    AllIngredientsList.Add(ing);
                }

                // 2. Tải danh sách sản phẩm kèm theo Category để binding Category.CategoryName hoặc CategoryName
                var products = await db.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                ProductsView.Clear();
                foreach (var p in products)
                {
                    ProductsView.Add(p);
                }

                if (ProductsView.Count > 0)
                {
                    SelectedProduct = ProductsView[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
                MessageBox.Show($"Lỗi chi tiết: {ex.ToString()}", "Debug Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadRecipeForSelectedProductAsync()
        {
            RecipeItemsView.Clear();
            if (SelectedProduct == null) return;

            try
            {
                using var db = _contextFactory();

                // Lấy danh sách ProductSizeId thuộc sản phẩm đang chọn
                var sizeIds = await db.ProductSizes
                    .Where(s => s.ProductId == SelectedProduct.ProductId)
                    .Select(s => s.ProductSizeId)
                    .ToListAsync();

                if (!sizeIds.Any()) return;

                // Lấy các dòng định lượng nguyên liệu tương ứng với các size của sản phẩm
                var recipes = await db.ProductRecipes
                    .Include(r => r.Ingredient)
                    .Where(r => sizeIds.Contains(r.ProductSizeId))
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var rec in recipes)
                {
                    RecipeItemsView.Add(new RecipeItemDisplayModel
                    {
                        ProductSizeId = rec.ProductSizeId,
                        IngredientId = rec.IngredientId,
                        IngredientName = rec.Ingredient?.IngredientName ?? "N/A",
                        QuantityUsed = rec.QuantityUsed,
                        Unit = rec.Ingredient?.Unit ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải công thức: {ex.Message}";
            }
        }

        private async Task SaveRecipeAsync()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm bên cột trái!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedIngredientInput == null)
            {
                MessageBox.Show("Vui lòng chọn nguyên liệu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityInput, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng định lượng lớn hơn 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = _contextFactory();

                // Tìm hoặc tạo Size mặc định (Standard) cho sản phẩm nếu chưa có
                var productSize = await db.ProductSizes.FirstOrDefaultAsync(s => s.ProductId == SelectedProduct.ProductId);
                if (productSize == null)
                {
                    productSize = new ProductSize
                    {
                        ProductId = SelectedProduct.ProductId,
                        SizeName = "Standard",
                        Price = 0
                    };
                    db.ProductSizes.Add(productSize);
                    await db.SaveChangesAsync();
                }

                // Kiểm tra nguyên liệu đã có trong công thức của size này chưa
                var existingRecipe = await db.ProductRecipes
                    .FirstOrDefaultAsync(r => r.ProductSizeId == productSize.ProductSizeId && r.IngredientId == SelectedIngredientInput.IngredientId);

                if (existingRecipe != null)
                {
                    existingRecipe.QuantityUsed = quantity;
                    StatusMessage = "Cập nhật định lượng thành công!";
                }
                else
                {
                    var newRecipe = new ProductRecipe
                    {
                        ProductSizeId = productSize.ProductSizeId,
                        IngredientId = SelectedIngredientInput.IngredientId,
                        QuantityUsed = quantity
                    };
                    db.ProductRecipes.Add(newRecipe);
                    StatusMessage = "Thêm nguyên liệu vào công thức thành công!";
                }

                await db.SaveChangesAsync();
                QuantityInput = string.Empty;
                SelectedIngredientInput = null;
                await LoadRecipeForSelectedProductAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu công thức: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteRecipeAsync()
        {
            if (SelectedRecipeItem == null) return;

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa nguyên liệu này khỏi công thức?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = _contextFactory();
                var entity = await db.ProductRecipes.FindAsync(SelectedRecipeItem.ProductSizeId, SelectedRecipeItem.IngredientId);
                if (entity != null)
                {
                    db.ProductRecipes.Remove(entity);
                    await db.SaveChangesAsync();
                    StatusMessage = "Đã xóa nguyên liệu khỏi công thức.";
                    SelectedRecipeItem = null;
                    await LoadRecipeForSelectedProductAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}