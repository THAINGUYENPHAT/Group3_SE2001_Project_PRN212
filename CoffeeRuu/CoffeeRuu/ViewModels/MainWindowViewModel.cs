
using System.Windows.Input;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly WindowService _windowService;

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateMenuCommand { get; }
        public ICommand NavigateProductCommand { get; }
        public ICommand NavigateOrdersCommand { get; }
        public ICommand NavigateKdsCommand { get; }
        public ICommand InventoryCommand { get; }
        public ICommand ReportCommand { get; }
        public ICommand TableCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainWindowViewModel(
            NavigationService navigationService,
            WindowService windowService)
        {
            _navigationService = navigationService;
            _windowService = windowService;

            NavigateHomeCommand = new RelayCommand(
                () => _navigationService.Navigate("Home"));

            NavigateMenuCommand = new RelayCommand(
                () => _navigationService.Navigate("Menu"));

            NavigateProductCommand = new RelayCommand(
                () => _navigationService.Navigate("Product"));

            NavigateOrdersCommand = new RelayCommand(
                () => _navigationService.Navigate("Orders"));

            NavigateKdsCommand = new RelayCommand(
                () => _navigationService.Navigate("KDS"));

            InventoryCommand = new RelayCommand(
                () => _navigationService.Navigate("Inventory"));

            ReportCommand = new RelayCommand(
                () => _navigationService.Navigate("Report"));

            TableCommand = new RelayCommand(
                () => _navigationService.Navigate("Table"));

            LogoutCommand = new RelayCommand(
                () => _windowService.Logout());
        }
    }
}
