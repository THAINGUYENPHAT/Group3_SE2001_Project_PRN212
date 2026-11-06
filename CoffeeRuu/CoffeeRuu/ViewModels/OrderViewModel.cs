
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

        // ==================================
        // DANH SÁCH ĐƠN HÀNG
        // ==================================

        public ObservableCollection<OrderListRow> Orders { get; }
            = new();

        public ICollectionView FilteredOrders { get; }

        private OrderListRow? _selectedOrder;

        public OrderListRow? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (Set(ref _selectedOrder, value))
                {
                    Notify(nameof(CanComplete));
                }
            }
        }

        // ==================================
        // DANH SÁCH NHÂN VIÊN
        // ==================================

        public ObservableCollection<EmployeeChoice> Employees { get; }
            = new();

        private EmployeeChoice? _selectedEmployee;

        public EmployeeChoice? SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

        // ==================================
        // TÀI KHOẢN ĐANG ĐĂNG NHẬP
        // ==================================

        public string CurrentEmployeeDisplay
        {
            get
            {
                var session = UserSession.Current;

                if (session == null)
                    return "Chưa đăng nhập";

                return $"{session.EmployeeId} - " +
                       $"{session.FullName} " +
                       $"({session.RoleName})";
            }
        }

        // ==================================
        // PHÂN QUYỀN ORDER PROCESSING
        // ==================================

        public bool CanOperateOrders
        {
            get
            {
                var session = UserSession.Current;

                return session != null
                    && session.EmployeeId > 0
                    && string.Equals(
                        session.RoleName.Trim(),
                        "Cashier",
                        StringComparison.OrdinalIgnoreCase);
            }
        }

        public string OrderPermissionMessage
        {
            get
            {
                if (UserSession.Current == null)
                    return "Vui lòng đăng nhập.";

                return CanOperateOrders
                    ? "Bạn có quyền bàn giao đơn hàng."
                    : "Chế độ chỉ xem - chỉ Cashier được bàn giao.";
            }
        }

        // ==================================
        // KIỂM TRA NÚT BÀN GIAO
        // ==================================

        public bool CanComplete =>
            !IsBusy &&
            CanOperateOrders &&
            SelectedOrder?.OrderStatus == "Ready";

        // ==================================
        // BỘ LỌC TRẠNG THÁI
        // ==================================

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

        private string _statusFilter = "Đang xử lý";

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (Set(ref _statusFilter, value))
                {
                    FilteredOrders.Refresh();
                    ValidateSelectedOrder();
                }
            }
        }

        // ==================================
        // TÌM KIẾM MÃ ĐƠN
        // ==================================

        private string _searchOrder = "";

        public string SearchOrder
        {
            get => _searchOrder;
            set
            {
                if (Set(ref _searchOrder, value))
                {
                    FilteredOrders.Refresh();
                    ValidateSelectedOrder();
                }
            }
        }

        // ==================================
        // LỌC THEO NGÀY
        // ==================================

        private DateTime? _filterDate;

        public DateTime? FilterDate
        {
            get => _filterDate;
            set
            {
                if (Set(ref _filterDate, value))
                {
                    FilteredOrders.Refresh();
                    ValidateSelectedOrder();
                }
            }
        }

        // ==================================
        // HÀM LỌC ĐƠN HÀNG
        // ==================================

        private bool FilterOrder(object obj)
        {
            if (obj is not OrderListRow order)
                return false;

            string keyword = SearchOrder.Trim();

            bool matchCode =
                string.IsNullOrWhiteSpace(keyword) ||
                (order.OrderCode?.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ?? false);

            bool matchStatus =
                StatusFilter == "Tất cả" ||

                (StatusFilter == "Đang xử lý" &&
                 (order.OrderStatus == "New" ||
                  order.OrderStatus == "Preparing" ||
                  order.OrderStatus == "Ready")) ||

                order.OrderStatus == StatusFilter;

            bool matchDate =
                !FilterDate.HasValue ||
                order.CreatedAt.Date == FilterDate.Value.Date;

            return matchCode && matchStatus && matchDate;
        }

        // Bỏ chọn đơn nếu đơn không còn hiển thị
        private void ValidateSelectedOrder()
        {
            if (SelectedOrder != null &&
                !FilterOrder(SelectedOrder))
            {
                SelectedOrder = null;
            }
        }

        // ==================================
        // THÔNG BÁO
        // ==================================

        private string _message = "";

        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        // ==================================
        // TRẠNG THÁI XỬ LÝ
        // ==================================

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (Set(ref _isBusy, value))
                {
                    Notify(nameof(CanComplete));
                }
            }
        }

        // ==================================
        // COMMANDS
        // ==================================

        public ICommand RefreshCommand { get; }
        public ICommand CompleteCommand { get; }

        // ==================================
        // CONSTRUCTOR
        // ==================================

        public OrderViewModel(OrderService service)
        {
            _service = service;

            FilteredOrders =
                CollectionViewSource.GetDefaultView(Orders);

            FilteredOrders.Filter = FilterOrder;

            RefreshCommand = new RelayCommand(
                async () => await RunAsync(LoadDataAsync));

            CompleteCommand = new RelayCommand(
                async () => await RunAsync(CompleteOrderAsync));
        }

        // ==================================
        // KHỞI TẠO DỮ LIỆU
        // ==================================

        public Task InitializeAsync()
        {
            return RunAsync(LoadDataAsync);
        }

        // ==================================
        // TẢI DỮ LIỆU
        // ==================================

        private async Task LoadDataAsync()
        {
            int? previousOrderId =
                SelectedOrder?.OrderId;

            int? previousEmployeeId =
                SelectedEmployee?.EmployeeId;

            // Lấy đơn hàng đã được tạo từ Menu/POS
            var orders = await _service.GetOrdersAsync();

            // Lấy nhân viên
            var employees = await _service.GetEmployeesAsync();

            // Cập nhật đơn hàng
            Replace(Orders, orders);

            // Chỉ hiện danh sách Cashier
            Replace(
                Employees,
                employees.Where(e =>
                    string.Equals(
                        e.RoleName.Trim(),
                        "Cashier",
                        StringComparison.OrdinalIgnoreCase)));

            // Giữ nhân viên đã chọn trước đó
            int? employeeToShow =
                previousEmployeeId ??
                UserSession.Current?.EmployeeId;

            SelectedEmployee = Employees.FirstOrDefault(e =>
                e.EmployeeId == employeeToShow);

            // Nếu không có nhân viên cũ thì hiện Cashier đầu tiên
            SelectedEmployee ??= Employees.FirstOrDefault();

            // Làm mới bộ lọc
            FilteredOrders.Refresh();

            // Giữ đơn đang chọn nếu còn hiển thị
            SelectedOrder = Orders.FirstOrDefault(o =>
                o.OrderId == previousOrderId &&
                FilterOrder(o));

            // Cập nhật thông tin phiên đăng nhập
            Notify(nameof(CurrentEmployeeDisplay));
            Notify(nameof(CanOperateOrders));
            Notify(nameof(OrderPermissionMessage));
            Notify(nameof(CanComplete));
        }

        // ==================================
        // BÀN GIAO ĐƠN
        // ==================================

        private async Task CompleteOrderAsync()
        {
            if (SelectedOrder == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn đơn cần bàn giao.");
            }

            if (!CanOperateOrders)
            {
                throw new InvalidOperationException(
                    "Chỉ Cashier được phép bàn giao đơn.");
            }

            if (SelectedOrder.OrderStatus != "Ready")
            {
                throw new InvalidOperationException(
                    "Chỉ được bàn giao đơn đã Ready.");
            }

            int orderId = SelectedOrder.OrderId;
            int employeeId = GetEmployeeId();

            // Cập nhật trạng thái trong database
            await _service.CompleteOrderAsync(
                orderId,
                employeeId);

            // Tải lại dữ liệu sau khi bàn giao
            await LoadDataAsync();

            // Bỏ chọn đơn vừa hoàn thành
            SelectedOrder = null;

            Message = "Đã bàn giao đơn hàng thành công.";
        }

        // ==================================
        // NHÂN VIÊN THỰC HIỆN
        // ==================================

        private int GetEmployeeId()
        {
            var session = UserSession.Current;

            if (session == null ||
                session.EmployeeId <= 0)
            {
                throw new InvalidOperationException(
                    "Vui lòng đăng nhập trước khi thao tác.");
            }

            if (!CanOperateOrders)
            {
                throw new InvalidOperationException(
                    "Tài khoản không có quyền bàn giao đơn.");
            }

            // Lấy đúng người đang đăng nhập,
            // không lấy từ ComboBox nhân viên.
            return session.EmployeeId;
        }

        // ==================================
        // XỬ LÝ COMMAND
        // ==================================

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

        // ==================================
        // CẬP NHẬT OBSERVABLE COLLECTION
        // ==================================

        private static void Replace<T>(
            ObservableCollection<T> target,
            IEnumerable<T> source)
        {
            target.Clear();

            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}
