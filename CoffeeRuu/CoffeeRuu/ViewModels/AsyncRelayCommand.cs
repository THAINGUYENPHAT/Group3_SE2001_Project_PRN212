using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private readonly Action<Exception>? _onError;

        private bool _isExecuting;

        public AsyncRelayCommand(
            Func<Task> execute,
            Func<bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
            _onError = onError;
        }

        // Kiểm tra command có được phép thực hiện hay không.
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting &&
                   (_canExecute?.Invoke() ?? true);
        }

        // Thực hiện command bất đồng bộ.
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
                // Chuyển lỗi ra bên ngoài để ViewModel xử lý.
                _onError?.Invoke(ex);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        // Thông báo cho giao diện cập nhật trạng thái nút.
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}