
using CoffeeRuu.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class KdsViewModel : NotifyBase
    {
        private readonly OrderService _service;

        // ============================
        // DANH SÁCH MÓN KDS
        // ============================

        public ObservableCollection<KdsItemRow> Items { get; }
            = new();

        // ============================
        // DANH SÁCH NHÂN VIÊN
        // ============================

        public ObservableCollection<EmployeeChoice> Employees { get; }
            = new();

        private EmployeeChoice? _selectedEmployee;

        public EmployeeChoice? SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

        // ============================
        // NHÂN VIÊN ĐANG ĐĂNG NHẬP
        // ============================

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

        // Giữ tương thích với code-behind cũ
        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        // ============================
        // MÓN ĐƯỢC CHỌN
        // ============================

        private KdsItemRow? _selectedItem;

        public KdsItemRow? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Set(ref _selectedItem, value))
                {
                    Notify(nameof(CanStart));
                    Notify(nameof(CanReady));
                }
            }
        }

        // ============================
        // KIỂM TRA TRẠNG THÁI NÚT
        // ============================

        public bool CanStart =>
            !IsBusy &&
            SelectedItem?.PreparationStatus == "Queued";

        public bool CanReady =>
            !IsBusy &&
            SelectedItem?.PreparationStatus == "Preparing";

        // ============================
        // BỘ LỌC KDS
        // ============================

        public ICollectionView FilteredItems { get; }

        // Ready đã ẩn nên không cần lọc Ready
        public string[] StatusOptions { get; } =
        {
            "Tất cả",
            "Queued",
            "Preparing"
        };

        private string _filterStatus = "Tất cả";

        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (Set(ref _filterStatus, value))
                    FilteredItems.Refresh();
            }
        }

        // Tìm theo mã đơn hoặc tên món
        private string _searchKds = "";

        public string SearchKds
        {
            get => _searchKds;
            set
            {
                if (Set(ref _searchKds, value))
                    FilteredItems.Refresh();
            }
        }

        private bool FilterItem(object obj)
        {
            if (obj is not KdsItemRow item)
                return false;

            string keyword =
                SearchKds?.Trim() ?? "";

            bool matchSearch =
                keyword.Length == 0 ||
                item.OrderCode.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||
                item.ProductName.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase);

            bool matchStatus =
                FilterStatus == "Tất cả" ||
                item.PreparationStatus == FilterStatus;

            return matchSearch && matchStatus;
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
        // TRẠNG THÁI ĐANG XỬ LÝ
        // ============================

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (Set(ref _isBusy, value))
                {
                    Notify(nameof(CanStart));
                    Notify(nameof(CanReady));
                }
            }
        }

        // ============================
        // COMMANDS
        // ============================

        public ICommand RefreshCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand ReadyCommand { get; }

        // ============================
        // CONSTRUCTOR
        // ============================

        public KdsViewModel(OrderService service)
        {
            _service = service;

            FilteredItems =
                CollectionViewSource.GetDefaultView(Items);

            FilteredItems.Filter = FilterItem;

            RefreshCommand = new RelayCommand(
                async () => await RunAsync(LoadDataAsync));

            StartCommand = new RelayCommand(
                async () => await RunAsync(
                    () => UpdateStatusAsync("Preparing")));

            ReadyCommand = new RelayCommand(
                async () => await RunAsync(
                    () => UpdateStatusAsync("Ready")));
        }

        public Task InitializeAsync()
        {
            return RunAsync(LoadDataAsync);
        }

        // ============================
        // TẢI DỮ LIỆU KDS
        // ============================

        private async Task LoadDataAsync()
        {
            int? previousEmployeeId =
                SelectedEmployee?.EmployeeId;

            // Lấy nhân viên từ database
            var employees =
                await _service.GetEmployeesAsync();

            // Chỉ nhận Queued và Preparing
            // vì Repository đã lọc bỏ Ready
            var items =
                await _service.GetKdsAsync();

            // Cập nhật danh sách nhân viên
            Employees.Clear();

            foreach (var employee in employees)
            {
                Employees.Add(employee);
            }

            // Giữ nhân viên đang xem
            int? employeeToShow =
                previousEmployeeId ??
                UserSession.Current?.EmployeeId;

            SelectedEmployee =
                Employees.FirstOrDefault(e =>
                    e.EmployeeId == employeeToShow);

            // Mã người đang đăng nhập
            EmployeeIdText =
                UserSession.Current?.EmployeeId.ToString() ?? "";

            Notify(nameof(CurrentEmployeeDisplay));

            // Cập nhật danh sách món
            Items.Clear();

            foreach (var item in items)
            {
                Items.Add(item);
            }

            SelectedItem = null;

            FilteredItems.Refresh();
        }

        // ============================
        // CẬP NHẬT TRẠNG THÁI MÓN
        // ============================

        private async Task UpdateStatusAsync(
            string status)
        {
            if (SelectedItem == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn món cần xử lý.");
            }

            // Bắt đầu pha chỉ khi Queued
            if (status == "Preparing" &&
                SelectedItem.PreparationStatus != "Queued")
            {
                throw new InvalidOperationException(
                    "Chỉ bắt đầu pha món đang Queued.");
            }

            // Pha xong chỉ khi Preparing
            if (status == "Ready" &&
                SelectedItem.PreparationStatus != "Preparing")
            {
                throw new InvalidOperationException(
                    "Chỉ hoàn thành món đang Preparing.");
            }

            int employeeId = GetEmployeeId();

            int orderItemId =
                SelectedItem.OrderItemId;

            // Gọi Service cập nhật database
            // Giữ nguyên logic công thức và trừ kho
            await _service.UpdatePreparationAsync(
                orderItemId,
                status,
                employeeId);

            // Sau khi thành công, tải lại KDS
            // Món Ready tự biến mất khỏi danh sách
            await LoadDataAsync();

            Message = status == "Preparing"
                ? "Đã bắt đầu pha chế món."
                : "Món đã pha xong và được ẩn khỏi KDS.";
        }

        // ============================
        // LẤY NHÂN VIÊN THỰC HIỆN
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

            if (!session.CanAccess("KDS"))
            {
                throw new InvalidOperationException(
                    "Tài khoản không có quyền thao tác KDS.");
            }

            return session.EmployeeId;
        }

        // ============================
        // HÀM HỖ TRỢ
        // ============================

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
    }
}
