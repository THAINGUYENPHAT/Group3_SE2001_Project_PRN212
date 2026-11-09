
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
    public class RecipeItemDisplayModel
    {
        public int ProductSizeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class RecipeManagementViewModel : ViewModelBase
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        private Product? _selectedProduct;
        private ProductSize? _selectedSize;
        private RecipeItemDisplayModel? _selectedRecipeItem;
        private Ingredient? _selectedIngredientInput;
        private string _quantityInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public ObservableCollection<Product> ProductsView { get; }
            = new ObservableCollection<Product>();

        public ObservableCollection<ProductSize> ProductSizesView { get; }
            = new ObservableCollection<ProductSize>();

        public ObservableCollection<RecipeItemDisplayModel> RecipeItemsView { get; }
            = new ObservableCollection<RecipeItemDisplayModel>();

        public ObservableCollection<Ingredient> AllIngredientsList { get; }
            = new ObservableCollection<Ingredient>();

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    StatusMessage = string.Empty;
                    _ = LoadSizesForSelectedProductAsync();
                }
            }
        }

        public ProductSize? SelectedSize
        {
            get => _selectedSize;
            set
            {
                if (SetProperty(ref _selectedSize, value))
                {
                    _ = LoadRecipeForSelectedProductAsync();
                }
            }
        }

        public RecipeItemDisplayModel? SelectedRecipeItem
        {
            get => _selectedRecipeItem;
            set
            {
                if (SetProperty(ref _selectedRecipeItem, value)
                    && value != null)
                {
                    QuantityInput = value.QuantityUsed.ToString();

                    SelectedIngredientInput =
                        AllIngredientsList.FirstOrDefault(i =>
                            i.IngredientId == value.IngredientId);
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

        public RecipeManagementViewModel(
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;

            LoadRecipeCommand = new RelayCommand(
                async () => await LoadRecipeForSelectedProductAsync());

            SaveRecipeCommand = new RelayCommand(
                async () => await SaveRecipeAsync());

            DeleteRecipeCommand = new RelayCommand(
                async () => await DeleteRecipeAsync(),
                () => SelectedRecipeItem != null);

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                using var db = _contextFactory();

                var ingredients = await db.Ingredients
                    .AsNoTracking()
                    .OrderBy(i => i.IngredientName)
                    .ToListAsync();

                AllIngredientsList.Clear();

                foreach (var ing in ingredients)
                {
                    AllIngredientsList.Add(ing);
                }

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
                StatusMessage =
                    $"Lỗi tải dữ liệu: {ex.Message}";

                MessageBox.Show(
                    $"Lỗi chi tiết: {ex}",
                    "Debug Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSizesForSelectedProductAsync()
        {
            var product = SelectedProduct;

            SelectedSize = null;
            ProductSizesView.Clear();
            RecipeItemsView.Clear();

            if (product == null)
                return;

            try
            {
                using var db = _contextFactory();

                var sizes = await db.ProductSizes
                    .Where(s => s.ProductId == product.ProductId)
                    .AsNoTracking()
                    .OrderBy(s => s.ProductSizeId)
                    .ToListAsync();

                if (SelectedProduct?.ProductId != product.ProductId)
                    return;

                foreach (var size in sizes)
                {
                    ProductSizesView.Add(size);
                }

                SelectedSize = ProductSizesView.FirstOrDefault();
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Lỗi tải size: {ex.Message}";
            }
        }

        private async Task LoadRecipeForSelectedProductAsync()
        {
            RecipeItemsView.Clear();

            var sizeId = SelectedSize?.ProductSizeId;

            if (!sizeId.HasValue)
                return;

            try
            {
                using var db = _contextFactory();

                var recipes = await db.ProductRecipes
                    .Include(r => r.Ingredient)
                    .Where(r =>
                        r.ProductSizeId == sizeId.Value)
                    .AsNoTracking()
                    .ToListAsync();

                if (SelectedSize?.ProductSizeId != sizeId.Value)
                    return;

                foreach (var rec in recipes)
                {
                    RecipeItemsView.Add(
                        new RecipeItemDisplayModel
                        {
                            ProductSizeId = rec.ProductSizeId,
                            IngredientId = rec.IngredientId,
                            IngredientName =
                                rec.Ingredient?.IngredientName ?? "N/A",
                            QuantityUsed = rec.QuantityUsed,
                            Unit = rec.Ingredient?.Unit ?? ""
                        });
                }
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Lỗi tải công thức: {ex.Message}";
            }
        }

        private async Task SaveRecipeAsync()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn sản phẩm bên cột trái!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (SelectedIngredientInput == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn nguyên liệu!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!decimal.TryParse(
                    QuantityInput,
                    out decimal quantity)
                || quantity <= 0)
            {
                MessageBox.Show(
                    "Vui lòng nhập số lượng định lượng lớn hơn 0!",
                    "Lỗi nhập liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                using var db = _contextFactory();

                var selectedSizeId =
                    SelectedSize?.ProductSizeId;

                ProductSize? productSize =
                    selectedSizeId.HasValue
                        ? await db.ProductSizes
                            .FirstOrDefaultAsync(s =>
                                s.ProductSizeId ==
                                    selectedSizeId.Value &&
                                s.ProductId ==
                                    SelectedProduct.ProductId)
                        : null;

                if (selectedSizeId.HasValue &&
                    productSize == null)
                {
                    throw new InvalidOperationException(
                        "Size được chọn không thuộc sản phẩm.");
                }

                if (productSize == null)
                {
                    bool hasSize = await db.ProductSizes
                        .AnyAsync(s =>
                            s.ProductId ==
                                SelectedProduct.ProductId);

                    if (hasSize)
                    {
                        MessageBox.Show(
                            "Vui lòng chọn size trước khi lưu công thức!",
                            "Thông báo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    productSize = new ProductSize
                    {
                        ProductId = SelectedProduct.ProductId,
                        SizeName = "Standard",
                        Price = 0
                    };

                    db.ProductSizes.Add(productSize);
                    await db.SaveChangesAsync();
                }

                var existingRecipe =
                    await db.ProductRecipes
                        .FirstOrDefaultAsync(r =>
                            r.ProductSizeId ==
                                productSize.ProductSizeId &&
                            r.IngredientId ==
                                SelectedIngredientInput.IngredientId);

                if (existingRecipe != null)
                {
                    existingRecipe.QuantityUsed = quantity;

                    StatusMessage =
                        "Cập nhật định lượng thành công!";
                }
                else
                {
                    var newRecipe = new ProductRecipe
                    {
                        ProductSizeId = productSize.ProductSizeId,
                        IngredientId =
                            SelectedIngredientInput.IngredientId,
                        QuantityUsed = quantity
                    };

                    db.ProductRecipes.Add(newRecipe);

                    StatusMessage =
                        "Thêm nguyên liệu vào công thức thành công!";
                }

                await db.SaveChangesAsync();

                QuantityInput = string.Empty;
                SelectedIngredientInput = null;

                if (SelectedSize == null)
                {
                    await LoadSizesForSelectedProductAsync();
                }
                else
                {
                    await LoadRecipeForSelectedProductAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi lưu công thức: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DeleteRecipeAsync()
        {
            if (SelectedRecipeItem == null)
                return;

            var confirm = MessageBox.Show(
                "Bạn có chắc muốn xóa nguyên liệu này khỏi công thức?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = _contextFactory();

                var entity = await db.ProductRecipes.FindAsync(
                    SelectedRecipeItem.ProductSizeId,
                    SelectedRecipeItem.IngredientId);

                if (entity != null)
                {
                    db.ProductRecipes.Remove(entity);

                    await db.SaveChangesAsync();

                    StatusMessage =
                        "Đã xóa nguyên liệu khỏi công thức.";

                    SelectedRecipeItem = null;

                    await LoadRecipeForSelectedProductAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xóa: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
