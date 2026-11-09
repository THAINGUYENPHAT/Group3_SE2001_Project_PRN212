
using CoffeeRuu.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace CoffeeRuu.ViewModels
{
    public class KdsViewModel : NotifyBase
    {
        private readonly OrderService _service;
        private readonly DispatcherTimer _refreshTimer;
        private bool _refreshInProgress;

        public ObservableCollection<KdsItemRow> Items { get; } = new();

        public ObservableCollection<EmployeeChoice> Employees { get; } = new();

        public ICollectionView FilteredItems { get; }

        public string[] StatusOptions { get; } =
        {
            "Tất cả",
            "Queued",
            "Preparing"
        };

        private EmployeeChoice? _selectedEmployee;

        public EmployeeChoice? SelectedEmployee
        {
            get => _selectedEmployee;
            set => Set(ref _selectedEmployee, value);
        }

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

        private string _searchKds = "";

        public string SearchKds
        {
            get => _searchKds;
            set
            {
                if (Set(ref _searchKds, value))
                    RefreshFilter();
            }
        }

        private string _filterStatus = "Tất cả";

        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (Set(ref _filterStatus, value))
                    RefreshFilter();
            }
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
            private set
            {
                if (Set(ref _isBusy, value))
                {
                    Notify(nameof(CanStart));
                    Notify(nameof(CanReady));
                }
            }
        }

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

        public bool CanOperateKds
        {
            get
            {
                var session = UserSession.Current;

                return session != null &&
                       session.EmployeeId > 0 &&
                       string.Equals(
                           session.RoleName.Trim(),
                           "Barista",
                           StringComparison.OrdinalIgnoreCase);
            }
        }

        public string KdsPermissionMessage =>
            CanOperateKds
                ? "Barista: có quyền cập nhật pha chế."
                : "Chế độ chỉ xem - chỉ Barista được cập nhật pha chế.";

        public bool CanStart =>
            !IsBusy &&
            CanOperateKds &&
            SelectedItem?.PreparationStatus == "Queued";

        public bool CanReady =>
            !IsBusy &&
            CanOperateKds &&
            SelectedItem?.PreparationStatus == "Preparing";

        public ICommand RefreshCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand ReadyCommand { get; }

        public KdsViewModel(OrderService service)
        {
            _service = service ??
                throw new ArgumentNullException(nameof(service));

            FilteredItems =
                CollectionViewSource.GetDefaultView(Items);

            FilteredItems.Filter = FilterItem;

            FilteredItems.GroupDescriptions?.Add(
                new PropertyGroupDescription(
                    nameof(KdsItemRow.OrderCode)));

            RefreshCommand = new RelayCommand(
                async () => await ExecuteAsync(LoadAsync));

            StartCommand = new RelayCommand(
                async () => await ExecuteAsync(
                    () => ChangeStatusAsync("Preparing")));

            ReadyCommand = new RelayCommand(
                async () => await ExecuteAsync(
                    () => ChangeStatusAsync("Ready")));

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };

            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        public Task InitializeAsync()
        {
            return ExecuteAsync(LoadAsync);
        }

        public void StartAutoRefresh()
        {
            if (!_refreshTimer.IsEnabled)
                _refreshTimer.Start();
        }

        public void StopAutoRefresh()
        {
            _refreshTimer.Stop();
        }

        private async void RefreshTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (_refreshInProgress || IsBusy)
                return;

            _refreshInProgress = true;

            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Message = "Không thể làm mới KDS: " + ex.Message;
            }
            finally
            {
                _refreshInProgress = false;
            }
        }

        private bool FilterItem(object obj)
        {
            if (obj is not KdsItemRow item)
                return false;

            if (item.PreparationStatus != "Queued" &&
                item.PreparationStatus != "Preparing")
            {
                return false;
            }

            string keyword = SearchKds.Trim();

            bool matchSearch =
                keyword.Length == 0 ||
                (item.OrderCode ?? "").Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||
                (item.ProductName ?? "").Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase);

            bool matchStatus =
                FilterStatus == "Tất cả" ||
                item.PreparationStatus == FilterStatus;

            return matchSearch && matchStatus;
        }

        private void RefreshFilter()
        {
            FilteredItems.Refresh();

            if (SelectedItem != null &&
                !FilterItem(SelectedItem))
            {
                SelectedItem = null;
            }
        }

        private async Task LoadAsync()
        {
            int? selectedEmployeeId =
                SelectedEmployee?.EmployeeId;

            int? selectedItemId =
                SelectedItem?.OrderItemId;

            var employees =
                await _service.GetEmployeesAsync();

            var items =
                await _service.GetKdsAsync();

            var baristas = employees
                .Where(e => string.Equals(
                    e.RoleName.Trim(),
                    "Barista",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var activeItems = items
                .Where(i =>
                    i.PreparationStatus == "Queued" ||
                    i.PreparationStatus == "Preparing")
                .ToList();

            bool employeesChanged =
                Employees.Count != baristas.Count ||
                Employees.Zip(
                    baristas,
                    (a, b) =>
                        a.EmployeeId == b.EmployeeId &&
                        a.FullName == b.FullName &&
                        a.RoleName == b.RoleName)
                .Any(same => !same);

            if (employeesChanged)
            {
                Employees.Clear();

                foreach (var employee in baristas)
                {
                    Employees.Add(employee);
                }

                int? employeeIdToShow =
                    selectedEmployeeId ??
                    UserSession.Current?.EmployeeId;

                SelectedEmployee =
                    Employees.FirstOrDefault(e =>
                        e.EmployeeId == employeeIdToShow)
                    ?? Employees.FirstOrDefault();
            }

            bool itemsChanged =
                Items.Count != activeItems.Count ||
                Items.Zip(
                    activeItems,
                    (a, b) =>
                        a.OrderItemId == b.OrderItemId &&
                        a.OrderCode == b.OrderCode &&
                        a.ProductName == b.ProductName &&
                        a.SizeName == b.SizeName &&
                        a.Quantity == b.Quantity &&
                        a.Note == b.Note &&
                        a.PreparationStatus == b.PreparationStatus)
                .Any(same => !same);

            if (itemsChanged)
            {
                Items.Clear();

                foreach (var item in activeItems)
                {
                    Items.Add(item);
                }

                SelectedItem =
                    Items.FirstOrDefault(i =>
                        i.OrderItemId == selectedItemId &&
                        FilterItem(i));

                FilteredItems.Refresh();
            }

            Notify(nameof(CurrentEmployeeDisplay));
            Notify(nameof(CanOperateKds));
            Notify(nameof(KdsPermissionMessage));
            Notify(nameof(CanStart));
            Notify(nameof(CanReady));
        }

        private async Task ChangeStatusAsync(string nextStatus)
        {
            if (!CanOperateKds ||
                UserSession.Current == null)
            {
                throw new InvalidOperationException(
                    "Chỉ tài khoản Barista được cập nhật pha chế.");
            }

            if (SelectedItem == null)
            {
                throw new InvalidOperationException(
                    "Hãy chọn món cần xử lý.");
            }

            bool valid =
                (SelectedItem.PreparationStatus == "Queued" &&
                 nextStatus == "Preparing") ||
                (SelectedItem.PreparationStatus == "Preparing" &&
                 nextStatus == "Ready");

            if (!valid)
            {
                throw new InvalidOperationException(
                    "Chuyển trạng thái pha chế không hợp lệ.");
            }

            int itemId = SelectedItem.OrderItemId;
            int employeeId = UserSession.Current.EmployeeId;

            await _service.UpdatePreparationAsync(
                itemId,
                nextStatus,
                employeeId);

            await LoadAsync();

            Message = nextStatus == "Preparing"
                ? "Đã bắt đầu pha chế."
                : "Món đã Ready và được ẩn khỏi danh sách đang pha.";
        }

        private async Task ExecuteAsync(Func<Task> action)
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
