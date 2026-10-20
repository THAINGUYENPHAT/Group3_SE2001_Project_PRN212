
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.Views
{
    public partial class MenuView : UserControl
    {
        private readonly MenuService _menuService;
        public MenuView()
    : this(new CoffeeRuu.Services.MenuService(
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
            await LoadMenuAsync();
        }

        private async System.Threading.Tasks.Task LoadMenuAsync()
        {
            try
            {
                MenuDataGrid.ItemsSource =
                    await _menuService.GetAllProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải thực đơn: " + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SearchButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var products =
                    await _menuService.GetAllProductsAsync();

                string keyword = SearchBox.Text.Trim();

                MenuDataGrid.ItemsSource = products
                    .Where(p =>
                        p.ProductName != null &&
                        p.ProductName.Contains(
                            keyword,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void AddButton_Click(
            object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Tiếp theo sẽ xây dựng form thêm món uống.",
                "Quản lý thực đơn");
        }

        private void EditButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (MenuDataGrid.SelectedItem is not Product product)
            {
                MessageBox.Show("Vui lòng chọn món cần sửa.");
                return;
            }

            MessageBox.Show(
                $"Bạn đã chọn món: {product.ProductName}",
                "Sửa món uống");
        }

        private async void DeleteButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (MenuDataGrid.SelectedItem is not Product product)
            {
                MessageBox.Show("Vui lòng chọn món cần xóa.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc muốn ẩn món '{product.ProductName}' khỏi thực đơn?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Ẩn món thay vì xóa bản ghi khỏi database
                product.IsAvailable = false;

                await _menuService.UpdateProductAsync(product);

                await LoadMenuAsync();

                MessageBox.Show("Đã ẩn món khỏi thực đơn.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật món: " + ex.Message);
            }
        }
    }
}
