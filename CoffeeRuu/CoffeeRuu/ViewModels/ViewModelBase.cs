using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoffeeRuu.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Phát sự kiện thông báo cho UI khi thuộc tính (Property) thay đổi giá trị.
        /// [CallerMemberName] tự động lấy tên của Property gọi hàm này.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Hàm hỗ trợ gán giá trị và tự động gọi OnPropertyChanged nếu giá trị mới khác giá trị cũ.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}