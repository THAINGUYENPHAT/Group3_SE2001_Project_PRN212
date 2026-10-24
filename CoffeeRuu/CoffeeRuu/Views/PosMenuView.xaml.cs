using CoffeeRuu.Models;
using CoffeeRuu.Services;
using CoffeeRuu.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CoffeeRuu.Views
{
    public partial class PosMenuView : UserControl
    {
        private readonly MenuService _menuService;
        private readonly OrderService _orderService;
        private readonly PrintService _printService;

        // ID Nhân viên đang đăng nhập (ví dụ cố định hoặc truyền từ Session)
        public int CurrentEmployeeId { get; set; } = 1;

        // Danh sách sản phẩm gốc từ database
        private readonly ObservableCollection<PosMenuCardItem> _allProducts
            = new ObservableCollection<PosMenuCardItem>();

        // Danh sách sản phẩm đang hiển thị
        private readonly ObservableCollection<PosMenuCardItem> _filteredProducts
            = new ObservableCollection<PosMenuCardItem>();

        // Danh sách món trong hóa đơn hiện tại (Giỏ hàng)
        public ObservableCollection<PosOrderItem> CartItems { get; set; }
            = new ObservableCollection<PosOrderItem>();

        private string _selectedCategory = "";

        public PosMenuView()
        {
            InitializeComponent();

            var dbContext = App.CreateDbContext();
            _menuService = new MenuService(new Repositories.MenuRepository(dbContext));
            _orderService = new OrderService(new Repositories.OrderRepository(App.CreateDbContext));
            _printService = new PrintService();

            // Gắn ItemsSource cho danh sách món và hóa đơn
            MenuCardItemsControl.ItemsSource = _filteredProducts;
            InvoiceDataGrid.ItemsSource = CartItems;

            Loaded += PosMenuView_Loaded;
        }

        private async void PosMenuView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var products = await _menuService.GetAllProductsAsync();

                _allProducts.Clear();

                foreach (var product in products)
                {
                    if (!product.IsAvailable)
                        continue;

                    var item = new PosMenuCardItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        CategoryName = product.Category?.CategoryName ?? ""
                    };

                    if (product.ProductSizes != null)
                    {
                        foreach (var size in product.ProductSizes
                            .Where(s => s.IsAvailable)
                            .OrderBy(s => s.Price))
                        {
                            item.SizePrices.Add(new PosMenuSizePrice
                            {
                                ProductSizeId = size.ProductSizeId,
                                SizeName = size.SizeName,
                                Price = size.Price
                            });
                        }
                    }

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

        #region Xử lý Giỏ hàng & Thanh toán

        // Thêm món vào hóa đơn khi click nút chọn Size (➕)
        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PosMenuSizePrice sizePrice)
            {
                var parentCard = FindParent<Border>(button);
                if (parentCard?.DataContext is PosMenuCardItem product)
                {
                    var existingItem = CartItems.FirstOrDefault(x =>
                        x.ProductId == product.ProductId &&
                        x.ProductSizeId == sizePrice.ProductSizeId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                        InvoiceDataGrid.Items.Refresh();
                    }
                    else
                    {
                        CartItems.Add(new PosOrderItem
                        {
                            ProductId = product.ProductId,
                            ProductSizeId = sizePrice.ProductSizeId,
                            ProductName = product.ProductName,
                            SizeName = sizePrice.SizeName,
                            Price = sizePrice.Price,
                            Quantity = 1
                        });
                    }

                    UpdateTotalAmount();
                }
            }
        }

        // Xóa món khỏi hóa đơn (❌)
        private void RemoveFromCartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PosOrderItem item)
            {
                CartItems.Remove(item);
                UpdateTotalAmount();
            }
        }

        // Cập nhật tổng tiền
        private void UpdateTotalAmount()
        {
            decimal total = CartItems.Sum(x => x.TotalPrice);
            TotalAmountText.Text = $"{total:N0} đ";
        }

        // XỬ LÝ THANH TOÁN VÀ IN HÓA ĐƠN
        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 món để thanh toán!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Chuyển đổi giỏ hàng sang danh sách CartLine
                var cartLines = CartItems.Select(x => new CartLine
                {
                    ProductSizeId = x.ProductSizeId,
                    ProductName = x.ProductName,
                    SizeName = x.SizeName,
                    UnitPrice = x.Price,
                    Quantity = x.Quantity
                }).ToList();

                // 2. Gọi OrderService để tạo đơn hàng vào CSDL
                Order savedOrder = await _orderService.CreateOrderAsync(
                    cart: cartLines,
                    orderType: "Takeaway", // Hoặc "DineIn" tùy chọn
                    tableId: null,
                    employeeId: CurrentEmployeeId,
                    note: ""
                );

                // 3. Chuẩn bị DTO và tiến hành in hóa đơn
                var orderDto = new OrderDto
                {
                    OrderId = savedOrder.OrderId,
                    OrderCode = savedOrder.OrderCode,
                    CreatedAt = savedOrder.CreatedAt,
                    TotalAmount = savedOrder.TotalAmount,
                    CustomerPaid = savedOrder.TotalAmount,
                    ChangeAmount = 0,
                    Items = CartItems.Select(x => new OrderItemDto
                    {
                        ProductName = x.ProductName,
                        SizeName = x.SizeName,
                        Quantity = x.Quantity,
                        Price = x.Price
                    }).ToList()
                };

                _printService.PrintReceipt(orderDto);

                MessageBox.Show($"Thanh toán thành công đơn hàng #{savedOrder.OrderCode}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // 4. Xóa giỏ hàng sau khi hoàn tất
                CartItems.Clear();
                UpdateTotalAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi trong quá trình thanh toán: {ex.Message}", "Lỗi Thanh Toán", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Lọc & Tìm kiếm Sản phẩm

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            _selectedCategory = button.Tag?.ToString() ?? "";

            foreach (var item in FindButtons(this))
            {
                bool isSelected = (item.Tag?.ToString() ?? "") == _selectedCategory;

                item.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isSelected ? "#4E342E" : "#D7CCC8"));

                item.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isSelected ? "#FFFFFF" : "#3E2723"));
            }

            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string keyword = SearchTextBox?.Text?.Trim() ?? "";

            _filteredProducts.Clear();

            var result = _allProducts.Where(p =>
            {
                bool matchesCategory =
                    string.IsNullOrEmpty(_selectedCategory) ||
                    string.Equals(
                        p.CategoryName,
                        _selectedCategory,
                        StringComparison.OrdinalIgnoreCase);

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

        #endregion

        #region Hàm tiện ích VisualTree

        private static IEnumerable<Button> FindButtons(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button && button.Tag != null)
                {
                    yield return button;
                }

                foreach (var nested in FindButtons(child))
                    yield return nested;
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        #endregion
    }

    #region Models hỗ trợ Dữ liệu Giao diện

    public class PosMenuCardItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";

        public ObservableCollection<PosMenuSizePrice> SizePrices { get; set; }
            = new ObservableCollection<PosMenuSizePrice>();
    }

    public class PosMenuSizePrice
    {
        public int ProductSizeId { get; set; }
        public string SizeName { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class PosOrderItem
    {
        public int ProductId { get; set; }
        public int ProductSizeId { get; set; }
        public string ProductName { get; set; } = "";
        public string SizeName { get; set; } = "";
        public string DisplayName => string.IsNullOrEmpty(SizeName) ? ProductName : $"{ProductName} ({SizeName})";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
    }

    #endregion
}