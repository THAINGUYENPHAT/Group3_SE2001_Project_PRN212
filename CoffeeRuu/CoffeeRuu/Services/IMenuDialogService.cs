using CoffeeRuu.Models;

namespace CoffeeRuu.Services
{
    // Các loại thông báo để ViewModel không phải phụ thuộc WPF.
    public enum DialogMessageType
    {
        Information,
        Warning,
        Error
    }

    public interface IMenuDialogService
    {
        // Mở hộp thoại thêm hoặc sửa món.
        Product? ShowProductDialog(Product? product = null);

        // Hiển thị hộp thoại xác nhận xóa mềm.
        bool ConfirmDelete(string productName);

        // Hiển thị thông báo.
        void ShowMessage(
            string message,
            string title,
            DialogMessageType type = DialogMessageType.Information);
    }
}