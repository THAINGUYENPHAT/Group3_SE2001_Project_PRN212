
using System.Windows;

namespace CoffeeRuu.Services
{
    public interface IMessageDialogService
    {
        void ShowInfo(string title, string message);
        void ShowWarning(string title, string message);
        void ShowError(string title, string message);
        bool Confirm(string title, string message);
    }

    public class MessageDialogService : IMessageDialogService
    {
        public void ShowInfo(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowWarning(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public bool Confirm(string title, string message)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
