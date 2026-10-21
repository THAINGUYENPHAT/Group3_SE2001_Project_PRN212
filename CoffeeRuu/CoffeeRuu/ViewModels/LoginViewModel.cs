
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Data;
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
                MessageBox.Show("Vui long nhap ten dang nhap!");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Vui long nhap mat khau!");
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
                        Password, employee.PasswordHash))
                {
                    MessageBox.Show(
                        "Ten dang nhap hoac mat khau khong dung.",
                        "Dang nhap that bai",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var home = new MainWindow();

                Application.Current.MainWindow = home;

                home.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView)
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
                    "Loi dang nhap",
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
