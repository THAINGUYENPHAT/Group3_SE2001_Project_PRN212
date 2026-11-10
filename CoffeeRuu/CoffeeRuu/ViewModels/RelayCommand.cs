using System;
using System.Windows.Input;

namespace CoffeeRuu.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _executeWithParameter;
        private readonly Func<object?, bool>? _canExecuteWithParameter;

        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        // Constructor có tham số theo mẫu của thầy
        public RelayCommand(
            Action<object?> execute,
            Func<object?, bool>? canExecute = null)
        {
            _executeWithParameter = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecuteWithParameter = canExecute;
        }

        // Constructor không có tham số
        // Giữ tương thích với code CoffeeRuu hiện tại
        public RelayCommand(
            Action execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_executeWithParameter != null)
            {
                return _canExecuteWithParameter == null
                    || _canExecuteWithParameter(parameter);
            }

            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            if (_executeWithParameter != null)
            {
                _executeWithParameter(parameter);
            }
            else
            {
                _execute?.Invoke();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Giữ RelayCommand<T> để không ảnh hưởng các ViewModel đang sử dụng
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(
            Action<T?> execute,
            Predicate<T?>? canExecute = null)
        {
            _execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
            {
                return _canExecute == null || _canExecute(default);
            }

            return _canExecute == null
                || _canExecute((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}