using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CoffeeRuu.Models;
using CoffeeRuu.Services;

namespace CoffeeRuu.ViewModels
{
    public class PosMenuViewModel : ViewModelBase
    {
        private readonly OrderService _orderService;
        private readonly IPrintService _printService;

        private const string BANK_ID = "MB";
        private const string ACCOUNT_NO = "0123456789";
        private const string ACCOUNT_NAME = "COFFEE RUU";

        public int CurrentEmployeeId { get; set; } = 1;

        #region Danh Sách Menu & Lọc

        private readonly List<MenuChoice> _allMenuItems = new List<MenuChoice>();

        public ObservableCollection<MenuGroupedItem> FilteredMenuItems { get; set; }
            = new ObservableCollection<MenuGroupedItem>();

        public ObservableCollection<TableChoice> Tables { get; set; }
            = new ObservableCollection<TableChoice>();

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                    ApplyMenuFilter();
            }
        }

        private string _selectedCategory = string.Empty;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    ApplyMenuFilter();
            }
        }

        #endregion

        #region Quản lý Giỏ hàng & Thanh toán

        public ObservableCollection<CartLine> Cart { get; set; }
            = new ObservableCollection<CartLine>();

        private TableChoice? _selectedTable;
        public TableChoice? SelectedTable
        {
            get => _selectedTable;
            set => SetProperty(ref _selectedTable, value);
        }

        private string _orderType = "DineIn";
        public string OrderType
        {
            get => _orderType;
            set
            {
                if (SetProperty(ref _orderType, value) && _orderType == "Takeaway")
                {
                    SelectedTable = null;
                }
            }
        }

        private string _orderNote = string.Empty;
        public string OrderNote
        {
            get => _orderNote;
            set => SetProperty(ref _orderNote, value);
        }

        public decimal TotalAmount => Cart.Sum(x => x.LineTotal);

        private decimal _customerPaid;
        public decimal CustomerPaid
        {
            get => _customerPaid;
            set
            {
                if (SetProperty(ref _customerPaid, value))
                {
                    OnPropertyChanged(nameof(ChangeAmount));
                }
            }
        }

        public decimal ChangeAmount => CustomerPaid >= TotalAmount ? CustomerPaid - TotalAmount : 0;

        private string _qrCodeUrl = string.Empty;
        public string QrCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand QuickCashCommand { get; }
        public ICommand CheckoutCommand { get; }

        #endregion

        public PosMenuViewModel(OrderService orderService, IPrintService? printService = null)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _printService = printService ?? new PrintService();

            Cart.CollectionChanged += (s, e) => UpdateCartTotals();

            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            SelectCategoryCommand = new RelayCommand<string>(cat => SelectedCategory = cat ?? string.Empty);
            AddToCartCommand = new RelayCommand<MenuChoice>(choice => AddToCart(choice));
            RemoveFromCartCommand = new RelayCommand<CartLine>(line => RemoveFromCart(line));
            IncreaseQuantityCommand = new RelayCommand<CartLine>(line => ChangeQuantity(line, 1));
            DecreaseQuantityCommand = new RelayCommand<CartLine>(line => ChangeQuantity(line, -1));
            QuickCashCommand = new RelayCommand<object>(amount => SetQuickCash(amount));
            CheckoutCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(), () => CanCheckout());
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var menuList = await _orderService.GetMenuAsync();
                _allMenuItems.Clear();
                if (menuList != null) _allMenuItems.AddRange(menuList);

                var tableList = await _orderService.GetTablesAsync();
                Tables.Clear();
                if (tableList != null)
                {
                    foreach (var tbl in tableList) Tables.Add(tbl);
                }

                ApplyMenuFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu menu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyMenuFilter()
        {
            FilteredMenuItems.Clear();
            var query = _allMenuItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                query = query.Where(x => x.ProductName.StartsWith(SearchKeyword.Trim(), StringComparison.CurrentCultureIgnoreCase));
            }

            var grouped = query
                .GroupBy(x => x.ProductName)
                .Select(g => new MenuGroupedItem
                {
                    ProductName = g.Key,
                    Sizes = g.ToList()
                });

            foreach (var group in grouped)
            {
                FilteredMenuItems.Add(group);
            }
        }

        private void AddToCart(MenuChoice? choice)
        {
            if (choice == null) return;

            var existingLine = Cart.FirstOrDefault(x => x.ProductSizeId == choice.ProductSizeId);
            if (existingLine != null)
            {
                existingLine.Quantity++;
                UpdateCartTotals();
            }
            else
            {
                Cart.Add(new CartLine
                {
                    ProductSizeId = choice.ProductSizeId,
                    ProductName = choice.ProductName,
                    SizeName = choice.SizeName,
                    UnitPrice = choice.Price,
                    Quantity = 1,
                    Note = string.Empty
                });
            }
        }

        private void RemoveFromCart(CartLine? line)
        {
            if (line != null && Cart.Contains(line)) Cart.Remove(line);
        }

        private void ChangeQuantity(CartLine? line, int delta)
        {
            if (line == null) return;
            line.Quantity += delta;
            if (line.Quantity <= 0) Cart.Remove(line);
            else UpdateCartTotals();
        }

        private void SetQuickCash(object? parameter)
        {
            if (parameter != null && decimal.TryParse(parameter.ToString(), out decimal amount))
            {
                CustomerPaid = amount;
            }
        }

        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ChangeAmount));
            UpdateQrCodeUrl();
        }

        private void UpdateQrCodeUrl()
        {
            if (TotalAmount <= 0)
            {
                QrCodeUrl = string.Empty;
                return;
            }

            string addInfo = Uri.EscapeDataString("Thanh toan CoffeeRuu");
            QrCodeUrl = $"https://img.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-compact2.png?amount={TotalAmount:0}&addInfo={addInfo}&accountName={Uri.EscapeDataString(ACCOUNT_NAME)}";
        }

        private bool CanCheckout()
        {
            if (Cart.Count == 0 || TotalAmount <= 0) return false;
            if (OrderType == "DineIn" && SelectedTable == null) return false;
            return true;
        }

        private async Task ExecuteCheckoutAsync()
        {
            try
            {
                Order savedOrder = await _orderService.CreateOrderAsync(
                    cart: Cart.ToList(),
                    orderType: OrderType,
                    tableId: SelectedTable?.TableId,
                    employeeId: CurrentEmployeeId,
                    note: OrderNote
                );

                var orderDto = new OrderDto
                {
                    OrderId = savedOrder.OrderId,
                    OrderCode = savedOrder.OrderCode,
                    CreatedAt = savedOrder.CreatedAt,
                    TotalAmount = savedOrder.TotalAmount,
                    CustomerPaid = CustomerPaid > 0 ? CustomerPaid : TotalAmount,
                    ChangeAmount = ChangeAmount,
                    PaymentMethod = CustomerPaid > 0 ? "Tiền mặt" : "Chuyển khoản QR",
                    Items = Cart.Select(x => new OrderItemDto
                    {
                        ProductName = x.ProductName,
                        SizeName = x.SizeName,
                        Quantity = x.Quantity,
                        Price = x.UnitPrice
                    }).ToList()
                };

                _printService.PrintReceipt(orderDto);

                MessageBox.Show($"Thanh toán thành công đơn hàng #{savedOrder.OrderCode}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                Cart.Clear();
                CustomerPaid = 0;
                OrderNote = string.Empty;
                SelectedTable = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo đơn hàng: {ex.Message}", "Lỗi Thanh Toán", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MenuGroupedItem
    {
        public string ProductName { get; set; } = string.Empty;
        public List<MenuChoice> Sizes { get; set; } = new List<MenuChoice>();
    }
}