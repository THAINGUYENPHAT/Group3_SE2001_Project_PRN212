
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.Views
{
    public partial class PosMenuView : UserControl
    {
        private readonly MenuService _menuService;

        // Danh sách sản phẩm gốc từ database
        private readonly ObservableCollection<PosMenuCardItem> _allProducts
            = new ObservableCollection<PosMenuCardItem>();

        // Danh sách sản phẩm đang hiển thị
        private readonly ObservableCollection<PosMenuCardItem> _filteredProducts
            = new ObservableCollection<PosMenuCardItem>();

        private string _selectedCategory = "";

        public PosMenuView()
            : this(new MenuService(
                new Repositories.MenuRepository(App.CreateDbContext())))
        {
        }

        public PosMenuView(MenuService menuService)
        {
            InitializeComponent();

            _menuService = menuService;

            MenuCardItemsControl.ItemsSource = _filteredProducts;

            Loaded += PosMenuView_Loaded;
        }

        private async void PosMenuView_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var products = await _menuService.GetAllProductsAsync();

                _allProducts.Clear();

                foreach (var product in products)
                {
                    // Chỉ hiển thị món đang kinh doanh
                    if (!product.IsAvailable)
                        continue;

                    var item = new PosMenuCardItem
                    {
                        ProductName = product.ProductName,
                        CategoryName = product.Category?.CategoryName ?? ""
                    };

                    // Lấy giá và size từ bảng ProductSizes
                    if (product.ProductSizes != null)
                    {
                        foreach (var size in product.ProductSizes
                            .Where(s => s.IsAvailable)
                            .OrderBy(s => s.Price))
                        {
                            item.SizePrices.Add(new PosMenuSizePrice
                            {
                                SizeName = size.SizeName,
                                Price = size.Price
                            });
                        }
                    }

                    // Không hiển thị món không có size/giá khả dụng
                    if (item.SizePrices.Count > 0)
                        _allProducts.Add(item);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách món.\n" + ex.Message,
                    "Lỗi tải menu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Bấm nút danh mục
        private void CategoryButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            _selectedCategory = button.Tag?.ToString() ?? "";

            // Cập nhật màu nút đang chọn
            foreach (var item in FindButtons(this))
            {
                bool isSelected =
                    (item.Tag?.ToString() ?? "") == _selectedCategory;

                item.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isSelected ? "#4E342E" : "#D7CCC8"));

                item.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isSelected ? "#FFFFFF" : "#3E2723"));
            }

            ApplyFilters();
        }

        // Tìm kiếm ngay khi nhập chữ
        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string keyword = SearchTextBox?.Text?.Trim() ?? "";

            _filteredProducts.Clear();

            var result = _allProducts.Where(p =>
            {
                // Lọc danh mục chính xác
                bool matchesCategory =
                    string.IsNullOrEmpty(_selectedCategory) ||
                    string.Equals(
                        p.CategoryName,
                        _selectedCategory,
                        StringComparison.OrdinalIgnoreCase);

                // Tìm từ đầu tên món, không phân biệt hoa/thường
                bool matchesKeyword =
                    string.IsNullOrEmpty(keyword) ||
                    p.ProductName.StartsWith(
                        keyword,
                        StringComparison.CurrentCultureIgnoreCase);

                return matchesCategory && matchesKeyword;
            });

            foreach (var product in result)
                _filteredProducts.Add(product);
        }

        private static System.Collections.Generic.IEnumerable<Button>
            FindButtons(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button &&
                    button.Tag != null)
                {
                    yield return button;
                }

                foreach (var nested in FindButtons(child))
                    yield return nested;
            }
        }
    }

    // Dữ liệu hiển thị cho mỗi thẻ món
    public class PosMenuCardItem
    {
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";

        public ObservableCollection<PosMenuSizePrice> SizePrices
        { get; set; } = new ObservableCollection<PosMenuSizePrice>();
    }

    public class PosMenuSizePrice
    {
        public string SizeName { get; set; } = "";
        public decimal Price { get; set; }
    }
}
