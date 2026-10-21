using System.Windows.Controls;
using CoffeeRuu.Services;

namespace CoffeeRuu.Views
{
    public partial class PosMenuView : UserControl
    {
        private readonly MenuService _menuService;

        public PosMenuView() : this(new MenuService(new Repositories.MenuRepository(App.CreateDbContext())))
        {
        }

        public PosMenuView(MenuService menuService)
        {
            InitializeComponent();
            _menuService = menuService;
            Loaded += PosMenuView_Loaded;
        }

        private async void PosMenuView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Load danh sách sản phẩm lên lưới thẻ card
                MenuCardItemsControl.ItemsSource = await _menuService.GetAllProductsAsync();
            }
            catch
            {
                // Xử lý lỗi nếu cần
            }
        }
    }
}