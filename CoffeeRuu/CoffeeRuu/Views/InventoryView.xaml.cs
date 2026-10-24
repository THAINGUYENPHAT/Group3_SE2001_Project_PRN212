using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using System.Windows;
using System.Windows.Controls;


namespace CoffeeRuu.Views
{
    public partial class InventoryView : Page
    {
        public InventoryView()
        {
            InitializeComponent();

            // F6.1 - Quản lý nguyên liệu
            IngredientFrame.Navigate(
                new IngredientView());

            // F6.1 - Quản lý nhà cung cấp
            SupplierFrame.Navigate(
                new SupplierView());

            // F6.2 - Nhập hàng
            InitializeStockReceipt();
            
            //F6.3
            LowStockAlertFrame.Navigate(new LowStockAlertView());
        }

        private void InitializeStockReceipt()
        {
            // TẠM THỜI: ID nhân viên dùng để kiểm thử
            // Phải là EmployeeId có thật và đang hoạt động
            int testEmployeeId = 1;

            var repository = new StockReceiptRepository(
                App.CreateDbContext());

            var service = new StockReceiptService(repository);

            var viewModel = new StockReceiptViewModel(
                service,
                testEmployeeId);

            StockReceiptFrame.Navigate(
                new StockReceiptView(viewModel));


        }
    }
}
