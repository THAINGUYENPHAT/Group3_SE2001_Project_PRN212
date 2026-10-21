using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
                    StatusInput = value.TableStatus;
                }
                else
                {
                    TableNameInput = GetNextTableName();
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

        public RelayCommand AddCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ClearCommand { get; }

        public TableViewModel(TableService tableService)
        {
            _tableService = tableService;

            // Nút Thêm chỉ sáng lên khi KHÔNG CÓ bàn nào được chọn (đang ở chế độ tạo mới)
            AddCommand = new RelayCommand(async () => await AddTableAsync(), () => SelectedTable == null);

            // Nút Cập nhật chỉ sáng lên khi CÓ bàn được chọn
            UpdateCommand = new RelayCommand(async () => await UpdateTableAsync(), () => SelectedTable != null);

            // ĐÃ THÊM: Nút Xóa chỉ sáng lên khi CÓ bàn được chọn
            DeleteCommand = new RelayCommand(async () => await DeleteTableAsync(), () => SelectedTable != null);

            ClearCommand = new RelayCommand(ClearInputs);
            LoadCommand = new RelayCommand(async () => await LoadTablesAsync());

            _ = LoadTablesAsync();
        }

        // --- Hàm tự động tính Tên bàn tiếp theo (Định dạng TXX) ---
        private string GetNextTableName()
        {
            if (Tables == null || Tables.Count == 0) return "T01";

            int maxNumber = 0;
            foreach (var table in Tables)
            {
                string numbers = new string(table.TableName.Where(char.IsDigit).ToArray());
                if (int.TryParse(numbers, out int num))
                {
                    if (num > maxNumber) maxNumber = num;
                }
            }

            return $"T{(maxNumber + 1):D2}";
        }

        // --- Hàm xử lý Thêm bàn ---
        private async Task AddTableAsync()
        {
            try
            {
                if (!int.TryParse(CapacityInput, out int capacity))
                    throw new ArgumentException("Sức chứa phải là số nguyên.");

                string newTableName = GetNextTableName(); // Tính lại tên cho chắc chắn trước khi lưu

                await _tableService.AddTableAsync(newTableName, capacity, StatusInput);

                await LoadTablesAsync();
                StatusMessage = $"Thêm {newTableName} thành công!";
                ClearInputs(); // Reset form sau khi thêm
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task LoadTablesAsync()
        {
            var items = await _tableService.GetTablesAsync();
            Tables.Clear();
            foreach (var item in items)
            {
                Tables.Add(item);
            }

            if (SelectedTable == null)
            {
                TableNameInput = GetNextTableName();
            }
        }

        private async Task UpdateTableAsync()
        {
            try
            {
                if (SelectedTable == null) return;
                if (!int.TryParse(CapacityInput, out int capacity))
                    throw new ArgumentException("Sức chứa phải là số nguyên.");

                await _tableService.UpdateTableAsync(SelectedTable.TableId, TableNameInput, capacity);
                await _tableService.ChangeTableStatusAsync(SelectedTable.TableId, StatusInput);

                await LoadTablesAsync();
                StatusMessage = "Cập nhật bàn thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        // ĐÃ THÊM: Hàm xử lý Xóa bàn
        private async Task DeleteTableAsync()
        {
            try
            {
                if (SelectedTable == null) return;

                await _tableService.DeleteTableAsync(SelectedTable.TableId);

                await LoadTablesAsync();
                ClearInputs(); // Reset form sau khi xóa
                StatusMessage = "Xóa bàn thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private void ClearInputs()
        {
            _selectedTable = null;
            OnPropertyChanged(nameof(SelectedTable));

            TableNameInput = GetNextTableName(); // Đặt sẵn tên bàn tiếp theo
            CapacityInput = "4";
            StatusInput = "Available";
            StatusMessage = "";

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            AddCommand?.RaiseCanExecuteChanged();
            UpdateCommand?.RaiseCanExecuteChanged();

            DeleteCommand?.RaiseCanExecuteChanged();

            ClearCommand?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}