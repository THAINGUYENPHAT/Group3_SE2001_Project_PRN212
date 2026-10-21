using System;
using System.Windows;
using CoffeeRuu.Models;
using CoffeeRuu.Views;

namespace CoffeeRuu.Services
{
    public class MenuDialogService : IMenuDialogService
    {
        private readonly Func<Window?> _ownerProvider;
        private readonly CategoryService _categoryService;

        public MenuDialogService(
            Func<Window?> ownerProvider,
            CategoryService categoryService)
        {
            _ownerProvider = ownerProvider;
            _categoryService = categoryService;
        }

        public Product? ShowProductDialog(Product? product = null)
        {
            ProductDialog dialog = new ProductDialog(
                _categoryService,
                product);

            Window? owner = _ownerProvider();

            if (owner != null)
            {
                dialog.Owner = owner;
            }

            return dialog.ShowDialog() == true
                ? dialog.ProductResult
                : null;
        }

        public bool ConfirmDelete(string productName)
        {
            Window? owner = _ownerProvider();

            string message =
                $"Bạn có chắc chắn muốn xóa món '{productName}' không?";

            MessageBoxResult result = owner != null
                ? MessageBox.Show(
                    owner, message, "Xác nhận xóa",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning)
                : MessageBox.Show(
                    message, "Xác nhận xóa",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        public void ShowMessage(
            string message,
            string title,
            MessageBoxImage image)
        {
            Window? owner = _ownerProvider();

            if (owner != null)
            {
                MessageBox.Show(
                    owner, message, title,
                    MessageBoxButton.OK, image);
            }
            else
            {
                MessageBox.Show(
                    message, title,
                    MessageBoxButton.OK, image);
            }
        }
    }
}