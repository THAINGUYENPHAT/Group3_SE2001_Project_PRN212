
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(
            Func<Task> execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting &&
                   (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                // Tránh lỗi async bị bỏ qua nếu handler chưa xử lý.
                System.Windows.MessageBox.Show(
                    "Đã xảy ra lỗi khi thực hiện thao tác:\n" + ex.Message,
                    "Lỗi",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
