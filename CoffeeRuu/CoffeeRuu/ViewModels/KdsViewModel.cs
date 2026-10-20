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
    public class KdsViewModel : NotifyBase
    {
        private readonly OrderService _service;

        public ObservableCollection<KdsItemRow> Items { get; }
            = new();

        // Món đang chọn
        private KdsItemRow? _selectedItem;

        public KdsItemRow? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        // Mã nhân viên demo
        private string _employeeIdText = "";

        public string EmployeeIdText
        {
            get => _employeeIdText;
            set => Set(ref _employeeIdText, value);
        }

        // Thông báo
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

        public ICommand RefreshCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand ReadyCommand { get; }

        public KdsViewModel(OrderService service)
        {
            _service = service;

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

        // ==========================
        // LẤY DANH SÁCH KDS
        // ==========================

        private async Task LoadDataAsync()
        {
            var items = await _service.GetKdsAsync();

            Items.Clear();

            foreach (var item in items)
                Items.Add(item);

            SelectedItem = null;
        }

        // ==========================
        // CẬP NHẬT TRẠNG THÁI MÓN
        // ==========================

        private async Task UpdateStatusAsync(
            string status)
        {
            if (SelectedItem == null)
                throw new InvalidOperationException(
                    "Hãy chọn món cần xử lý.");

            int employeeId = GetEmployeeId();
            int orderItemId = SelectedItem.OrderItemId;

            await _service.UpdatePreparationAsync(
                orderItemId,
                status,
                employeeId);

            await LoadDataAsync();

            Message = status == "Preparing"
                ? "Đã bắt đầu pha chế món."
                : "Món đã pha xong.";
        }

        // ==========================
        // HÀM HỖ TRỢ
        // ==========================

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
