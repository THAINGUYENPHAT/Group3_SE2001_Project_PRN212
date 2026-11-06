
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

        // DANH SÁCH MÓN
        public ObservableCollection<KdsItemRow> Items { get; }
            = new();

        // DANH SÁCH BARISTA - CHỈ ĐỂ XEM
        public ObservableCollection<EmployeeChoice> Employees { get; }
            = new();

        private EmployeeChoice? _selectedEmployee;

        public EmployeeChoice? SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

        // THÔNG TIN ĐĂNG NHẬP
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

        // CHỈ BARISTA ĐƯỢC THAO TÁC
        public bool CanOperateKds
        {
            get
            {
                var session = UserSession.Current;

                return session != null
                    && session.EmployeeId > 0
                    && string.Equals(
                        session.RoleName.Trim(),
                        "Barista",
                        StringComparison.OrdinalIgnoreCase);
            }
        }

        public string KdsPermissionMessage
        {
            get
            {
                if (UserSession.Current == null)
                    return "Vui lòng đăng nhập.";

                if (CanOperateKds)
                    return "Bạn có quyền cập nhật pha chế.";

                return "Chế độ chỉ xem - chỉ Barista được pha chế.";
            }
        }

        // GIỮ PROPERTY CŨ ĐỂ TƯƠNG THÍCH
        // KHÔNG DÙNG PROPERTY NÀY XÁC THỰC
        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        // MÓN ĐƯỢC CHỌN
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

        // ĐIỀU KIỆN BẬT NÚT
        public bool CanStart =>
            !IsBusy
            && CanOperateKds
            && SelectedItem?.PreparationStatus == "Queued";

        public bool CanReady =>
            !IsBusy
            && CanOperateKds
            && SelectedItem?.PreparationStatus == "Preparing";

        // LỌC VÀ GOM NHÓM MÓN
        public ICollectionView FilteredItems { get; }

        public string[] StatusOptions { get; } =
        {
            "Tất cả",
            "Queued",
            "Preparing"
        };

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

        private bool FilterItem(object obj)
        {
            if (obj is not KdsItemRow item)
                return false;

            string keyword = SearchKds.Trim();

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

        // THÔNG BÁO
        private string _message = "";

        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        // ĐANG XỬ LÝ
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

        // COMMANDS
        public ICommand RefreshCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand ReadyCommand { get; }

        public KdsViewModel(OrderService service)
        {
            _service = service;

            FilteredItems =
                CollectionViewSource.GetDefaultView(Items);

            FilteredItems.Filter = FilterItem;

            // MVVM: GOM MÓN THEO MÃ ĐƠN TẠI VIEWMODEL
            FilteredItems.GroupDescriptions?.Add(
                new PropertyGroupDescription(
                    nameof(KdsItemRow.OrderCode)));

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

        // LẤY DỮ LIỆU
        private async Task LoadDataAsync()
        {
            int? previousEmployeeId =
                SelectedEmployee?.EmployeeId;

            var employees =
                await _service.GetEmployeesAsync();

            var items =
                await _service.GetKdsAsync();

            Employees.Clear();

            // CHỈ HIỂN THỊ BARISTA
            foreach (var employee in employees.Where(e =>
                string.Equals(
                    e.RoleName.Trim(),
                    "Barista",
                    StringComparison.OrdinalIgnoreCase)))
            {
                Employees.Add(employee);
            }

            int? employeeToShow =
                previousEmployeeId ??
                UserSession.Current?.EmployeeId;

            SelectedEmployee = Employees.FirstOrDefault(e =>
                e.EmployeeId == employeeToShow);

            // THÔNG TIN NGƯỜI ĐĂNG NHẬP
            EmployeeIdText =
                UserSession.Current?.EmployeeId.ToString() ?? "";

            Notify(nameof(CurrentEmployeeDisplay));
            Notify(nameof(CanOperateKds));
            Notify(nameof(KdsPermissionMessage));

            Items.Clear();

            foreach (var item in items)
                Items.Add(item);

            SelectedItem = null;

            FilteredItems.Refresh();

            Notify(nameof(CanStart));
            Notify(nameof(CanReady));
        }

        // CẬP NHẬT PHA CHẾ
        private async Task UpdateStatusAsync(string status)
        {
            if (!CanOperateKds)
            {
                throw new InvalidOperationException(
                    "Chỉ tài khoản Barista được pha chế.");
            }

            if (SelectedItem == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn món cần xử lý.");
            }

            if (status == "Preparing" &&
                SelectedItem.PreparationStatus != "Queued")
            {
                throw new InvalidOperationException(
                    "Chỉ bắt đầu pha món đang Queued.");
            }

            if (status == "Ready" &&
                SelectedItem.PreparationStatus != "Preparing")
            {
                throw new InvalidOperationException(
                    "Chỉ hoàn thành món đang Preparing.");
            }

            int employeeId = GetEmployeeId();
            int orderItemId = SelectedItem.OrderItemId;

            // SERVICE VẪN XỬ LÝ CÔNG THỨC VÀ TRỪ KHO
            await _service.UpdatePreparationAsync(
                orderItemId,
                status,
                employeeId);

            await LoadDataAsync();

            Message = status == "Preparing"
                ? "Đã bắt đầu pha chế."
                : "Món đã pha xong và được ẩn khỏi KDS.";
        }

        // LẤY NHÂN VIÊN ĐANG ĐĂNG NHẬP
        private int GetEmployeeId()
        {
            var session = UserSession.Current;

            if (session == null || session.EmployeeId <= 0)
            {
                throw new InvalidOperationException(
                    "Vui lòng đăng nhập.");
            }

            if (!CanOperateKds)
            {
                throw new InvalidOperationException(
                    "Tài khoản không có quyền pha chế.");
            }

            return session.EmployeeId;
        }

        // CHẠY COMMAND
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
