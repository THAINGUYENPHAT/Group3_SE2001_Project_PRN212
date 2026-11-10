
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    // Class dùng để hiển thị nguyên liệu trong công thức
    public class RecipeItemDisplayModel
    {
        public int ProductSizeId { get; set; }

        public int IngredientId { get; set; }

        public string IngredientName { get; set; } = string.Empty;

        public decimal QuantityUsed { get; set; }

        public string Unit { get; set; } = string.Empty;
    }

    public class RecipeManagementViewModel : BaseViewModel
    {
        private readonly RecipeService _recipeService;
        private readonly IMessageDialogService _dialogService;

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
            RecipeService recipeService,
            IMessageDialogService dialogService)
        {
            _recipeService = recipeService;
            _dialogService = dialogService;

            LoadRecipeCommand = new RelayCommand(
                async () => await LoadRecipeForSelectedProductAsync());

            SaveRecipeCommand = new RelayCommand(
                async () => await SaveRecipeAsync());

            DeleteRecipeCommand = new RelayCommand(
                async () => await DeleteRecipeAsync(),
                () => SelectedRecipeItem != null);

            _ = InitializeDataAsync();
        }

        // Tải danh sách sản phẩm và nguyên liệu
        private async Task InitializeDataAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var ingredients =
                    await _recipeService.GetIngredientsAsync();

                AllIngredientsList.Clear();

                foreach (var ingredient in ingredients)
                {
                    AllIngredientsList.Add(ingredient);
                }

                var products =
                    await _recipeService.GetProductsAsync();

                ProductsView.Clear();

                foreach (var product in products)
                {
                    ProductsView.Add(product);
                }

                if (ProductsView.Count > 0)
                {
                    SelectedProduct = ProductsView[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";

                _dialogService.ShowError(
                    "Lỗi tải dữ liệu",
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Tải danh sách size theo sản phẩm được chọn
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
                var sizes = await _recipeService.GetSizesAsync(
                    product.ProductId);

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
                StatusMessage = $"Lỗi tải size: {ex.Message}";

                _dialogService.ShowError(
                    "Lỗi tải size",
                    ex.Message);
            }
        }

        // Tải công thức theo size được chọn
        private async Task LoadRecipeForSelectedProductAsync()
        {
            RecipeItemsView.Clear();

            var sizeId = SelectedSize?.ProductSizeId;

            if (!sizeId.HasValue)
                return;

            try
            {
                var recipes =
                    await _recipeService.GetRecipesAsync(sizeId.Value);

                if (SelectedSize?.ProductSizeId != sizeId.Value)
                    return;

                foreach (var recipe in recipes)
                {
                    RecipeItemsView.Add(
                        new RecipeItemDisplayModel
                        {
                            ProductSizeId = recipe.ProductSizeId,
                            IngredientId = recipe.IngredientId,
                            IngredientName =
                                recipe.Ingredient?.IngredientName ?? "N/A",
                            QuantityUsed = recipe.QuantityUsed,
                            Unit = recipe.Ingredient?.Unit ?? ""
                        });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải công thức: {ex.Message}";

                _dialogService.ShowError(
                    "Lỗi tải công thức",
                    ex.Message);
            }
        }

        // Thêm hoặc cập nhật nguyên liệu trong công thức
        private async Task SaveRecipeAsync()
        {
            if (SelectedProduct == null)
            {
                _dialogService.ShowWarning(
                    "Thông báo",
                    "Vui lòng chọn sản phẩm bên cột trái!");

                return;
            }

            if (SelectedIngredientInput == null)
            {
                _dialogService.ShowWarning(
                    "Thông báo",
                    "Vui lòng chọn nguyên liệu!");

                return;
            }

            if (!decimal.TryParse(QuantityInput, out decimal quantity)
                || quantity <= 0)
            {
                _dialogService.ShowWarning(
                    "Lỗi nhập liệu",
                    "Vui lòng nhập số lượng định lượng lớn hơn 0!");

                return;
            }

            try
            {
                string? message = await _recipeService.SaveRecipeAsync(
                    SelectedProduct.ProductId,
                    SelectedSize?.ProductSizeId,
                    SelectedIngredientInput.IngredientId,
                    quantity);

                if (message == null)
                {
                    _dialogService.ShowWarning(
                        "Thông báo",
                        "Vui lòng chọn size trước khi lưu công thức!");

                    return;
                }

                StatusMessage = message;

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
                _dialogService.ShowError(
                    "Lỗi lưu công thức",
                    ex.Message);
            }
        }

        // Xóa nguyên liệu khỏi công thức
        private async Task DeleteRecipeAsync()
        {
            if (SelectedRecipeItem == null)
                return;

            bool confirm = _dialogService.Confirm(
                "Xác nhận",
                "Bạn có chắc muốn xóa nguyên liệu này khỏi công thức?");

            if (!confirm)
                return;

            try
            {
                bool deleted = await _recipeService.DeleteRecipeAsync(
                    SelectedRecipeItem.ProductSizeId,
                    SelectedRecipeItem.IngredientId);

                if (deleted)
                {
                    StatusMessage =
                        "Đã xóa nguyên liệu khỏi công thức.";

                    SelectedRecipeItem = null;

                    await LoadRecipeForSelectedProductAsync();
                }
                else
                {
                    _dialogService.ShowWarning(
                        "Thông báo",
                        "Không tìm thấy nguyên liệu trong công thức.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    "Lỗi khi xóa",
                    ex.Message);
            }
        }
    }
}
