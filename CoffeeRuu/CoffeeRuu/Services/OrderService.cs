using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using CoffeeRuu.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace CoffeeRuu.Services
{
    public class OrderService
    {
        private readonly OrderRepository _repo;

        public OrderService(OrderRepository repo)
        {
            _repo = repo;
        }

        public Task<List<MenuChoice>> GetMenuAsync()
            => _repo.GetMenuAsync();

        public Task<List<TableChoice>> GetTablesAsync()
            => _repo.GetTablesAsync();

        public Task<List<OrderListRow>> GetOrdersAsync()
            => _repo.GetOrdersAsync();

        public Task<List<KdsItemRow>> GetKdsAsync()
            => _repo.GetKdsAsync();

        // =================================
        // TẠO ĐƠN HÀNG
        // =================================

        public async Task<Order> CreateOrderAsync(
            IReadOnlyCollection<CartLine> cart,
            string orderType,
            int? tableId,
            int employeeId,
            string? note)
        {
            if (cart.Count == 0)
                throw new Exception("Giỏ hàng đang trống.");

            if (orderType != "Takeaway" &&
                orderType != "DineIn")
                throw new Exception("Loại đơn không hợp lệ.");

            if (orderType == "DineIn" && tableId == null)
                throw new Exception("Vui lòng chọn bàn.");

            if (note?.Length > 500)
                throw new Exception("Ghi chú đơn quá dài.");

            if (cart.Any(x =>
                x.Quantity < 1 ||
                x.Quantity > 100 ||
                (x.Note?.Length ?? 0) > 300))
            {
                throw new Exception(
                    "Số lượng hoặc ghi chú món không hợp lệ.");
            }

            if (orderType == "Takeaway")
                tableId = null;

            await using var db = _repo.CreateContext();

            await using var tx =
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
                    throw new Exception("Bàn không hợp lệ.");
            }

            // Lấy giá từ database, không dùng giá tự nhập
            var sizeIds = cart
                .Select(x => x.ProductSizeId)
                .Distinct()
                .ToList();

            var products = await db.ProductSizes
                .AsNoTracking()
                .Where(s =>
                    sizeIds.Contains(s.ProductSizeId) &&
                    s.IsAvailable &&
                    s.Product.IsAvailable &&
                    s.Product.Category.IsActive)
                .Select(s => new
                {
                    s.ProductSizeId,
                    s.Price,
                    s.SizeName,
                    s.Product.ProductName
                })
                .ToDictionaryAsync(s => s.ProductSizeId);

            if (products.Count != sizeIds.Count)
            {
                throw new Exception(
                    "Có sản phẩm đã ngừng bán. Hãy làm mới menu.");
            }

            DateTime now = DateTime.Now;

            var order = new Order
            {
                OrderCode = "CR" +
                    Guid.NewGuid().ToString("N")[..20],

                TableId = tableId,
                CreatedBy = employeeId,
                OrderType = orderType,
                OrderStatus = "New",
                CreatedAt = now,
                DiscountAmount = 0,
                Note = note
            };

            // Lưu từng món
            foreach (var line in cart)
            {
                var product = products[line.ProductSizeId];

                order.OrderItems.Add(new OrderItem
                {
                    ProductSizeId = line.ProductSizeId,
                    ProductNameSnapshot = product.ProductName,
                    SizeNameSnapshot = product.SizeName,
                    UnitPrice = product.Price,
                    Quantity = line.Quantity,
                    LineTotal = product.Price * line.Quantity,
                    PreparationStatus = "Queued",
                    Note = line.Note
                });
            }

            // Tính tiền
            order.Subtotal = order.OrderItems
                .Sum(i => i.LineTotal);

            order.TotalAmount = order.Subtotal;

            // Ghi lịch sử tạo đơn
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
            await tx.CommitAsync();

            return order;
        }

        // =================================
        // KDS: CẬP NHẬT TRẠNG THÁI MÓN
        // =================================

        public async Task UpdatePreparationAsync(
            int orderItemId,
            string nextStatus,
            int employeeId)
        {
            if (nextStatus != "Preparing" &&
                nextStatus != "Ready")
            {
                throw new Exception("Trạng thái không hợp lệ.");
            }

            await using var db = _repo.CreateContext();

            int? orderId = await db.OrderItems
                .AsNoTracking()
                .Where(i => i.OrderItemId == orderItemId)
                .Select(i => (int?)i.OrderId)
                .FirstOrDefaultAsync();

            if (!orderId.HasValue)
                throw new Exception("Không tìm thấy món.");

            await using var tx =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);

            await LockOrderAsync(db, orderId.Value);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleAsync(o => o.OrderId == orderId.Value);

            if (order.OrderStatus == "Completed" ||
                order.OrderStatus == "Cancelled")
            {
                throw new Exception("Đơn đã kết thúc.");
            }

            var item = order.OrderItems
                .Single(i => i.OrderItemId == orderItemId);

            bool valid =
                (item.PreparationStatus == "Queued" &&
                 nextStatus == "Preparing") ||
                (item.PreparationStatus == "Preparing" &&
                 nextStatus == "Ready");

            if (!valid)
            {
                throw new Exception(
                    $"Không thể chuyển {item.PreparationStatus} " +
                    $"sang {nextStatus}.");
            }

            // Thay đổi trạng thái món
            item.PreparationStatus = nextStatus;

            // Nếu mọi món Ready thì toàn đơn Ready
            string orderStatus =
                order.OrderItems.All(i =>
                    i.PreparationStatus == "Ready")
                    ? "Ready"
                    : "Preparing";

            ChangeOrderStatus(
                db, order, orderStatus, employeeId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // =================================
        // BÀN GIAO ĐƠN
        // =================================

        public async Task CompleteOrderAsync(
            int orderId,
            int employeeId)
        {
            await using var db = _repo.CreateContext();

            await using var tx =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);

            await LockOrderAsync(db, orderId);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn.");

            if (order.OrderStatus != "Ready" ||
                order.OrderItems.Count == 0 ||
                order.OrderItems.Any(i =>
                    i.PreparationStatus != "Ready"))
            {
                throw new Exception(
                    "Chỉ bàn giao khi tất cả món đã pha xong.");
            }

            foreach (var item in order.OrderItems)
            {
                item.PreparationStatus = "Served";
            }

            order.CompletedAt = DateTime.Now;

            ChangeOrderStatus(
                db, order, "Completed", employeeId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // =================================
        // HỦY ĐƠN CHƯA PHA CHẾ
        // =================================

        public async Task CancelNewOrderAsync(
            int orderId,
            int employeeId)
        {
            await using var db = _repo.CreateContext();

            await using var tx =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            await CheckEmployeeAsync(db, employeeId);

            await LockOrderAsync(db, orderId);

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .SingleOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Không tìm thấy đơn.");

            if (order.OrderStatus != "New" ||
                order.OrderItems.Any(i =>
                    i.PreparationStatus != "Queued"))
            {
                throw new Exception(
                    "Chỉ được hủy đơn chưa pha chế.");
            }

            // Không xử lý thanh toán ở module này
            bool isPaid = await db.Payments.AnyAsync(p =>
                p.OrderId == orderId &&
                p.PaymentStatus == "Success");

            if (isPaid)
            {
                throw new Exception(
                    "Đơn đã thanh toán, cần bộ phận Payment xử lý.");
            }

            foreach (var item in order.OrderItems)
            {
                item.PreparationStatus = "Cancelled";
            }

            ChangeOrderStatus(
                db, order, "Cancelled", employeeId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // =================================
        // CÁC HÀM HỖ TRỢ
        // =================================

        private static async Task CheckEmployeeAsync(
            CoffeeRuuDbContext db,
            int employeeId)
        {
            bool valid = employeeId > 0 &&
                await db.Employees.AnyAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

            if (!valid)
                throw new Exception(
                    "EmployeeId không tồn tại hoặc ngừng hoạt động.");
        }

        private static async Task LockOrderAsync(
            CoffeeRuuDbContext db,
            int orderId)
        {
            var connection = db.Database.GetDbConnection();

            await using var command =
                connection.CreateCommand();

            command.Transaction =
                db.Database.CurrentTransaction!
                    .GetDbTransaction();

            command.CommandText = @"
            SELECT OrderId
            FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
            WHERE OrderId = @orderId";

            var parameter = command.CreateParameter();

            parameter.ParameterName = "@orderId";
            parameter.Value = orderId;

            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                throw new Exception("Không tìm thấy đơn.");
        }

        private static void ChangeOrderStatus(
            CoffeeRuuDbContext db,
            Order order,
            string nextStatus,
            int employeeId)
        {
            if (order.OrderStatus == nextStatus)
                return;

            string oldStatus = order.OrderStatus;

            order.OrderStatus = nextStatus;

            db.OrderStatusHistories.Add(
                new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    ChangedBy = employeeId,
                    OldStatus = oldStatus,
                    NewStatus = nextStatus,
                    ChangedAt = DateTime.Now,
                    Note = "Cập nhật trạng thái đơn hàng"
                });
        }
    }
}
