using CoffeeRuu.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using CoffeeRuu.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace CoffeeRuu.Services
{
    public class OrderService
    {
        private readonly OrderRepository _repository;

        public OrderService(OrderRepository repository)
        {
            _repository = repository;
        }

        // ==========================
        // LẤY DỮ LIỆU
        // ==========================

        public Task<List<MenuChoice>> GetMenuAsync()
        {
            return _repository.GetMenuAsync();
        }

        public Task<List<TableChoice>> GetTablesAsync()
        {
            return _repository.GetTablesAsync();
        }

        public Task<List<OrderListRow>> GetOrdersAsync()
        {
            return _repository.GetOrdersAsync();
        }

        public Task<List<KdsItemRow>> GetKdsAsync()
        {
            return _repository.GetKdsAsync();
        }

        // ==========================
        // 1. TẠO ĐƠN HÀNG
        // ==========================

        public async Task<Order> CreateOrderAsync(
            IReadOnlyCollection<CartLine> cart,
            string orderType,
            int? tableId,
            int employeeId,
            string? note)
        {
            if (cart == null || cart.Count == 0)
                throw new InvalidOperationException(
                    "Giỏ hàng đang trống.");

            if (orderType != "DineIn" &&
                orderType != "Takeaway")
                throw new InvalidOperationException(
                    "Loại đơn không hợp lệ.");

            if (orderType == "DineIn" && !tableId.HasValue)
                throw new InvalidOperationException(
                    "Vui lòng chọn bàn.");

            if ((note?.Length ?? 0) > 500)
                throw new InvalidOperationException(
                    "Ghi chú đơn tối đa 500 ký tự.");

            if (cart.Any(i =>
                i.Quantity <= 0 ||
                i.Quantity > 100 ||
                (i.Note?.Length ?? 0) > 300))
            {
                throw new InvalidOperationException(
                    "Số lượng hoặc ghi chú món không hợp lệ.");
            }

            if (orderType == "Takeaway")
                tableId = null;

            await using var db =
                _repository.CreateContext();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);

            // Kiểm tra bàn
            if (tableId.HasValue)
            {
                bool tableValid = await db.CafeTables
                    .AnyAsync(t =>
                        t.TableId == tableId.Value &&
                        t.IsActive &&
                        (t.TableStatus == "Available" ||
                         t.TableStatus == "Occupied"));

                if (!tableValid)
                    throw new InvalidOperationException(
                        "Bàn không hợp lệ hoặc ngừng hoạt động.");
            }

            // Lấy giá và sản phẩm thực tế
            var productSizeIds = cart
                .Select(i => i.ProductSizeId)
                .Distinct()
                .ToList();

            var products = await db.ProductSizes
                .AsNoTracking()
                .Where(s =>
                    productSizeIds.Contains(s.ProductSizeId) &&
                    s.IsAvailable &&
                    s.Product.IsAvailable &&
                    s.Product.Category.IsActive)
                .Select(s => new
                {
                    s.ProductSizeId,
                    s.SizeName,
                    s.Price,
                    s.Product.ProductName
                })
                .ToDictionaryAsync(s => s.ProductSizeId);

            if (products.Count != productSizeIds.Count)
                throw new InvalidOperationException(
                    "Có sản phẩm không còn bán. Hãy làm mới menu.");

            var now = DateTime.Now;

            var order = new Order
            {
                OrderCode = "CR" +
                    Guid.NewGuid().ToString("N")[..18],

                TableId = tableId,
                CreatedBy = employeeId,
                OrderType = orderType,
                OrderStatus = "New",

                Subtotal = 0,
                DiscountAmount = 0,
                TotalAmount = 0,

                Note = note?.Trim(),
                CreatedAt = now
            };

            // Thêm món vào đơn
            foreach (var cartItem in cart)
            {
                var product =
                    products[cartItem.ProductSizeId];

                decimal lineTotal =
                    product.Price * cartItem.Quantity;

                order.OrderItems.Add(new OrderItem
                {
                    ProductSizeId =
                        cartItem.ProductSizeId,

                    ProductNameSnapshot =
                        product.ProductName,

                    SizeNameSnapshot =
                        product.SizeName,

                    UnitPrice = product.Price,
                    Quantity = cartItem.Quantity,
                    LineTotal = lineTotal,

                    PreparationStatus = "Queued",
                    Note = (cartItem.Note ?? "").Trim()
                });
            }

            // Tính tổng tiền
            order.Subtotal = order.OrderItems
                .Sum(i => i.LineTotal);

            order.TotalAmount =
                order.Subtotal - order.DiscountAmount;

            // Lưu lịch sử tạo đơn
            order.OrderStatusHistories.Add(
                new OrderStatusHistory
                {
                    ChangedBy = employeeId,
                    OldStatus = null,
                    NewStatus = "New",
                    ChangedAt = now,
                    Note = "Tạo đơn hàng"
                });

            db.Orders.Add(order);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return order;
        }

        // ==========================
        // 2. KDS: CẬP NHẬT PHA CHẾ
        // ==========================

        public async Task UpdatePreparationAsync(
            int orderItemId,
            string nextStatus,
            int employeeId)
        {
            if (nextStatus != "Preparing" &&
                nextStatus != "Ready")
            {
                throw new InvalidOperationException(
                    "Trạng thái pha chế không hợp lệ.");
            }

            await using var db =
                _repository.CreateContext();

            // Xác định đơn hàng chứa món
            int? orderId = await db.OrderItems
                .AsNoTracking()
                .Where(i => i.OrderItemId == orderItemId)
                .Select(i => (int?)i.OrderId)
                .FirstOrDefaultAsync();

            if (!orderId.HasValue)
                throw new InvalidOperationException(
                    "Không tìm thấy món.");

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);
            await LockOrderAsync(db, orderId.Value);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o =>
                    o.OrderId == orderId.Value);

            if (order == null)
                throw new InvalidOperationException(
                    "Không tìm thấy đơn hàng.");

            if (order.OrderStatus != "New" &&
                order.OrderStatus != "Preparing" &&
                order.OrderStatus != "Ready")
            {
                throw new InvalidOperationException(
                    "Đơn hàng không còn được pha chế.");
            }

            var item = order.OrderItems
                .SingleOrDefault(i =>
                    i.OrderItemId == orderItemId);

            if (item == null)
                throw new InvalidOperationException(
                    "Món không thuộc đơn hàng này.");

            // Chỉ cho chuyển theo thứ tự
            bool isValid =
                (item.PreparationStatus == "Queued" &&
                 nextStatus == "Preparing") ||
                (item.PreparationStatus == "Preparing" &&
                 nextStatus == "Ready");

            if (!isValid)
                throw new InvalidOperationException(
                    $"Không thể chuyển từ " +
                    $"{item.PreparationStatus} sang {nextStatus}.");

            item.PreparationStatus = nextStatus;

            // Tính lại trạng thái tổng của đơn
            bool allReady = order.OrderItems
                .All(i => i.PreparationStatus == "Ready");

            string newOrderStatus = allReady
                ? "Ready"
                : "Preparing";

            ChangeOrderStatus(
                db,
                order,
                newOrderStatus,
                employeeId,
                "Cập nhật tiến độ pha chế");

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // ==========================
        // 3. BÀN GIAO ĐƠN
        // ==========================

        public async Task CompleteOrderAsync(
            int orderId,
            int employeeId)
        {
            await using var db =
                _repository.CreateContext();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);
            await LockOrderAsync(db, orderId);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o =>
                    o.OrderId == orderId);

            if (order == null)
                throw new InvalidOperationException(
                    "Không tìm thấy đơn hàng.");

            if (order.OrderStatus != "Ready")
                throw new InvalidOperationException(
                    "Đơn chưa sẵn sàng để bàn giao.");

            if (order.OrderItems.Count == 0 ||
                order.OrderItems.Any(i =>
                    i.PreparationStatus != "Ready"))
            {
                throw new InvalidOperationException(
                    "Vẫn còn món chưa pha xong.");
            }

            // Cập nhật các món đã giao
            foreach (var item in order.OrderItems)
            {
                item.PreparationStatus = "Served";
            }

            order.CompletedAt = DateTime.Now;

            ChangeOrderStatus(
                db,
                order,
                "Completed",
                employeeId,
                "Đã bàn giao đơn hàng");

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // ==========================
        // 4. HỦY ĐƠN MỚI
        // ==========================

        public async Task CancelNewOrderAsync(
            int orderId,
            int employeeId)
        {
            await using var db =
                _repository.CreateContext();

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);
            await LockOrderAsync(db, orderId);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o =>
                    o.OrderId == orderId);

            if (order == null)
                throw new InvalidOperationException(
                    "Không tìm thấy đơn hàng.");

            if (order.OrderStatus != "New" ||
                order.OrderItems.Any(i =>
                    i.PreparationStatus != "Queued"))
            {
                throw new InvalidOperationException(
                    "Chỉ được hủy đơn chưa pha chế.");
            }

            // Không hủy đơn đã có giao dịch thanh toán
            bool hasPayment = await db.Payments
                .AnyAsync(p => p.OrderId == orderId);

            if (hasPayment)
                throw new InvalidOperationException(
                    "Đơn có giao dịch thanh toán. " +
                    "Cần bộ phận Payment xử lý.");

            foreach (var item in order.OrderItems)
            {
                item.PreparationStatus = "Cancelled";
            }

            ChangeOrderStatus(
                db,
                order,
                "Cancelled",
                employeeId,
                "Hủy đơn chưa pha chế");

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // ==========================
        // 5. KIỂM TRA NHÂN VIÊN
        // ==========================

        private static async Task CheckEmployeeAsync(
            CoffeeRuuDbContext db,
            int employeeId)
        {
            bool valid = employeeId > 0 &&
                await db.Employees.AnyAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

            if (!valid)
                throw new InvalidOperationException(
                    "EmployeeId không hợp lệ hoặc ngừng hoạt động.");
        }

        // ==========================
        // 6. KHÓA ĐƠN KHI CẬP NHẬT
        // ==========================

        private static async Task LockOrderAsync(
            CoffeeRuuDbContext db,
            int orderId)
        {
            var connection =
                db.Database.GetDbConnection();

            await using var command =
                connection.CreateCommand();

            command.Transaction =
                db.Database.CurrentTransaction!
                    .GetDbTransaction();

            command.CommandText = @"
                SELECT OrderId
                FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = @id";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@id";
            parameter.Value = orderId;

            command.Parameters.Add(parameter);

            var result =
                await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException(
                    "Không tìm thấy đơn hàng.");
        }

        // ==========================
        // 7. LƯU LỊCH SỬ TRẠNG THÁI
        // ==========================

        private static void ChangeOrderStatus(
            CoffeeRuuDbContext db,
            Order order,
            string nextStatus,
            int employeeId,
            string note)
        {
            string oldStatus = order.OrderStatus;

            if (oldStatus == nextStatus)
                return;

            order.OrderStatus = nextStatus;

            db.OrderStatusHistories.Add(
                new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    ChangedBy = employeeId,
                    OldStatus = oldStatus,
                    NewStatus = nextStatus,
                    ChangedAt = DateTime.Now,
                    Note = note
                });
        }
    }
}
