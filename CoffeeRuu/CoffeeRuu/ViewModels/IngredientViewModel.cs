
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class IngredientViewModel : INotifyPropertyChanged
    {
        private readonly InventoryService _inventoryService;

        // Danh sách nguyên liệu
        public ObservableCollection<Ingredient> Ingredients
        { get; } = new();

        // Danh sách hiển thị sau khi lọc
        public ICollectionView IngredientsView { get; }

        // Nguyên liệu được chọn
        private Ingredient? _selectedIngredient;

        public Ingredient? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (_selectedIngredient == value)
                    return;

                _selectedIngredient = value;
                OnPropertyChanged();

                if (value != null)
                {
                    IngredientNameInput = value.IngredientName;
                    UnitInput = value.Unit;
                    MinimumStockText =
                        value.MinimumStock.ToString(
                            CultureInfo.CurrentCulture);
                }
                else
                {
                    ClearInputs();
                }

                OnPropertyChanged(nameof(CanEditUnit));
                OnPropertyChanged(nameof(ToggleButtonText));
                UpdateCommands();
            }
        }

        // Form nhập liệu
        private string _ingredientNameInput = "";

        public string IngredientNameInput
        {
            get => _ingredientNameInput;
            set
            {
                _ingredientNameInput = value;
                OnPropertyChanged();
            }
        }

        private string _unitInput = "";

        public string UnitInput
        {
            get => _unitInput;
            set
            {
                _unitInput = value;
                OnPropertyChanged();
            }
        }

        private string _minimumStockText = "0";

        public string MinimumStockText
        {
            get => _minimumStockText;
            set
            {
                _minimumStockText = value;
                OnPropertyChanged();
            }
        }

        // Tìm kiếm nguyên liệu
        private string _searchText = "";

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                IngredientsView.Refresh();
            }
        }

        // Trạng thái xử lý
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                UpdateCommands();
            }
        }

        private string _statusMessage = "";

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool CanEditUnit => SelectedIngredient == null;

        public string ToggleButtonText =>
            SelectedIngredient?.IsActive == true
                ? "Ngừng sử dụng"
                : "Kích hoạt lại";

        // Commands
        public RelayCommand LoadCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }
        public RelayCommand ClearCommand { get; }

        // Constructor
        public IngredientViewModel(
            InventoryService inventoryService)
        {
            _inventoryService = inventoryService;

            IngredientsView =
                CollectionViewSource.GetDefaultView(Ingredients);

            IngredientsView.Filter = FilterIngredient;

            LoadCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    LoadIngredientsAsync),
                () => !IsBusy);

            AddCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    AddIngredientAsync),
                () => !IsBusy && SelectedIngredient == null);

            UpdateCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    UpdateIngredientAsync),
                () => !IsBusy && SelectedIngredient != null);

            ToggleActiveCommand = new RelayCommand(
                async () => await ExecuteSafeAsync(
                    ToggleActiveAsync),
                () => !IsBusy && SelectedIngredient != null);

            ClearCommand = new RelayCommand(
                ClearForm,
                () => !IsBusy);

            // Tải danh sách khi ViewModel được tạo
            _ = ExecuteSafeAsync(LoadIngredientsAsync);
        }

        // 1. Lấy danh sách nguyên liệu
        private async Task LoadIngredientsAsync()
        {
            int? selectedId = SelectedIngredient?.IngredientId;

            var items = await _inventoryService
                .GetIngredientsAsync();

            Ingredients.Clear();

            foreach (var item in items)
            {
                Ingredients.Add(item);
            }

            SelectedIngredient = selectedId.HasValue
                ? Ingredients.FirstOrDefault(
                    i => i.IngredientId == selectedId.Value)
                : null;

            IngredientsView.Refresh();
        }

        // 2. Thêm nguyên liệu
        private async Task AddIngredientAsync()
        {
            decimal minimumStock = ParseMinimumStock();

            await _inventoryService.AddIngredientAsync(
                IngredientNameInput,
                UnitInput,
                minimumStock);

            await LoadIngredientsAsync();

            ClearForm();

            StatusMessage = "Thêm nguyên liệu thành công.";
        }

        // 3. Sửa nguyên liệu
        private async Task UpdateIngredientAsync()
        {
            if (SelectedIngredient == null)
                return;

            decimal minimumStock = ParseMinimumStock();

            int id = SelectedIngredient.IngredientId;

            // Giữ nguyên đơn vị của nguyên liệu đã có
            await _inventoryService.UpdateIngredientAsync(
                id,
                IngredientNameInput,
                SelectedIngredient.Unit,
                minimumStock);

            await LoadIngredientsAsync();

            StatusMessage = "Cập nhật nguyên liệu thành công.";
        }

        // 4. Thay đổi trạng thái nguyên liệu
        private async Task ToggleActiveAsync()
        {
            if (SelectedIngredient == null)
                return;

            bool newStatus = !SelectedIngredient.IsActive;

            await _inventoryService.SetIngredientActiveAsync(
                SelectedIngredient.IngredientId,
                newStatus);

            await LoadIngredientsAsync();

            StatusMessage = newStatus
                ? "Đã kích hoạt nguyên liệu."
                : "Đã ngừng sử dụng nguyên liệu.";
        }

        // 5. Lọc nguyên liệu theo tên
        private bool FilterIngredient(object obj)
        {
            if (obj is not Ingredient ingredient)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return ingredient.IngredientName.Contains(
                SearchText.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        // 6. Chuyển ngưỡng tồn kho sang decimal
        private decimal ParseMinimumStock()
        {
            if (!decimal.TryParse(
                MinimumStockText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal result))
            {
                throw new ArgumentException(
                    "Ngưỡng tồn kho phải là một số hợp lệ.");
            }

            return result;
        }

        // 7. Xóa form
        private void ClearInputs()
        {
            IngredientNameInput = "";
            UnitInput = "";
            MinimumStockText = "0";
        }

        private void ClearForm()
        {
            SelectedIngredient = null;
            ClearInputs();
        }

        // 8. Xử lý các thao tác bất đồng bộ
        private async Task ExecuteSafeAsync(Func<Task> action)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "";

                await action();
            }
            catch (ArgumentException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (KeyNotFoundException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = "Có lỗi xảy ra: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // 9. Cập nhật trạng thái Command
        private void UpdateCommands()
        {
            LoadCommand?.RaiseCanExecuteChanged();
            AddCommand?.RaiseCanExecuteChanged();
            UpdateCommand?.RaiseCanExecuteChanged();
            ToggleActiveCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }

        // 10. Thông báo thay đổi Property
        public event PropertyChangedEventHandler?
            PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
