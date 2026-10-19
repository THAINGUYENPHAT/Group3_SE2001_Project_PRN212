using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Models;

namespace CoffeeRuu.ViewModels
{
    public class TableManagementViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CafeTable> _tables;

        public ObservableCollection<CafeTable> Tables
        {
            get => _tables;
            set { _tables = value; OnPropertyChanged(); }
        }

        // Khai báo Command để xử lý sự kiện click vào bàn
        public ICommand TableClickCommand { get; }

        public TableManagementViewModel()
        {
            LoadMockData();
            // Gán sự kiện click vào hàm OnTableClicked
            TableClickCommand = new RelayCommand<CafeTable>(OnTableClicked);
        }

        private void LoadMockData()
        {
            // DỮ LIỆU GIẢ (Mock Data) ĐỂ DEMO GIẢNG VIÊN
            Tables = new ObservableCollection<CafeTable>
            {
                new CafeTable { TableId = 1, TableName = "T01", Capacity = 2, TableStatus = "Available" },
                new CafeTable { TableId = 2, TableName = "T02", Capacity = 2, TableStatus = "Occupied" },
                new CafeTable { TableId = 3, TableName = "T03", Capacity = 4, TableStatus = "Reserved" },
                new CafeTable { TableId = 4, TableName = "T04", Capacity = 4, TableStatus = "Available" },
                new CafeTable { TableId = 5, TableName = "T05", Capacity = 6, TableStatus = "OutOfService" }
            };
        }

        private void OnTableClicked(CafeTable clickedTable)
        {
            if (clickedTable == null) return;

            // KỊCH BẢN DEMO KHI CLICK VÀO TỪNG LOẠI BÀN
            if (clickedTable.TableStatus == "Available")
            {
                var result = MessageBox.Show($"Bạn có muốn mở bàn {clickedTable.TableName} để tạo đơn hàng mới không?",
                                             "Mở bàn mới", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    clickedTable.TableStatus = "Occupied"; // Chuyển sang Đang phục vụ (Màu đỏ)
                    RefreshTableUI(clickedTable);
                }
            }
            else if (clickedTable.TableStatus == "Occupied")
            {
                MessageBox.Show($"Bàn {clickedTable.TableName} đang có khách. \nGiả lập: Chuyển sang màn hình thêm món/Thanh toán...",
                                "Thông tin bàn", MessageBoxButton.OK, MessageBoxImage.Information);

                // Giả lập thao tác khách thanh toán xong -> Dọn bàn thành trống
                clickedTable.TableStatus = "Available"; // Chuyển về Trống (Màu xanh)
                RefreshTableUI(clickedTable);
            }
            else if (clickedTable.TableStatus == "Reserved")
            {
                MessageBox.Show($"Bàn {clickedTable.TableName} đã được đặt trước. \nKhách đã đến và nhận bàn?",
                                "Xác nhận đặt bàn", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // Giả lập khách đã tới nhận bàn
                clickedTable.TableStatus = "Occupied"; // Chuyển sang Đang phục vụ (Màu đỏ)
                RefreshTableUI(clickedTable);
            }
            else if (clickedTable.TableStatus == "OutOfService")
            {
                MessageBox.Show("Bàn này đang bị hỏng hoặc bảo trì, không thể phục vụ!",
                                "Bảo trì", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Thủ thuật để Giao diện (UI) chớp lấy dữ liệu mới ngay lập tức khi trạng thái thay đổi
        private void RefreshTableUI(CafeTable table)
        {
            int index = Tables.IndexOf(table);
            if (index >= 0)
            {
                Tables[index] = null!;
                Tables[index] = table;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}