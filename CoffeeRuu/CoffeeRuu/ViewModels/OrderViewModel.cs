using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels;

public class OrderViewModel : NotifyBase
{
    private readonly OrderService _service;

    public ObservableCollection<MenuChoice> Menu { get; } = new();
    public ObservableCollection<TableChoice> Tables { get; } = new();
    public ObservableCollection<CartLine> Cart { get; } = new();
    public ObservableCollection<OrderListRow> Orders { get; } = new();

    private MenuChoice? _selectedMenu;

    public MenuChoice? SelectedMenu
    {
        get => _selectedMenu;
        set => Set(ref _selectedMenu, value);
    }

    private TableChoice? _selectedTable;

    public TableChoice? SelectedTable
    {
        get => _selectedTable;
        set => Set(ref _selectedTable, value);
    }

    private CartLine? _selectedCart;

    public CartLine? SelectedCart
    {
        get => _selectedCart;
        set => Set(ref _selectedCart, value);
    }

    private OrderListRow? _selectedOrder;

    public OrderListRow? SelectedOrder
    {
        get => _selectedOrder;
        set => Set(ref _selectedOrder, value);
    }

    private string _orderType = "Takeaway";

    public string OrderType
    {
        get => _orderType;
        set
        {
            if (Set(ref _orderType, value))
                Notify(nameof(IsDineIn));
        }
    }

    public bool IsDineIn => OrderType == "DineIn";

    private string _quantityText = "1";

    public string QuantityText
    {
        get => _quantityText;
        set => Set(ref _quantityText, value);
    }

    private string _employeeIdText = "";

    public string EmployeeIdText
    {
        get => _employeeIdText;
        set => Set(ref _employeeIdText, value);
    }

    private string _itemNote = "";

    public string ItemNote
    {
        get => _itemNote;
        set => Set(ref _itemNote, value);
    }

    private string _orderNote = "";

    public string OrderNote
    {
        get => _orderNote;
        set => Set(ref _orderNote, value);
    }

    private string _message = "";

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public decimal CartTotal =>
        Cart.Sum(i => i.LineTotal);

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CompleteCommand { get; }
    public ICommand CancelCommand { get; }

    public OrderViewModel(OrderService service)
    {
        _service = service;

        AddCommand = new RelayCommand(AddItem);

        RemoveCommand = new RelayCommand(RemoveItem);

        RefreshCommand = new RelayCommand(
            async () => await RunAsync(LoadAsync));

        CreateCommand = new RelayCommand(
            async () => await RunAsync(CreateAsync));

        CompleteCommand = new RelayCommand(
            async () => await RunAsync(CompleteAsync));

        CancelCommand = new RelayCommand(
            async () => await RunAsync(CancelAsync));
    }

    public Task InitializeAsync()
    {
        return RunAsync(LoadAsync);
    }

    // Lấy EmployeeId
    private int EmployeeId()
    {
        if (!int.TryParse(EmployeeIdText, out int id) ||
            id <= 0)
        {
            throw new InvalidOperationException(
                "Nhập EmployeeId hợp lệ.");
        }

        return id;
    }

    // Thêm món vào giỏ
    private void AddItem()
    {
        if (IsBusy)
            return;

        if (SelectedMenu == null)
        {
            Message = "Hãy chọn món.";
            return;
        }

        if (!int.TryParse(QuantityText, out int quantity) ||
            quantity < 1 || quantity > 100)
        {
            Message = "Số lượng phải từ 1 đến 100.";
            return;
        }

        if (ItemNote.Length > 300)
        {
            Message = "Ghi chú món quá dài.";
            return;
        }

        Cart.Add(new CartLine
        {
            ProductSizeId = SelectedMenu.ProductSizeId,
            ProductName = SelectedMenu.ProductName,
            SizeName = SelectedMenu.SizeName,
            Quantity = quantity,
            UnitPrice = SelectedMenu.Price,
            Note = ItemNote
        });

        ItemNote = "";
        Message = "Đã thêm món vào giỏ.";

        Notify(nameof(CartTotal));
    }

    // Xóa món khỏi giỏ
    private void RemoveItem()
    {
        if (IsBusy)
            return;

        if (SelectedCart == null)
        {
            Message = "Chọn món cần xóa.";
            return;
        }

        Cart.Remove(SelectedCart);

        Notify(nameof(CartTotal));

        Message = "Đã xóa món.";
    }

    // Load menu, bàn và đơn hàng
    private async Task LoadAsync()
    {
        var menu = await _service.GetMenuAsync();
        var tables = await _service.GetTablesAsync();
        var orders = await _service.GetOrdersAsync();

        Replace(Menu, menu);
        Replace(Tables, tables);
        Replace(Orders, orders);

        SelectedMenu = Menu.FirstOrDefault();
    }

    // Tạo đơn hàng
    private async Task CreateAsync()
    {
        int employeeId = EmployeeId();

        var order = await _service.CreateOrderAsync(
            Cart.ToList(),
            OrderType,
            SelectedTable?.TableId,
            employeeId,
            OrderNote
        );

        Cart.Clear();
        OrderNote = "";

        Notify(nameof(CartTotal));

        await LoadAsync();

        Message =
            $"Đã tạo đơn {order.OrderCode}. " +
            $"Tổng tiền: {order.TotalAmount:N0} đ.";
    }

    // Bàn giao đơn
    private async Task CompleteAsync()
    {
        if (SelectedOrder == null)
            throw new InvalidOperationException(
                "Hãy chọn đơn cần bàn giao.");

        await _service.CompleteOrderAsync(
            SelectedOrder.OrderId,
            EmployeeId()
        );

        await LoadAsync();

        Message = "Đã bàn giao đơn hàng.";
    }

    // Hủy đơn
    private async Task CancelAsync()
    {
        if (SelectedOrder == null)
            throw new InvalidOperationException(
                "Hãy chọn đơn cần hủy.");

        await _service.CancelNewOrderAsync(
            SelectedOrder.OrderId,
            EmployeeId()
        );

        await LoadAsync();

        Message = "Đã hủy đơn hàng.";
    }

    // Xử lý lỗi
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Message = "";

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Message = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Replace<T>(
        ObservableCollection<T> collection,
        IEnumerable<T> items)
    {
        collection.Clear();

        foreach (var item in items)
            collection.Add(item);
    }
}