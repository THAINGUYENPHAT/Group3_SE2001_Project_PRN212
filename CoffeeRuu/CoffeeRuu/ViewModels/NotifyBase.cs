using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeRuu.ViewModels
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(
            ref T field,
            T value,
            [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));

            return true;
        }

        protected void Notify(
            [CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}
