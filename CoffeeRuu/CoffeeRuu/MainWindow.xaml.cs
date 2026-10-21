
using System;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using CoffeeRuu.Views;

namespace CoffeeRuu
{
    public partial class MainWindow
    {
        private readonly NavigationService _navigationService;
        private readonly OrderService _orderService;

        public MainWindow()
        {
            InitializeComponent();

            _navigationService = new NavigationService(MainContent);

            _orderService = new OrderService(
                new OrderRepository(App.CreateDbContextOptions()));

            RegisterViews();

            DataContext = new MainWindowViewModel(
                _navigationService,
                new WindowService(this));

            _navigationService.Navigate("Home");
        }

        private void RegisterViews()
        {
            _navigationService.Register(
                "Home",
                () => new HomeView());

            _navigationService.Register(
                "Menu",
                () => new PosMenuView());

            _navigationService.Register(
                "Product",
                () => new ProductManagementView());

            _navigationService.Register(
                "Orders",
                () => new OrderView
                {
                    DataContext = new OrderViewModel(_orderService)
                });

            _navigationService.Register(
                "KDS",
                () => new KdsView
                {
                    DataContext = new KdsViewModel(_orderService)
                });

            _navigationService.Register(
                "Inventory",
                () => new InventoryView());

            _navigationService.Register(
                "Report",
                () => new ReportView());

            _navigationService.Register(
                "Table",
                () => new TableView());
        }
    }
}
