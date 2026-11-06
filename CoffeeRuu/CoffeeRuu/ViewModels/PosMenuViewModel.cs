using CoffeeRuu.Models;
using CoffeeRuu.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeRuu.ViewModels
{
    public class PosMenuViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;
        private readonly OrderService _orderService;
        private readonly IPrintService _printService;

        private bool _isBusy;
        private bool _isLoaded;
        private string _searchKeyword = "";
        private string _selectedCategory = "";
        private string _orderType = "DineIn";
        private TableChoice? _selectedTable;
        private decimal _totalAmount;

        // ID nhân viên đang đăng nhập.
        // Giữ nguyên giá trị hiện tại của màn hình POS.
        public int CurrentEmployeeId { get; set; } = 1;

        // Dữ liệu menu.
        private readonly ObservableCollection<PosMenuCardItem> _allMenuItems = new();

        public ObservableCollection<PosMenuCardItem> FilteredMenuItems { get; } = new();

        public ObservableCollection<PosMenuCategory> Categories { get; } = new();

        // Dữ liệu bàn.
        public ObservableCollection<TableChoice> Tables { get; } = new();

        // Giỏ hàng.
        public ObservableCollection<CartLine> Cart { get; } = new();

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyMenuFilter();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    UpdateCategorySelection();
                    ApplyMenuFilter();
                }
            }
        }

        public string OrderType
        {
            get => _orderType;
            set
            {
                if (SetProperty(ref _orderType, value))
                {
                    if (_orderType == "Takeaway")
                    {
                        SelectedTable = null;
                    }

                    RaiseCheckoutCanExecuteChanged();
                }
            }
        }

        public TableChoice? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (SetProperty(ref _selectedTable, value))
                {
                    RaiseCheckoutCanExecuteChanged();
                }
            }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            private set => SetProperty(ref _totalAmount, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCheckoutCanExecuteChanged();
                }
            }
        }

        // Các callback để View hiển thị hộp thoại.
        public Func<string, string, string?>? ShowItemNoteDialog { get; set; }

        public Func<decimal, bool>? ShowQrPaymentDialog { get; set; }

        public Action<string, string, bool>? ShowMessage { get; set; }

        public Action? NavigateToKds { get; set; }

        // Commands.
        public RelayCommand LoadDataCommand { get; }

        public RelayCommand<string> SelectCategoryCommand { get; }

        public RelayCommand<PosMenuSizePrice> AddToCartCommand { get; }

        public RelayCommand<CartLine> RemoveFromCartCommand { get; }

        public RelayCommand CheckoutCommand { get; }

        public PosMenuViewModel(
            MenuService menuService,
            OrderService orderService,
            IPrintService printService)
        {
            _menuService = menuService;
            _orderService = orderService;
            _printService = printService;

            LoadDataCommand = new RelayCommand(
                async () => await LoadDataAsync());

            SelectCategoryCommand = new RelayCommand<string>(
                category => SelectedCategory = category ?? "");

            AddToCartCommand = new RelayCommand<PosMenuSizePrice>(
                AddToCart);

            RemoveFromCartCommand = new RelayCommand<CartLine>(
                RemoveFromCart);

            CheckoutCommand = new RelayCommand(
                async () => await CheckoutAsync(),
                CanCheckout);

            Cart.CollectionChanged += Cart_CollectionChanged;

            InitializeCategories();
        }

        // ==========================
        // 1. KHỞI TẠO DANH MỤC
        // ==========================

        private void InitializeCategories()
        {
            Categories.Add(new PosMenuCategory("Tất cả", ""));

            Categories.Add(new PosMenuCategory("Cà phê", "Cà phê"));

            Categories.Add(new PosMenuCategory("Trà sữa", "Trà sữa"));

            Categories.Add(new PosMenuCategory("Trà trái cây", "Trà trái cây"));

            Categories.Add(new PosMenuCategory("Đá xay", "Đá xay"));

            Categories.Add(new PosMenuCategory("Đồ ăn vặt", "Đồ ăn vặt"));

            UpdateCategorySelection();
        }

        private void UpdateCategorySelection()
        {
            foreach (var category in Categories)
            {
                category.IsSelected =
                    category.CategoryValue == SelectedCategory;
            }
        }

        // ==========================
        // 2. TẢI DỮ LIỆU
        // ==========================

        private async Task LoadDataAsync()
        {
            if (_isLoaded || IsBusy)
                return;

            IsBusy = true;

            try
            {
                var products = await _menuService.GetAllProductsAsync();

                _allMenuItems.Clear();

                foreach (var product in products)
                {
                    if (!product.IsAvailable)
                        continue;

                    var menuItem = new PosMenuCardItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        CategoryName =
                            product.Category?.CategoryName ?? ""
                    };

                    if (product.ProductSizes != null)
                    {
                        foreach (var size in product.ProductSizes
                            .Where(s => s.IsAvailable)
                            .OrderBy(s => s.Price))
                        {
                            menuItem.SizePrices.Add(
                                new PosMenuSizePrice
                                {
                                    ProductId = product.ProductId,
                                    ProductName = product.ProductName,
                                    ProductSizeId = size.ProductSizeId,
                                    SizeName = size.SizeName,
                                    Price = size.Price
                                });
                        }
                    }

                    if (menuItem.SizePrices.Count > 0)
                    {
                        _allMenuItems.Add(menuItem);
                    }
                }

                var tables = await _orderService.GetTablesAsync();

                Tables.Clear();

                foreach (var table in tables)
                {
                    Tables.Add(table);
                }

                if (Tables.Count > 0)
                {
                    SelectedTable = Tables[0];
                }

                ApplyMenuFilter();

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                ShowMessage?.Invoke(
                    "Không thể tải dữ liệu màn hình POS.\n" + ex.Message,
                    "Lỗi tải dữ liệu",
                    true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==========================
        // 3. TÌM KIẾM VÀ LỌC MENU
        // ==========================

        private void ApplyMenuFilter()
        {
            string keyword = SearchKeyword.Trim();

            var result = _allMenuItems.Where(item =>
            {
                bool matchesCategory =
                    string.IsNullOrEmpty(SelectedCategory) ||
                    string.Equals(
                        item.CategoryName,
                        SelectedCategory,
                        StringComparison.OrdinalIgnoreCase);

                bool matchesKeyword =
                    string.IsNullOrEmpty(keyword) ||
                    item.ProductName.Contains(
                        keyword,
                        StringComparison.CurrentCultureIgnoreCase);

                return matchesCategory && matchesKeyword;
            });

            FilteredMenuItems.Clear();

            foreach (var item in result)
            {
                FilteredMenuItems.Add(item);
            }
        }

        // ==========================
        // 4. GIỎ HÀNG
        // ==========================

        private void AddToCart(PosMenuSizePrice? sizePrice)
        {
            if (sizePrice == null)
                return;

            // Mở hộp thoại ghi chú.
            string? note = ShowItemNoteDialog?.Invoke(
                sizePrice.ProductName,
                sizePrice.SizeName);

            // Người dùng bấm Hủy.
            if (note == null)
                return;

            note = note.Trim();

            // Chỉ gộp khi cùng size và cùng ghi chú.
            var existingItem = Cart.FirstOrDefault(item =>
                item.ProductSizeId == sizePrice.ProductSizeId &&
                item.Note == note);

            if (existingItem != null)
            {
                if (existingItem.Quantity >= 100)
                {
                    ShowMessage?.Invoke(
                        "Số lượng một món không được vượt quá 100.",
                        "Thông báo",
                        false);

                    return;
                }

                existingItem.Quantity++;
            }
            else
            {
                Cart.Add(new CartLine
                {
                    ProductSizeId = sizePrice.ProductSizeId,
                    ProductName = sizePrice.ProductName,
                    SizeName = sizePrice.SizeName,
                    UnitPrice = sizePrice.Price,
                    Quantity = 1,
                    Note = note
                });
            }

            UpdateCartTotals();
        }

        private void RemoveFromCart(CartLine? item)
        {
            if (item == null)
                return;

            Cart.Remove(item);

            UpdateCartTotals();
        }

        private void Cart_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            UpdateCartTotals();
        }

        private void UpdateCartTotals()
        {
            TotalAmount = Cart.Sum(item => item.LineTotal);

            RaiseCheckoutCanExecuteChanged();
        }

        // ==========================
        // 5. THANH TOÁN
        // ==========================

        private bool CanCheckout()
        {
            if (IsBusy || Cart.Count == 0)
                return false;

            if (OrderType == "DineIn" && SelectedTable == null)
                return false;

            return true;
        }

        private async Task CheckoutAsync()
        {
            if (!CanCheckout())
                return;

            if (OrderType != "DineIn" &&
                OrderType != "Takeaway")
            {
                ShowMessage?.Invoke(
                    "Loại đơn hàng không hợp lệ.",
                    "Thông báo",
                    false);

                return;
            }

            int? tableId = null;

            if (OrderType == "DineIn")
            {
                if (SelectedTable == null)
                {
                    ShowMessage?.Invoke(
                        "Vui lòng chọn bàn trước khi thanh toán.",
                        "Thông báo",
                        false);

                    return;
                }

                tableId = SelectedTable.TableId;
            }

            IsBusy = true;

            try
            {
                // 1. Tính tổng tiền.
                decimal totalAmount = TotalAmount;

                // 2. Hiển thị QR và xác nhận đã nhận tiền.
                bool isPaid = ShowQrPaymentDialog?.Invoke(totalAmount) == true;

                if (!isPaid)
                    return;

                // 3. Chuyển giỏ hàng sang dữ liệu mà OrderService yêu cầu.
                var cartLines = Cart.Select(item => new CartLine
                {
                    ProductSizeId = item.ProductSizeId,
                    ProductName = item.ProductName,
                    SizeName = item.SizeName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    Note = item.Note
                }).ToList();

                // 4. Lưu đơn hàng vào database.
                Order savedOrder = await _orderService.CreateOrderAsync(
                    cart: cartLines,
                    orderType: OrderType,
                    tableId: tableId,
                    employeeId: CurrentEmployeeId,
                    note: "");

                // 5. Tạo dữ liệu hóa đơn để in.
                var orderDto = new OrderDto
                {
                    OrderId = savedOrder.OrderId,
                    OrderCode = savedOrder.OrderCode,
                    CreatedAt = savedOrder.CreatedAt,
                    TotalAmount = savedOrder.TotalAmount,
                    CustomerPaid = savedOrder.TotalAmount,
                    ChangeAmount = 0,
                    PaymentMethod = "Chuyển khoản QR",

                    Items = Cart.Select(item => new OrderItemDto
                    {
                        ProductName = item.ProductName,
                        SizeName = item.SizeName,
                        Quantity = item.Quantity,
                        Price = item.UnitPrice,
                        Note = item.Note
                    }).ToList()
                };

                // 6. In hóa đơn.
                _printService.PrintReceipt(orderDto);

                ShowMessage?.Invoke(
                    $"Thanh toán và in hóa đơn thành công đơn hàng #{savedOrder.OrderCode}!",
                    "Thành công",
                    false);

                // 7. Xóa giỏ hàng.
                Cart.Clear();

                UpdateCartTotals();

                // 8. Chuyển sang KDS.
                NavigateToKds?.Invoke();
            }
            catch (Exception ex)
            {
                ShowMessage?.Invoke(
                    "Lỗi trong quá trình thanh toán:\n" + ex.Message,
                    "Lỗi thanh toán",
                    true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RaiseCheckoutCanExecuteChanged()
        {
            CheckoutCommand?.RaiseCanExecuteChanged();
        }
    }

    // ==========================
    // MODEL DÀNH CHO MENU POS
    // ==========================

    public class PosMenuCardItem
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public ObservableCollection<PosMenuSizePrice> SizePrices { get; set; } = new();
    }

    public class PosMenuSizePrice
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public int ProductSizeId { get; set; }

        public string SizeName { get; set; } = "";

        public decimal Price { get; set; }
    }

    public class PosMenuCategory : ViewModelBase
    {
        private bool _isSelected;

        public string CategoryName { get; set; }

        public string CategoryValue { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public PosMenuCategory(
            string categoryName,
            string categoryValue)
        {
            CategoryName = categoryName;
            CategoryValue = categoryValue;
        }
    }
}