using CoffeeRuu.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class OrderViewModel : NotifyBase
    {
        private readonly OrderService _service;

        public ObservableCollection<MenuChoice> Menu { get; }
            = new();

        public ObservableCollection<TableChoice> Tables { get; }
            = new();

        public ObservableCollection<CartLine> Cart { get; }
            = new();

        public ObservableCollection<OrderListRow> Orders { get; }
            = new();

        public string[] OrderTypes { get; } =
            new[] { "Takeaway", "DineIn" };

        // Sản phẩm chọn từ menu
        private MenuChoice? _selectedMenu;

        public MenuChoice? SelectedMenu
        {
            get => _selectedMenu;
            set => Set(ref _selectedMenu, value);
        }

        // Bàn được chọn
        private TableChoice? _selectedTable;

        public TableChoice? SelectedTable
        {
            get => _selectedTable;
            set => Set(ref _selectedTable, value);
        }

        // Món được chọn trong giỏ
        private CartLine? _selectedCart;

        public CartLine? SelectedCart
        {
            get => _selectedCart;
            set => Set(ref _selectedCart, value);
        }

        // Đơn hàng được chọn
        private OrderListRow? _selectedOrder;

        public OrderListRow? SelectedOrder
        {
            get => _selectedOrder;
            set => Set(ref _selectedOrder, value);
        }

        // Loại đơn
        private string _orderType = "Takeaway";

        public string OrderType
        {
            get => _orderType;
            set
            {
                if (Set(ref _orderType, value))
                {
                    if (_orderType == "Takeaway")
                        SelectedTable = null;

                    Notify(nameof(IsDineIn));
                }
            }
        }

        public bool IsDineIn => OrderType == "DineIn";

        // Mã nhân viên demo
        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        // Số lượng
        private string _quantityText = "1";

        public string QuantityText
        {
            get => _quantityText;
            set => Set(ref _quantityText, value);
        }

        // Ghi chú món
        private string _itemNote = "";

        public string ItemNote
        {
            get => _itemNote;
            set => Set(ref _itemNote, value);
        }

        // Ghi chú đơn
        private string _orderNote = "";

        public string OrderNote
        {
            get => _orderNote;
            set => Set(ref _orderNote, value);
        }

        // Thông báo
        private string _message = "";

        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        // Trạng thái xử lý
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        // Tổng tiền
        public decimal CartTotal =>
            Cart.Sum(i => i.LineTotal);

        public string CartTotalText =>
            $"Tổng cộng: {CartTotal:N0} đ";

        // Commands
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CompleteCommand { get; }

        public OrderViewModel(OrderService service)
        {
            _service = service;

            AddCommand = new RelayCommand(AddItem);

            RemoveCommand = new RelayCommand(RemoveItem);

            CreateCommand = new RelayCommand(
                async () => await RunAsync(CreateOrderAsync));

            RefreshCommand = new RelayCommand(
                async () => await RunAsync(LoadDataAsync));

            CancelCommand = new RelayCommand(
                async () => await RunAsync(CancelOrderAsync));

            CompleteCommand = new RelayCommand(
                async () => await RunAsync(CompleteOrderAsync));
        }

        public Task InitializeAsync()
        {
            return RunAsync(LoadDataAsync);
        }

        // ===========================
        // TẢI DỮ LIỆU
        // ===========================

        private async Task LoadDataAsync()
        {
            var menu = await _service.GetMenuAsync();
            var tables = await _service.GetTablesAsync();
            var orders = await _service.GetOrdersAsync();

            Replace(Menu, menu);
            Replace(Tables, tables);
            Replace(Orders, orders);

            SelectedMenu = Menu.FirstOrDefault();
            SelectedOrder = null;
        }

        // ===========================
        // THÊM MÓN VÀO GIỎ
        // ===========================

        private void AddItem()
        {
            if (IsBusy)
                return;

            if (SelectedMenu == null)
            {
                Message = "Vui lòng chọn sản phẩm.";
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
                Message = "Ghi chú món tối đa 300 ký tự.";
                return;
            }

            Cart.Add(new CartLine
            {
                ProductSizeId =
                    SelectedMenu.ProductSizeId,

                ProductName =
                    SelectedMenu.ProductName,

                SizeName =
                    SelectedMenu.SizeName,

                UnitPrice =
                    SelectedMenu.Price,

                Quantity = quantity,
                Note = ItemNote.Trim()
            });

            QuantityText = "1";
            ItemNote = "";

            NotifyCartTotal();

            Message = "Đã thêm món vào đơn.";
        }

        // ===========================
        // XÓA MÓN
        // ===========================

        private void RemoveItem()
        {
            if (IsBusy)
                return;

            if (SelectedCart == null)
            {
                Message = "Hãy chọn món cần xóa.";
                return;
            }

            Cart.Remove(SelectedCart);
            SelectedCart = null;

            NotifyCartTotal();

            Message = "Đã xóa món khỏi đơn.";
        }

        // ===========================
        // XÁC NHẬN TẠO ĐƠN
        // ===========================

        private async Task CreateOrderAsync()
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
            QuantityText = "1";

            NotifyCartTotal();

            await LoadDataAsync();

            Message =
                $"Đã tạo đơn {order.OrderCode}. " +
                $"Tổng tiền: {order.TotalAmount:N0} đ.";
        }

        // ===========================
        // HỦY ĐƠN
        // ===========================

        private async Task CancelOrderAsync()
        {
            if (SelectedOrder == null)
                throw new InvalidOperationException(
                    "Hãy chọn đơn cần hủy.");

            int orderId = SelectedOrder.OrderId;

            await _service.CancelNewOrderAsync(
                orderId,
                GetEmployeeId());

            await LoadDataAsync();

            Message = "Đã hủy đơn hàng.";
        }

        // ===========================
        // BÀN GIAO ĐƠN
        // ===========================

        private async Task CompleteOrderAsync()
        {
            if (SelectedOrder == null)
                throw new InvalidOperationException(
                    "Hãy chọn đơn cần bàn giao.");

            int orderId = SelectedOrder.OrderId;

            await _service.CompleteOrderAsync(
                orderId,
                GetEmployeeId());

            await LoadDataAsync();

            Message = "Đã bàn giao đơn hàng.";
        }

        // ===========================
        // HÀM HỖ TRỢ
        // ===========================

        private int GetEmployeeId()
        {
            if (!int.TryParse(
                EmployeeIdText, out int employeeId) ||
                employeeId <= 0)
            {
                throw new InvalidOperationException(
                    "Vui lòng nhập EmployeeId hợp lệ.");
            }

            return employeeId;
        }

        private void NotifyCartTotal()
        {
            Notify(nameof(CartTotal));
            Notify(nameof(CartTotalText));
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
            IEnumerable<T> source)
        {
            target.Clear();

            foreach (var item in source)
                target.Add(item);
        }
    }
}
