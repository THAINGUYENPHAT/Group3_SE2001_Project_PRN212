
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly WindowService _windowService;

        private string _currentPage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage == value)
                {
                    return;
                }

                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public Visibility HomeVisibility =>
            CanAccess("Home") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MenuVisibility =>
            CanAccess("Menu") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ProductVisibility =>
            CanAccess("Product") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility OrdersVisibility =>
            CanAccess("Orders") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility KdsVisibility =>
            CanAccess("KDS") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility InventoryVisibility =>
            CanAccess("Inventory") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ReportVisibility =>
            CanAccess("Report") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility TableVisibility =>
            CanAccess("Table") ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EmployeeVisibility =>
            CanAccess("Employee") ? Visibility.Visible : Visibility.Collapsed;

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateMenuCommand { get; }
        public ICommand NavigateProductCommand { get; }
        public ICommand NavigateOrdersCommand { get; }
        public ICommand NavigateKdsCommand { get; }
        public ICommand InventoryCommand { get; }
        public ICommand ReportCommand { get; }
        public ICommand TableCommand { get; }
        public ICommand EmployeeCommand { get; }
        public ICommand LogoutCommand { get; }

        public string CurrentUserName =>
            UserSession.Current?.FullName ?? "Chưa đăng nhập";

        public string CurrentRoleName =>
            UserSession.Current?.RoleName ?? string.Empty;

        public MainWindowViewModel(
            NavigationService navigationService,
            WindowService windowService)
        {
            _navigationService = navigationService;
            _windowService = windowService;

            _navigationService.Navigated += OnNavigated;

            NavigateHomeCommand = new RelayCommand(
                () => Navigate("Home"));

            NavigateMenuCommand = new RelayCommand(
                () => Navigate("Menu"));

            NavigateProductCommand = new RelayCommand(
                () => Navigate("Product"));

            NavigateOrdersCommand = new RelayCommand(
                () => Navigate("Orders"));

            NavigateKdsCommand = new RelayCommand(
                () => Navigate("KDS"));

            InventoryCommand = new RelayCommand(
                () => Navigate("Inventory"));

            ReportCommand = new RelayCommand(
                () => Navigate("Report"));

            TableCommand = new RelayCommand(
                () => Navigate("Table"));

            EmployeeCommand = new RelayCommand(
                () => Navigate("Employee"));

            LogoutCommand = new RelayCommand(
                () => _windowService.Logout());
        }

        private void OnNavigated(string pageName)
        {
            CurrentPage = pageName;
        }

        private bool CanAccess(string screen)
        {
            return UserSession.Current?.CanAccess(screen) == true;
        }

        private void Navigate(string screen)
        {
            if (!CanAccess(screen))
            {
                MessageBox.Show(
                    "Tài khoản của bạn không có quyền truy cập chức năng này.",
                    "Không đủ quyền",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _navigationService.Navigate(screen);
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
