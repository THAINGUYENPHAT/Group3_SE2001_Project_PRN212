
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Views
{
    public partial class ProductDialog : Window
    {
        private readonly Product? _editingProduct;

        public Product? ProductResult { get; private set; }

        public ProductDialog(Product? product = null)
        {
            InitializeComponent();

            _editingProduct = product;

            if (_editingProduct != null)
            {
                TitleText.Text = "Sửa món uống";
                ProductNameBox.Text = _editingProduct.ProductName;
                DescriptionBox.Text = _editingProduct.Description ?? "";
                AvailableCheckBox.IsChecked = _editingProduct.IsAvailable;
            }

            Loaded += ProductDialog_Loaded;
        }

        private async void ProductDialog_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = App.CreateDbContext();

                var categories = await db.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                CategoryComboBox.ItemsSource = categories;

                if (_editingProduct != null)
                {
                    CategoryComboBox.SelectedValue =
                        _editingProduct.CategoryId;
                }
                else if (categories.Count > 0)
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh mục: " + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(
            object sender, RoutedEventArgs e)
        {
            string name = ProductNameBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên món uống.");
                ProductNameBox.Focus();
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn danh mục.");
                return;
            }

            int categoryId = Convert.ToInt32(
                CategoryComboBox.SelectedValue);

            ProductResult = new Product
            {
                ProductId = _editingProduct?.ProductId ?? 0,
                ProductName = name,
                CategoryId = categoryId,
                Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,
                ImageUrl = _editingProduct?.ImageUrl,
                IsAvailable = AvailableCheckBox.IsChecked == true,
                CreatedAt = _editingProduct?.CreatedAt ?? DateTime.Now
            };

            DialogResult = true;
        }

        private void CancelButton_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
