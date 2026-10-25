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

        // Chỉ hiển thị 3 trạng thái tiếng Việt theo yêu cầu
        public ObservableCollection<string> StatusList { get; } = new()
        {
            "Đang có sẵn", "Đã đặt", "Đang bảo trì"
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
                    // Chuyển DB status thành Tiếng Việt để hiển thị
                    StatusInput = MapStatusToVietnamese(value.TableStatus);
                }
                else
                {
                    // Tinh gọn, chỉ gọi ClearInputs 1 lần
                    ClearInputs(false);
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

        private string _statusInput = "Đang có sẵn";
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

            AddCommand = new RelayCommand(async () => await AddTableAsync(), () => SelectedTable == null);
            UpdateCommand = new RelayCommand(async () => await UpdateTableAsync(), () => SelectedTable != null);
            DeleteCommand = new RelayCommand(async () => await DeleteTableAsync(), () => SelectedTable != null);

            // Lệnh làm mới chỉ reset UI, không tải lại danh sách bàn (giảm tải DB)
            ClearCommand = new RelayCommand(() => ClearInputs(true));

            LoadCommand = new RelayCommand(async () => await LoadTablesAsync());

            _ = LoadTablesAsync();
        }

        // --- Helper Methods ---

        private string GetNextTableName()
        {
            if (Tables == null || Tables.Count == 0) return "T01";

            int maxNumber = 0;
            foreach (var table in Tables)
            {
                string numbers = new string(table.TableName.Where(char.IsDigit).ToArray());
                if (int.TryParse(numbers, out int num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
            return $"T{(maxNumber + 1):D2}";
        }

        // Map Tiếng Anh từ DB sang Tiếng Việt
        private string MapStatusToVietnamese(string dbStatus)
        {
            return dbStatus switch
            {
                "Available" => "Đang có sẵn",
                "Reserved" => "Đã đặt",
                "OutOfService" => "Đang bảo trì",
                // Mặc định, mapping Occupied về Đã đặt cho khớp với yêu cầu 3 trạng thái của bạn, hoặc bạn có thể tự chỉnh
                "Occupied" => "Đã đặt",
                _ => "Đang có sẵn"
            };
        }

        // Map Tiếng Việt sang Tiếng Anh lưu vào DB
        private string MapStatusToDatabase(string viStatus)
        {
            return viStatus switch
            {
                "Đang có sẵn" => "Available",
                "Đã đặt" => "Reserved",
                "Đang bảo trì" => "OutOfService",
                _ => "Available"
            };
        }

        // --- Commands ---

        private async Task AddTableAsync()
        {
            try
            {
                if (!int.TryParse(CapacityInput, out int capacity))
                    throw new ArgumentException("Sức chứa phải là số nguyên.");

                string newTableName = GetNextTableName();
                // Map trạng thái tiếng Việt sang tiếng Anh trước khi lưu DB
                string dbStatus = MapStatusToDatabase(StatusInput);

                await _tableService.AddTableAsync(newTableName, capacity, dbStatus);

                await LoadTablesAsync();
                StatusMessage = $"Thêm {newTableName} thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task LoadTablesAsync()
        {
            var items = await _tableService.GetTablesAsync();
            Tables.Clear();
            foreach (var item in items)
            {
                // Cập nhật lại trạng thái bằng Tiếng Việt trực tiếp trên Model để DataGrid hiển thị đẹp
                item.TableStatus = MapStatusToVietnamese(item.TableStatus);
                Tables.Add(item);
            }

            if (SelectedTable == null)
            {
                ClearInputs(false);
            }
        }

        private async Task UpdateTableAsync()
        {
            try
            {
                if (SelectedTable == null) return;
                if (!int.TryParse(CapacityInput, out int capacity))
                    throw new ArgumentException("Sức chứa phải là số nguyên.");

                // Map trạng thái tiếng Việt sang tiếng Anh
                string dbStatus = MapStatusToDatabase(StatusInput);

                await _tableService.UpdateTableAsync(SelectedTable.TableId, TableNameInput, capacity);
                await _tableService.ChangeTableStatusAsync(SelectedTable.TableId, dbStatus);

                await LoadTablesAsync();
                StatusMessage = "Cập nhật bàn thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task DeleteTableAsync()
        {
            try
            {
                if (SelectedTable == null) return;

                await _tableService.DeleteTableAsync(SelectedTable.TableId);
                await LoadTablesAsync();
                StatusMessage = "Xóa bàn thành công!";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private void ClearInputs(bool isFromCommand)
        {
            // Tránh vòng lặp khi Set SelectedTable = null sẽ gọi lại ClearInputs
            if (isFromCommand && _selectedTable != null)
            {
                _selectedTable = null;
                OnPropertyChanged(nameof(SelectedTable));
            }

            TableNameInput = GetNextTableName();
            CapacityInput = "4";
            StatusInput = "Đang có sẵn";

            // Xóa message lỗi nếu người dùng nhấn nút làm mới
            if (isFromCommand) StatusMessage = "";

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