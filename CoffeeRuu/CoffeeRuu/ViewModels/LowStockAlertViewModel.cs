
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class LowStockAlertViewModel : INotifyPropertyChanged
    {
        private readonly LowStockAlertService _service;

        public ObservableCollection<LowStockAlertItem> Alerts
        { get; } = new();

        public int TotalAlerts => Alerts.Count;

        public int OutOfStockCount =>
            Alerts.Count(x => x.CurrentStock == 0);

        public int LowStockCount =>
            Alerts.Count(x => x.CurrentStock > 0);

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = "";

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }

        public LowStockAlertViewModel(
            LowStockAlertService service)
        {
            _service = service;
            RefreshCommand = new RefreshLowStockCommand(this);
        }

        public async Task LoadAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            StatusMessage = "Đang tải dữ liệu tồn kho...";

            try
            {
                var results = await _service.GetAlertsAsync();

                Alerts.Clear();

                foreach (var item in results)
                {
                    Alerts.Add(item);
                }

                OnPropertyChanged(nameof(TotalAlerts));
                OnPropertyChanged(nameof(OutOfStockCount));
                OnPropertyChanged(nameof(LowStockCount));

                StatusMessage = Alerts.Count == 0
                    ? "Không có nguyên liệu nào cần cảnh báo."
                    : $"Đã cập nhật {DateTime.Now:HH:mm:ss}. " +
                      $"Có {TotalAlerts} nguyên liệu cần chú ý.";
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Không thể tải cảnh báo: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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

        private sealed class RefreshLowStockCommand : ICommand
        {
            private readonly LowStockAlertViewModel _viewModel;

            public RefreshLowStockCommand(
                LowStockAlertViewModel viewModel)
            {
                _viewModel = viewModel;

                _viewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(IsLoading))
                    {
                        CanExecuteChanged?.Invoke(
                            this, EventArgs.Empty);
                    }
                };
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return !_viewModel.IsLoading;
            }

            public async void Execute(object? parameter)
            {
                if (CanExecute(parameter))
                {
                    await _viewModel.LoadAsync();
                }
            }
        }
    }
}
