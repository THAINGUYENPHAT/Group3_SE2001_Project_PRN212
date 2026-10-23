
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CoffeeRuu.ViewModels;

public class StockReceiptLineViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Ingredient> AvailableIngredients
    { get; } = new();

    private Ingredient? _selectedIngredient;
    private string _quantityText = "";
    private string _unitCostText = "";

    public Ingredient? SelectedIngredient
    {
        get => _selectedIngredient;
        set
        {
            _selectedIngredient = value;
            OnPropertyChanged();
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            _quantityText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public string UnitCostText
    {
        get => _unitCostText;
        set
        {
            _unitCostText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    // Chuyển dữ liệu người dùng nhập thành decimal
    public bool TryGetValues(
        out decimal quantity,
        out decimal unitCost)
    {
        bool validQuantity = decimal.TryParse(
            QuantityText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out quantity);

        bool validCost = decimal.TryParse(
            UnitCostText,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out unitCost);

        return validQuantity
            && validCost
            && quantity > 0
            && unitCost >= 0
            && decimal.Round(quantity, 3) == quantity
            && decimal.Round(unitCost, 2) == unitCost;
    }

    // Thành tiền của dòng nguyên liệu
    public decimal LineTotal
    {
        get
        {
            if (!TryGetValues(out var quantity, out var unitCost))
                return 0;

            try
            {
                return quantity * unitCost;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
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

public class StockReceiptViewModel : INotifyPropertyChanged
{
    private readonly StockReceiptService _service;
    private readonly int _currentEmployeeId;

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Ingredient> Ingredients { get; } = new();

    public ObservableCollection<StockReceiptLineViewModel> Lines
    { get; } = new();

    public ObservableCollection<StockReceipt> RecentReceipts
    { get; } = new();

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            _selectedSupplier = value;
            OnPropertyChanged();
        }
    }

    private StockReceiptLineViewModel? _selectedLine;
    public StockReceiptLineViewModel? SelectedLine
    {
        get => _selectedLine;
        set
        {
            _selectedLine = value;
            OnPropertyChanged();
            RemoveLineCommand.RaiseCanExecuteChanged();
        }
    }

    private string _note = "";
    public string Note
    {
        get => _note;
        set
        {
            _note = value;
            OnPropertyChanged();
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

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();

            AddLineCommand.RaiseCanExecuteChanged();
            RemoveLineCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal TotalCost
    {
        get
        {
            try
            {
                return decimal.Round(
                    Lines.Sum(x => x.LineTotal),
                    2,
                    MidpointRounding.AwayFromZero);
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
    }

    public RelayCommand AddLineCommand { get; }
    public RelayCommand RemoveLineCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public StockReceiptViewModel(
        StockReceiptService service,
        int currentEmployeeId)
    {
        _service = service;
        _currentEmployeeId = currentEmployeeId;

        AddLineCommand = new RelayCommand(
            AddLine,
            () => !IsBusy);

        RemoveLineCommand = new RelayCommand(
            RemoveSelectedLine,
            () => !IsBusy && SelectedLine != null);

        SaveCommand = new RelayCommand(
            async () => await SaveAsync(),
            () => !IsBusy);

        RefreshCommand = new RelayCommand(
            async () => await LoadAsync(),
            () => !IsBusy);

        AddLine();
    }

    // Thêm dòng nguyên liệu

    private void AddLine()
    {
        if (IsBusy)
            return;

        var line = new StockReceiptLineViewModel();

        // Cung cấp danh sách nguyên liệu cho dòng mới
        foreach (var ingredient in Ingredients)
        {
            line.AvailableIngredients.Add(ingredient);
        }

        line.PropertyChanged += OnLinePropertyChanged;

        Lines.Add(line);

        OnPropertyChanged(nameof(TotalCost));
    }


    // Xóa dòng đang chọn
    private void RemoveSelectedLine()
    {
        if (IsBusy || SelectedLine == null)
            return;

        SelectedLine.PropertyChanged -= OnLinePropertyChanged;

        Lines.Remove(SelectedLine);
        SelectedLine = null;

        OnPropertyChanged(nameof(TotalCost));
    }

    // Cập nhật tổng tiền khi số lượng hoặc đơn giá thay đổi
    private void OnLinePropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StockReceiptLineViewModel.LineTotal))
        {
            OnPropertyChanged(nameof(TotalCost));
        }
    }

    // Xóa dữ liệu phiếu nhập sau khi lưu thành công
    private void ResetForm()
    {
        SelectedSupplier = null;
        SelectedLine = null;
        Note = "";

        foreach (var line in Lines)
        {
            line.PropertyChanged -= OnLinePropertyChanged;
        }

        Lines.Clear();
        AddLine();

        OnPropertyChanged(nameof(TotalCost));
    }

    // Lấy dữ liệu từ database
    private async Task ReloadDataAsync()
    {
        var selectedSupplierId = SelectedSupplier?.SupplierId;

        var lineIngredientIds = Lines
            .Select(x => x.SelectedIngredient?.IngredientId)
            .ToList();

        // Thực hiện lần lượt vì Repository dùng cùng DbContext
        var suppliers = await _service.GetActiveSuppliersAsync();
        var ingredients = await _service.GetActiveIngredientsAsync();
        var receipts = await _service.GetRecentReceiptsAsync();

        Suppliers.Clear();
        foreach (var supplier in suppliers)
            Suppliers.Add(supplier);

        Ingredients.Clear();
        foreach (var ingredient in ingredients)
            Ingredients.Add(ingredient);

        RecentReceipts.Clear();
        foreach (var receipt in receipts)
            RecentReceipts.Add(receipt);

        SelectedSupplier = Suppliers.FirstOrDefault(
            s => s.SupplierId == selectedSupplierId);


        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            var ingredientId = lineIngredientIds[i];

            line.AvailableIngredients.Clear();

            foreach (var ingredient in Ingredients)
            {
                line.AvailableIngredients.Add(ingredient);
            }

            line.SelectedIngredient =
                line.AvailableIngredients.FirstOrDefault(
                    x => x.IngredientId == ingredientId);
        }

    }

    // Được gọi khi mở trang hoặc nhấn Refresh
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Đang tải dữ liệu...";

        try
        {
            await ReloadDataAsync();

            StatusMessage = "Đã tải dữ liệu nhập hàng.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Không thể tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Xác nhận nhập hàng
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        if (SelectedSupplier == null)
        {
            StatusMessage = "Vui lòng chọn nhà cung cấp.";
            return;
        }

        if (_currentEmployeeId <= 0)
        {
            StatusMessage = "Không xác định được nhân viên đăng nhập.";
            return;
        }

        if (Lines.Count == 0)
        {
            StatusMessage = "Vui lòng thêm ít nhất một nguyên liệu.";
            return;
        }

        var request = new CreateStockReceiptRequest
        {
            SupplierId = SelectedSupplier.SupplierId,
            ReceivedBy = _currentEmployeeId,
            Note = Note,
            Items = new List<StockReceiptItemRequest>()
        };

        // Kiểm tra từng dòng và tạo request
        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];

            if (line.SelectedIngredient == null)
            {
                StatusMessage =
                    $"Dòng {i + 1}: Vui lòng chọn nguyên liệu.";
                return;
            }

            if (!line.TryGetValues(
                out var quantity,
                out var unitCost))
            {
                StatusMessage =
                    $"Dòng {i + 1}: Số lượng hoặc đơn giá không hợp lệ.";
                return;
            }

            request.Items.Add(new StockReceiptItemRequest
            {
                IngredientId = line.SelectedIngredient.IngredientId,
                Quantity = quantity,
                UnitCost = unitCost
            });
        }

        IsBusy = true;
        StatusMessage = "Đang lưu phiếu nhập...";

        int receiptId;

        try
        {
            receiptId = await _service.CreateStockReceiptAsync(request);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Nhập hàng thất bại: {ex.Message}";
            IsBusy = false;
            return;
        }

        // Đến đây phiếu nhập đã được lưu thành công
        ResetForm();

        try
        {
            await ReloadDataAsync();
            StatusMessage = $"Đã nhập hàng thành công! Mã phiếu: #{receiptId}";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Đã lưu phiếu #{receiptId}, nhưng không thể tải lại dữ liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
