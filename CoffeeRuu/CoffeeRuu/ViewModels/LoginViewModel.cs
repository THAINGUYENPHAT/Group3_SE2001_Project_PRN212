
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Services;
using CoffeeRuu.Views;

namespace CoffeeRuu.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }

        private void Login()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên đăng nhập!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show(
                    "Vui lòng nhập mật khẩu!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = App.CreateDbContext();

                var employee = db.Employees
                    .FirstOrDefault(e => e.Username == Username);

                if (employee == null ||
                    !employee.IsActive ||
                    !PasswordService.VerifyPassword(
                        Password,
                        employee.PasswordHash))
                {
                    MessageBox.Show(
                        "Tên đăng nhập hoặc mật khẩu không đúng.",
                        "Đăng nhập thất bại",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Dang nhap thanh cong: mo giao dien chinh
                var home = new MainShellWindow();

                Application.Current.MainWindow = home;

                home.Show();

                // Dong cua so dang nhap
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Lỗi đăng nhập",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
