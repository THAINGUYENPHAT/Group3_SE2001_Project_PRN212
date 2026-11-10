using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoffeeRuu.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // Bổ sung SetProperty để tương thích với các ViewModel hiện tại
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;

            OnPropertyChanged(propertyName);

            return true;
        }
    }
}