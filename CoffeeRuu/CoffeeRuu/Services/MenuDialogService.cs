using System.Windows;
using CoffeeRuu.Models;
using CoffeeRuu.Views;

namespace CoffeeRuu.Services
{
    public class MenuDialogService : IMenuDialogService
    {
        private readonly Window? _owner;
        private readonly MenuService _menuService;

        public MenuDialogService(
            Window? owner,
            MenuService menuService)
        {
            _owner = owner;
            _menuService = menuService;
        }

        // Mở hộp thoại thêm hoặc sửa món.
        public Product? ShowProductDialog(Product? product = null)
        {
            ProductDialog dialog = new ProductDialog(
                _menuService,
                product);

            if (_owner != null)
            {
                dialog.Owner = _owner;
            }

            return dialog.ShowDialog() == true
                ? dialog.ProductResult
                : null;
        }

        // Xác nhận ngừng bán món.
        public bool ConfirmDelete(string productName)
        {
            MessageBoxResult result = MessageBox.Show(
                _owner,
                $"Bạn có chắc chắn muốn ngừng bán món '{productName}' không?\n\n" +
                "Dữ liệu đơn hàng và công thức vẫn được giữ lại.",
                "Xác nhận ngừng bán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        // Hiển thị thông báo.
        // Hiển thị thông báo.
        public void ShowMessage(
            string message,
            string title,
            DialogMessageType type = DialogMessageType.Information)
        {
            MessageBoxImage image = MessageBoxImage.Information;

            if (type == DialogMessageType.Warning)
            {
                image = MessageBoxImage.Warning;
            }
            else if (type == DialogMessageType.Error)
            {
                image = MessageBoxImage.Error;
            }

            MessageBox.Show(
                _owner,
                message,
                title,
                MessageBoxButton.OK,
                image);
        }
    }
}