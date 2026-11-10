using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System;


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
            // Lấy nhân viên từ phiên đăng nhập
            var currentUser = UserSession.Current;

            if (currentUser == null)
            {
                throw new InvalidOperationException(
                    "Vui lòng đăng nhập để sử dụng chức năng nhập kho.");
            }

            var repository = new StockReceiptRepository(
                App.CreateDbContext());

            var service = new StockReceiptService(repository);

            var viewModel = new StockReceiptViewModel(
                service,
                currentUser.EmployeeId);

            StockReceiptFrame.Navigate(
                new StockReceiptView(viewModel));
        }

    }
}
