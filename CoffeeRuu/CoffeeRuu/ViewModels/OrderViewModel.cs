using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class OrderViewModel : NotifyBase
    {
        private readonly OrderService _service;

        public ObservableCollection<MenuChoice> Menu { get; } = new();
        public ObservableCollection<TableChoice> Tables { get; } = new();
        public ObservableCollection<CartLine> Cart { get; } = new();
        public ObservableCollection<OrderListRow> Orders { get; } = new();

        public string[] OrderTypes { get; } =
            new[] { "Takeaway", "DineIn" };

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

        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        private string _quantityText = "1";

        public string QuantityText
        {
            get => _quantityText;
            set => Set(ref _quantityText, value);
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
            Cart.Sum(x => x.LineTotal);

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

            CreateCommand = new RelayCommand(
                async () => await RunAsync(CreateAsync));

            RefreshCommand = new RelayCommand(
                async () => await RunAsync(LoadAsync));

            CompleteCommand = new RelayCommand(
                async () => await RunAsync(CompleteAsync));

            CancelCommand = new RelayCommand(
                async () => await RunAsync(CancelAsync));
        }

        public Task InitializeAsync()
        {
            return RunAsync(LoadAsync);
        }

        // =================================
        // TẢI DỮ LIỆU
        // =================================

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

        // =================================
        // THÊM MÓN VÀO GIỎ
        // =================================

        private void AddItem()
        {
            if (IsBusy)
                return;

            if (SelectedMenu == null)
            {
                Message = "Vui lòng chọn sản phẩm.";
                return;
            }

            if (!int.TryParse(QuantityText, out int qty) ||
                qty < 1 || qty > 100)
            {
                Message = "Số lượng phải từ 1 đến 100.";
                return;
            }

            if (ItemNote.Length > 300)
            {
                Message = "Ghi chú món tối đa 300 ký tự.";
                return;
            }

            Cart.Add(new CartLine
            {
                ProductSizeId = SelectedMenu.ProductSizeId,
                ProductName = SelectedMenu.ProductName,
                SizeName = SelectedMenu.SizeName,
                UnitPrice = SelectedMenu.Price,
                Quantity = qty,
                Note = ItemNote.Trim()
            });

            QuantityText = "1";
            ItemNote = "";

            Notify(nameof(CartTotal));

            Message = "Đã thêm món vào giỏ.";
        }

        // =================================
        // XÓA MÓN KHỎI GIỎ
        // =================================

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
            SelectedCart = null;

            Notify(nameof(CartTotal));

            Message = "Đã xóa món.";
        }

        // =================================
        // TẠO ĐƠN HÀNG
        // =================================

        private async Task CreateAsync()
        {
            int employeeId = GetEmployeeId();

            var order = await _service.CreateOrderAsync(
                Cart.ToList(),
                OrderType,
                SelectedTable?.TableId,
                employeeId,
                OrderNote
            );

            Cart.Clear();

            OrderNote = "";
            ItemNote = "";

            Notify(nameof(CartTotal));

            await LoadAsync();

            Message =
                $"Đã tạo đơn {order.OrderCode}. " +
                $"Tổng: {order.TotalAmount:N0} đ.";
        }

        // =================================
        // BÀN GIAO ĐƠN
        // =================================

        private async Task CompleteAsync()
        {
            if (SelectedOrder == null)
                throw new Exception("Chọn đơn cần bàn giao.");

            await _service.CompleteOrderAsync(
                SelectedOrder.OrderId,
                GetEmployeeId());

            await LoadAsync();

            Message = "Đã bàn giao đơn hàng.";
        }

        // =================================
        // HỦY ĐƠN
        // =================================

        private async Task CancelAsync()
        {
            if (SelectedOrder == null)
                throw new Exception("Chọn đơn cần hủy.");

            await _service.CancelNewOrderAsync(
                SelectedOrder.OrderId,
                GetEmployeeId());

            await LoadAsync();

            Message = "Đã hủy đơn hàng.";
        }

        // =================================
        // HÀM HỖ TRỢ
        // =================================

        private int GetEmployeeId()
        {
            if (!int.TryParse(EmployeeIdText, out int id) ||
                id <= 0)
            {
                throw new Exception(
                    "Nhập EmployeeId hợp lệ.");
            }

            return id;
        }

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
                Message = "Lỗi: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static void Replace<T>(
            ObservableCollection<T> target,
            IEnumerable<T> values)
        {
            target.Clear();

            foreach (var item in values)
            {
                target.Add(item);
            }
        }
    }
}
