
using System;
using System.Linq;
using System.Threading.Tasks;
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
            : this(new MenuService(
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

        // READ: Tải danh sách món uống
        private async Task LoadMenuAsync()
        {
            try
            {
                MenuDataGrid.ItemsSource =
                    await _menuService.GetAllProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải thực đơn: " + ex.Message);
            }
        }

        // SEARCH: Tìm theo tên món
        private async void SearchButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var products =
                    await _menuService.GetAllProductsAsync();

                string keyword = SearchBox.Text.Trim();

                MenuDataGrid.ItemsSource = products
                    .Where(p => p.ProductName != null &&
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

        // CREATE: Thêm món uống
        private async void AddButton_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true ||
                dialog.ProductResult == null)
                return;

            try
            {
                await _menuService.AddProductAsync(
                    dialog.ProductResult);

                await LoadMenuAsync();

                MessageBox.Show("Thêm món uống thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể thêm món: " + ex.Message);
            }
        }

        // UPDATE: Sửa món được chọn
        private async void EditButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (MenuDataGrid.SelectedItem is not Product selected)
            {
                MessageBox.Show("Vui lòng chọn món cần sửa.");
                return;
            }

            var dialog = new ProductDialog(selected)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true ||
                dialog.ProductResult == null)
                return;

            try
            {
                await _menuService.UpdateProductAsync(
                    dialog.ProductResult);

                await LoadMenuAsync();

                MessageBox.Show("Cập nhật món uống thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể sửa món: " + ex.Message);
            }
        }

        // DELETE: Ẩn món khỏi thực đơn

        private async void DeleteButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (MenuDataGrid.SelectedItem is not Product product)
            {
                MessageBox.Show("Vui lòng chọn món cần xóa.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa món '{product.ProductName}' không?\n" +
                "Thao tác này sẽ xóa bản ghi món khỏi database.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _menuService.DeleteProductAsync(product.ProductId);

                await LoadMenuAsync();

                MessageBox.Show("Đã xóa món uống thành công.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xóa món uống. Có thể món đang được sử dụng " +
                    "trong size, công thức hoặc đơn hàng.\n\nChi tiết: " +
                    ex.Message,
                    "Lỗi xóa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

    }
}
