using System.Windows;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class MainShellWindow : Window
    {
        private readonly NavigationService _navigationService;
        private readonly OrderService _orderService;

        public MainShellWindow()
        {
            InitializeComponent();

            _navigationService = new NavigationService(MainContent);

            _orderService = new OrderService(
                new OrderRepository(App.CreateDbContext));

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
                () =>
                {
                    var vm = new KdsViewModel(_orderService);

                    var page = new KdsOrderListView
                    {
                        DataContext = vm
                    };

                    page.Loaded += async (sender, e) =>
                    {
                        await vm.InitializeAsync();
                        vm.StartAutoRefresh();
                    };

                    page.Unloaded += (sender, e) =>
                    {
                        vm.StopAutoRefresh();
                    };

                    return page;
                });


            _navigationService.Register(
                "Inventory",
                () => new InventoryView());

            // Đăng ký màn hình Quản lý công thức kèm ViewModel
            _navigationService.Register(
                "RecipeManagement",
                () => new RecipeManagementView
                {
                    DataContext = new RecipeManagementViewModel(App.CreateDbContext)
                });

            _navigationService.Register(
                "Report",
                () => new ReportView());

            _navigationService.Register(
                "Table",
                () => new TableView());

            _navigationService.Register(
                "Employee",
                () => new EmployeeManagementView());
        }

        // Chuyển sang KDS sau khi in bill.
        public void OpenKds()
        {
            _navigationService.Navigate("KDS");
        }
    }
}