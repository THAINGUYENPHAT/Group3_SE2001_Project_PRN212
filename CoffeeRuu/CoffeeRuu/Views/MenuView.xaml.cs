using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Repositories;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class MenuView : UserControl
    {
        private readonly MenuService _menuService;

        public MenuView()
        {
            InitializeComponent();

            // Khởi tạo Repository.
            var repo = new MenuRepository(App.CreateDbContext());

            // Khởi tạo Service.
            _menuService = new MenuService(repo);

            // Chờ View được gắn vào Window rồi mới lấy Owner.
            Loaded += MenuView_Loaded;
        }

        private async void MenuView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= MenuView_Loaded;

            // Khởi tạo DialogService khi đã có Window.
            var dialogService = new MenuDialogService(
                Window.GetWindow(this),
                _menuService);

            // Khởi tạo ViewModel.
            var viewModel = new MenuViewModel(
                _menuService,
                dialogService);

            DataContext = viewModel;

            // Tải dữ liệu.
            await viewModel.LoadProductsAsync();
        }

    }
}