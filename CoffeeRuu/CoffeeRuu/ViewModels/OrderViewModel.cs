
using CoffeeRuu.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class OrderViewModel : NotifyBase
    {
        private readonly OrderService _service;

        // ============================
        // DANH SÁCH DỮ LIỆU
        // ============================

        public ObservableCollection<MenuChoice> Menu { get; }
            = new();

        public ObservableCollection<TableChoice> Tables { get; }
            = new();

        public ObservableCollection<CartLine> Cart { get; }
            = new();

        public ObservableCollection<OrderListRow> Orders { get; }
            = new();

        public ObservableCollection<EmployeeChoice> Employees { get; }
            = new();

        public string[] OrderTypes { get; } =
        {
            "Takeaway",
            "DineIn"
        };

        // ============================
        // SẢN PHẨM ĐƯỢC CHỌN
        // ============================

        private MenuChoice? _selectedMenu;

        public MenuChoice? SelectedMenu
        {
            get => _selectedMenu;
            set => Set(ref _selectedMenu, value);
        }

        // ============================
        // BÀN ĐƯỢC CHỌN
        // ============================

        private TableChoice? _selectedTable;

        public TableChoice? SelectedTable
        {
            get => _selectedTable;
            set => Set(ref _selectedTable, value);
        }

        // ============================
        // MÓN TRONG GIỎ
        // ============================

        private CartLine? _selectedCart;

        public CartLine? SelectedCart
        {
            get => _selectedCart;
            set => Set(ref _selectedCart, value);
        }

        // ============================
        // ĐƠN HÀNG ĐƯỢC CHỌN
        // ============================

        private OrderListRow? _selectedOrder;

        public OrderListRow? SelectedOrder
        {
            get => _selectedOrder;
            set => Set(ref _selectedOrder, value);
        }

        // ============================
        // LOẠI ĐƠN
        // ============================

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

        public bool IsDineIn =>
            OrderType == "DineIn";

        // ============================
        // DANH SÁCH NHÂN VIÊN
        // ============================

        private EmployeeChoice? _selectedEmployee;

        public EmployeeChoice? SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

        // Giữ tương thích với giao diện tạo đơn cũ.
        // Mã người thao tác được lấy từ UserSession.
        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        // ============================
        // SỐ LƯỢNG
        // ============================

        private string _quantityText = "1";

        public string QuantityText
        {
            get => _quantityText;
            set => Set(ref _quantityText, value);
        }

        // ============================
        // GHI CHÚ MÓN
        // ============================

        private string _itemNote = "";

        public string ItemNote
        {
            get => _itemNote;
            set => Set(ref _itemNote, value);
        }

        // ============================
        // GHI CHÚ ĐƠN
        // ============================

        private string _orderNote = "";

        public string OrderNote
        {
            get => _orderNote;
            set => Set(ref _orderNote, value);
        }

        // ============================
        // THÔNG BÁO
        // ============================

        private string _message = "";

        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        // ============================
        // TRẠNG THÁI XỬ LÝ
        // ============================

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        // ============================
        // TỔNG TIỀN
        // ============================

        public decimal CartTotal =>
            Cart.Sum(i => i.LineTotal);

        public string CartTotalText =>
            $"Tổng cộng: {CartTotal:N0} đ";

        // ============================
        // BỘ LỌC ĐƠN HÀNG
        // ============================

        public ICollectionView FilteredOrders { get; }

        public string[] StatusOptions { get; } =
        {
            "Đang xử lý",
            "Tất cả",
            "New",
            "Preparing",
            "Ready",
            "Completed",
            "Cancelled"
        };

        // Mặc định ẩn Completed và Cancelled
        private string _statusFilter = "Đang xử lý";

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (Set(ref _statusFilter, value))
                    FilteredOrders.Refresh();
            }
        }

        // Tìm mã đơn
        private string _searchOrder = "";

        public string SearchOrder
        {
            get => _searchOrder;
            set
            {
                if (Set(ref _searchOrder, value))
                    FilteredOrders.Refresh();
            }
        }

        // Lọc theo ngày
        private DateTime? _filterDate;

        public DateTime? FilterDate
        {
            get => _filterDate;
            set
            {
                if (Set(ref _filterDate, value))
                    FilteredOrders.Refresh();
            }
        }

        // Kiểm tra điều kiện lọc
        private bool FilterOrder(object obj)
        {
            if (obj is not OrderListRow order)
                return false;

            string keyword =
                SearchOrder?.Trim() ?? "";

            bool matchCode =
                keyword.Length == 0 ||
                order.OrderCode.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase);

            bool matchStatus =
                StatusFilter == "Tất cả" ||

                (
                    StatusFilter == "Đang xử lý" &&
                    (
                        order.OrderStatus == "New" ||
                        order.OrderStatus == "Preparing" ||
                        order.OrderStatus == "Ready"
                    )
                ) ||

                order.OrderStatus == StatusFilter;

            bool matchDate =
                !FilterDate.HasValue ||
                order.CreatedAt.Date ==
                    FilterDate.Value.Date;

            return matchCode &&
                   matchStatus &&
                   matchDate;
        }

        // ============================
        // COMMANDS
        // ============================

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CompleteCommand { get; }

        // ============================
        // CONSTRUCTOR
        // ============================

        public OrderViewModel(OrderService service)
        {
            _service = service;

            // Tạo bộ lọc cho danh sách đơn
            FilteredOrders =
                CollectionViewSource.GetDefaultView(Orders);

            FilteredOrders.Filter = FilterOrder;

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

        // ============================
        // TẢI DỮ LIỆU
        // ============================

        private async Task LoadDataAsync()
        {
            int? previousEmployeeId =
                SelectedEmployee?.EmployeeId;

            int? previousOrderId =
                SelectedOrder?.OrderId;

            var menu = await _service.GetMenuAsync();
            var tables = await _service.GetTablesAsync();
            var orders = await _service.GetOrdersAsync();
            var employees = await _service.GetEmployeesAsync();

            Replace(Menu, menu);
            Replace(Tables, tables);
            Replace(Orders, orders);
            Replace(Employees, employees);

            // Giữ nhân viên đang chọn
            int? employeeToShow =
                previousEmployeeId ??
                UserSession.Current?.EmployeeId;

            SelectedEmployee = Employees.FirstOrDefault(e =>
                e.EmployeeId == employeeToShow);

            // Mã nhân viên đăng nhập
            EmployeeIdText =
                UserSession.Current?.EmployeeId.ToString() ?? "";

            // Giữ đơn đang chọn nếu còn
            SelectedOrder = Orders.FirstOrDefault(o =>
                o.OrderId == previousOrderId);

            if (SelectedMenu == null ||
                !Menu.Contains(SelectedMenu))
            {
                SelectedMenu = Menu.FirstOrDefault();
            }

            FilteredOrders.Refresh();
        }

        // ============================
        // THÊM MÓN VÀO GIỎ
        // ============================

        private void AddItem()
        {
            if (IsBusy)
                return;

            if (SelectedMenu == null)
            {
                Message = "Vui lòng chọn sản phẩm.";
                return;
            }

            if (!int.TryParse(
                QuantityText, out int quantity) ||
                quantity < 1 || quantity > 100)
            {
                Message =
                    "Số lượng phải từ 1 đến 100.";
                return;
            }

            if (ItemNote.Length > 300)
            {
                Message =
                    "Ghi chú món tối đa 300 ký tự.";
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

        // ============================
        // XÓA MÓN KHỎI GIỎ
        // ============================

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

        // ============================
        // TẠO ĐƠN
        // ============================

        private async Task CreateOrderAsync()
        {
            int employeeId = GetEmployeeId();

            var order = await _service.CreateOrderAsync(
                Cart.ToList(),
                OrderType,
                SelectedTable?.TableId,
                employeeId,
                OrderNote);

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

        // ============================
        // HỦY ĐƠN
        // ============================

        private async Task CancelOrderAsync()
        {
            if (SelectedOrder == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn đơn cần hủy.");
            }

            // Chỉ hủy đơn mới
            if (SelectedOrder.OrderStatus != "New")
            {
                throw new InvalidOperationException(
                    "Chỉ được hủy đơn có trạng thái New.");
            }

            int orderId = SelectedOrder.OrderId;

            await _service.CancelNewOrderAsync(
                orderId,
                GetEmployeeId());

            // Làm mới để đơn Cancelled tự ẩn
            await LoadDataAsync();

            SelectedOrder = null;

            Message = "Đã hủy đơn hàng.";
        }

        // ============================
        // BÀN GIAO ĐƠN
        // ============================

        private async Task CompleteOrderAsync()
        {
            if (SelectedOrder == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn đơn cần bàn giao.");
            }

            if (SelectedOrder.OrderStatus != "Ready")
            {
                throw new InvalidOperationException(
                    "Chỉ bàn giao khi đơn đã Ready.");
            }

            int orderId = SelectedOrder.OrderId;

            await _service.CompleteOrderAsync(
                orderId,
                GetEmployeeId());

            // Làm mới danh sách sau khi bàn giao
            await LoadDataAsync();

            // Bỏ dòng đang chọn
            SelectedOrder = null;

            Message =
                "Đã bàn giao đơn hàng thành công.";
        }

        // ============================
        // MÃ NHÂN VIÊN THỰC HIỆN
        // ============================

        private int GetEmployeeId()
        {
            var session = UserSession.Current;

            if (session == null ||
                session.EmployeeId <= 0)
            {
                throw new InvalidOperationException(
                    "Vui lòng đăng nhập trước khi thao tác.");
            }

            if (!session.CanAccess("Orders"))
            {
                throw new InvalidOperationException(
                    "Tài khoản không có quyền xử lý đơn hàng.");
            }

            return session.EmployeeId;
        }

        // ============================
        // HÀM HỖ TRỢ
        // ============================

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
