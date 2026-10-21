using System.Windows;
using CoffeeRuu.Models;
using CoffeeRuu.Views;

namespace CoffeeRuu.Services
{
    public class MenuDialogService : IMenuDialogService
    {
        private readonly Window? _owner;

        public MenuDialogService(Window? owner = null)
        {
            _owner = owner;
        }

        public Product? ShowProductDialog(Product? product = null)
        {
            ProductDialog dialog = new ProductDialog(product);

            if (_owner != null)
                dialog.Owner = _owner;

            return dialog.ShowDialog() == true
                ? dialog.ProductResult
                : null;
        }

        public bool ConfirmDelete(string productName)
        {
            MessageBoxResult result = MessageBox.Show(
                _owner,
                $"Bạn có chắc chắn muốn xóa món '{productName}' không?\n" +
                "Thao tác này sẽ xóa bản ghi món khỏi database.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        public void ShowMessage(
            string message,
            string title,
            MessageBoxImage image)
        {
            MessageBox.Show(
                _owner,
                message,
                title,
                MessageBoxButton.OK,
                image);
        }
    }
}