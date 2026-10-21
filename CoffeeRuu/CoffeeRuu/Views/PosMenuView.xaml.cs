using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.Repositories;

namespace CoffeeRuu.Views
{
    public partial class PosMenuView : UserControl
    {
        private readonly MenuService _menuService;

        // Lưu toàn bộ món đã tải từ database
        private List<Product> _allProducts = new List<Product>();

        // Danh mục đang chọn; chuỗi rỗng là Tất cả
        private string _selectedCategory = "";

        public PosMenuView()
            : this(new MenuService(
                new MenuRepository(App.CreateDbContext())))
        {
        }

        public PosMenuView(MenuService menuService)
        {
            InitializeComponent();

            _menuService = menuService;

            Loaded += PosMenuView_Loaded;
        }

        // Tải danh sách món khi mở giao diện
        private async void PosMenuView_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                _allProducts =
                    await _menuService.GetAllProductsAsync();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tải được menu: " + ex.Message,
                    "Lỗi tải menu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Xử lý khi chọn danh mục
        private void FilterCategory_Click(
            object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            _selectedCategory =
                button.CommandParameter?.ToString() ?? "";

            // Đổi màu nút danh mục đang chọn
            foreach (Button item in FindVisualChildren<Button>(this))
            {
                if (item.CommandParameter == null)
                    continue;

                string name =
                    item.CommandParameter.ToString() ?? "";

                item.Style = name == _selectedCategory
                    ? (Style)FindResource(
                        "SelectedCategoryButtonStyle")
                    : (Style)FindResource(
                        "CategoryButtonStyle");
            }

            ApplyFilters();
        }

        // Tìm kiếm khi người dùng nhập chữ
        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Xóa từ khóa tìm kiếm
        private void ClearSearch_Click(
            object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            SearchTextBox.Focus();
        }

        // Kết hợp lọc danh mục và tìm kiếm tên món
        private void ApplyFilters()
        {
            string keyword = SearchTextBox?.Text?.Trim() ?? "";

            var filteredProducts = _allProducts
                .Where(p =>
                    // Lọc theo danh mục đang chọn
                    (string.IsNullOrWhiteSpace(_selectedCategory)
                     || (p.Category != null
                         && p.Category.CategoryName == _selectedCategory))

                    // Tìm tên món bắt đầu bằng từ khóa
                    && (string.IsNullOrWhiteSpace(keyword)
                        || (!string.IsNullOrWhiteSpace(p.ProductName)
                            && p.ProductName.Trim().StartsWith(
                                keyword,
                                StringComparison.OrdinalIgnoreCase)))
                )
                .ToList();

            MenuCardItemsControl.ItemsSource = filteredProducts;
        }

        // Tìm các nút nằm bên trong giao diện
        private static IEnumerable<T> FindVisualChildren<T>(
            DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child =
                    VisualTreeHelper.GetChild(parent, i);

                if (child is T target)
                    yield return target;

                foreach (T descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}