
using CoffeeRuu.Models;
using System.Windows;

namespace CoffeeRuu.Views
{
    public interface IMenuDialogService
    {
        Product? ShowProductDialog(Product? product = null);

        bool ConfirmDelete(string productName);

        void ShowMessage(
            string message,
            string title,
            MessageBoxImage image);
    }
}
