using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class TableViewModel : INotifyPropertyChanged
    {
        private readonly TableService _tableService;

        public ObservableCollection<CafeTable> Tables { get; } = new();

        // Danh sách trạng thái chuẩn từ Database
        public ObservableCollection<string> StatusList { get; } = new()
        {
            "Available", "Occupied", "Reserved", "OutOfService"
        };

        private CafeTable? _selectedTable;
        public CafeTable? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (_selectedTable == value) return;

                _selectedTable = value;
                OnPropertyChanged();

                if (value != null)
                {
                    TableNameInput = value.TableName;
                    CapacityInput = value.Capacity.ToString();
                    StatusInput = value.TableStatus; // Lấy trạng thái hiện tại của bàn
                }
                else
                {
                    TableNameInput = "";
                    CapacityInput = "4";
                    StatusInput = "Available";
                    StatusMessage = "";
                }

                UpdateCommands();
            }
        }

        private string _tableNameInput = "";
        public string TableNameInput
        {
            get => _tableNameInput;
            set { _tableNameInput = value; OnPropertyChanged(); }
        }

        private string _capacityInput = "4";
        public string CapacityInput
        {
            get => _capacityInput;
            set { _capacityInput = value; OnPropertyChanged(); }
        }

        // Biến lưu trạng thái đang được chọn trong Form
        private string _statusInput = "Available";
        public string StatusInput
        {
            get => _statusInput;
            set { _statusInput = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand LoadCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand ClearCommand { get; }

        public TableViewModel(TableService tableService)
        {
            _tableService = tableService;

            LoadCommand = new RelayCommand(async () => await LoadTablesAsync());
            UpdateCommand = new RelayCommand(async () => await UpdateTableAsync(), () => SelectedTable != null);
            ClearCommand = new RelayCommand(ClearInputs);

            _ = LoadTablesAsync();
        }

        private async Task LoadTablesAsync()
        {
            var items = await _tableService.GetTablesAsync();
            Tables.Clear();
            foreach (var item in items)
            {
                Tables.Add(item);
            }
        }

        private async Task UpdateTableAsync()
        {
            try
            {
                if (SelectedTable == null) return;
                if (!int.TryParse(CapacityInput, out int capacity)) throw new ArgumentException("Sức chứa phải là số nguyên.");

                await _tableService.UpdateTableAsync(SelectedTable.TableId, TableNameInput, capacity);

                await _tableService.ChangeTableStatusAsync(SelectedTable.TableId, StatusInput);

                await LoadTablesAsync();
                StatusMessage = "Cập nhật bàn thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private void ClearInputs()
        {
            _selectedTable = null;
            OnPropertyChanged(nameof(SelectedTable));

            TableNameInput = "";
            CapacityInput = "4";
            StatusInput = "Available";
            StatusMessage = "";

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            UpdateCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}