
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;

namespace CoffeeRuu.Views
{
    public partial class MenuView : UserControl
    {
        private MenuViewModel? _viewModel;
        private readonly MenuService _menuService;

        public MenuView()
            : this(
                new MenuService(
                    new CoffeeRuu.Repositories.MenuRepository(
                        App.CreateDbContext())))
        {
        }

        public MenuView(MenuService menuService)
        {
            InitializeComponent();

            _menuService = menuService;

            Loaded += MenuView_Loaded;
        }

        private async void MenuView_Loaded(
            object sender, RoutedEventArgs e)
        {
            Loaded -= MenuView_Loaded;

            var dialogService = new MenuDialogService(
                Window.GetWindow(this));

            var viewModel = new MenuViewModel(
                _menuService,
                dialogService);

            _viewModel = viewModel;
            DataContext = viewModel;

            await viewModel.LoadProductsAsync();
        }
    }
}
