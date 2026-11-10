
using System;
using System.Windows.Input;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authenticationService;
        private readonly IMessageDialogService _dialogService;
        private readonly WindowService _windowService;

        private string _username = string.Empty;
        private string _password = string.Empty;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(
            AuthenticationService authenticationService,
            IMessageDialogService dialogService,
            WindowService windowService)
        {
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _windowService = windowService;

            LoginCommand = new RelayCommand(Login);
        }

        private void Login()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                _dialogService.ShowWarning(
                    "Thông báo",
                    "Vui lòng nhập tên đăng nhập!");

                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                _dialogService.ShowWarning(
                    "Thông báo",
                    "Vui lòng nhập mật khẩu!");

                return;
            }

            try
            {
                var user = _authenticationService.Authenticate(
                    Username.Trim(),
                    Password);

                if (user == null)
                {
                    _dialogService.ShowWarning(
                        "Đăng nhập thất bại",
                        "Tên đăng nhập hoặc mật khẩu không đúng.");

                    return;
                }

                UserSession.Start(
                    user.EmployeeId,
                    user.RoleId,
                    user.Username,
                    user.FullName,
                    user.RoleName);

                _windowService.OpenMainWindowAfterLogin();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    "Lỗi đăng nhập",
                    ex.Message);
            }
        }
    }
}
