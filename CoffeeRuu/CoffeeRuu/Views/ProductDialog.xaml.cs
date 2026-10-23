
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
                DescriptionBox.Text =
                    _editingProduct.Description ?? "";

                AvailableCheckBox.IsChecked =
                    _editingProduct.IsAvailable;

                PriceSBox.Text = GetPriceText("S");
                PriceMBox.Text = GetPriceText("M");
                PriceLBox.Text = GetPriceText("L");
            }

            Loaded += ProductDialog_Loaded;
        }

        private string GetPriceText(string code)
        {
            var size = FindExistingSize(code);

            return size != null && size.IsAvailable
                ? size.Price.ToString("0.##")
                : "";
        }

        private ProductSize? FindExistingSize(string code)
        {
            return _editingProduct?.ProductSizes.FirstOrDefault(s =>
                s.SizeName.EndsWith(
                    $"({code})",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    s.SizeName,
                    code,
                    StringComparison.OrdinalIgnoreCase));
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

            if (!TryReadPrice(PriceSBox.Text, true, out decimal priceS)
                || !TryReadPrice(PriceMBox.Text, false, out decimal priceM)
                || !TryReadPrice(PriceLBox.Text, false, out decimal priceL))
            {
                MessageBox.Show(
                    "Giá phải là số lớn hơn 0. Giá M và L có thể để trống.",
                    "Giá không hợp lệ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int categoryId = Convert.ToInt32(
                CategoryComboBox.SelectedValue);

            var product = new Product
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

            AddOrUpdateSize(product, "S", "Nhỏ (S)", priceS, true);
            AddOrUpdateSize(product, "M", "Lớn (M)", priceM, false);
            AddOrUpdateSize(product, "L", "Rất lớn (L)", priceL, false);

            ProductResult = product;
            DialogResult = true;
        }

        private bool TryReadPrice(
            string text,
            bool required,
            out decimal price)
        {
            text = text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                price = 0;
                return !required;
            }

            return decimal.TryParse(text, out price) && price > 0;
        }

        private void AddOrUpdateSize(
            Product product,
            string code,
            string sizeName,
            decimal price,
            bool required)
        {
            var oldSize = FindExistingSize(code);

            // Size chưa có và không nhập giá thì bỏ qua
            if (oldSize == null && price <= 0)
                return;

            // Giữ ProductSizeId cũ để không tạo bản ghi trùng
            product.ProductSizes.Add(new ProductSize
            {
                ProductSizeId = oldSize?.ProductSizeId ?? 0,
                ProductId = product.ProductId,
                SizeName = sizeName,
                Price = price > 0 ? price : oldSize?.Price ?? 0,
                IsAvailable = price > 0
            });
        }

        private void CancelButton_Click(
            object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
