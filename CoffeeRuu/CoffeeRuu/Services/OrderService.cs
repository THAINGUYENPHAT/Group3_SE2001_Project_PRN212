using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using CoffeeRuu.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoffeeRuu.Services;

public class OrderService
{
    private readonly OrderRepository _repo;

    public OrderService(OrderRepository repo)
    {
        _repo = repo;
    }

    // Các phương thức lấy dữ liệu
    public Task<List<MenuChoice>> GetMenuAsync()
        => _repo.GetMenuAsync();

    public Task<List<TableChoice>> GetTablesAsync()
        => _repo.GetTablesAsync();

    public Task<List<OrderListRow>> GetOrdersAsync()
        => _repo.GetOrdersAsync();

    public Task<List<KdsItemRow>> GetKdsAsync()
        => _repo.GetKdsAsync();

    // ==============================
    // 1. TẠO ĐƠN HÀNG
    // ==============================

    public async Task<Order> CreateOrderAsync(
        IReadOnlyCollection<CartLine> cart,
        string orderType,
        int? tableId,
        int employeeId,
        string? note)
    {
        if (cart.Count == 0)
            throw new InvalidOperationException(
                "Hãy thêm ít nhất một món.");

        if (orderType is not ("DineIn" or "Takeaway"))
            throw new InvalidOperationException(
                "Loại đơn không hợp lệ.");

        if (orderType == "DineIn" && !tableId.HasValue)
            throw new InvalidOperationException(
                "Đơn tại quán phải chọn bàn.");

        if (note?.Length > 500)
            throw new InvalidOperationException(
                "Ghi chú đơn tối đa 500 ký tự.");

        if (cart.Any(x =>
            x.Quantity < 1 ||
            x.Quantity > 100 ||
            (x.Note?.Length ?? 0) > 300))
        {
            throw new InvalidOperationException(
                "Số lượng hoặc ghi chú món không hợp lệ.");
        }

        if (orderType == "Takeaway")
            tableId = null;

        await using var db = _repo.CreateContext();

        await using var tx =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        await RequireEmployeeAsync(db, employeeId);

        // Kiểm tra bàn hợp lệ
        if (tableId.HasValue)
        {
            bool validTable = await db.CafeTables.AnyAsync(
                t => t.TableId == tableId.Value &&
                     t.IsActive &&
                    (t.TableStatus == "Available" ||
                     t.TableStatus == "Occupied"));

            if (!validTable)
                throw new InvalidOperationException(
                    "Bàn không còn hợp lệ.");
        }

        // Lấy giá sản phẩm từ database
        var ids = cart
            .Select(x => x.ProductSizeId)
            .Distinct()
            .ToList();

        var menu = await db.ProductSizes
            .AsNoTracking()
            .Where(s =>
                ids.Contains(s.ProductSizeId) &&
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
            .ToDictionaryAsync(x => x.ProductSizeId);

        if (menu.Count != ids.Count)
            throw new InvalidOperationException(
                "Có món đã ngừng bán. Vui lòng làm mới menu.");

        DateTime now = DateTime.Now;

        // Tạo đơn
        var order = new Order
        {
            OrderCode =
                ($"CR{now:yyMMddHHmmss}{Guid.NewGuid():N}")[..22],

            TableId = tableId,
            CreatedBy = employeeId,
            OrderType = orderType,
            OrderStatus = "New",
            DiscountAmount = 0,
            Note = note,
            CreatedAt = now
        };

        // Thêm từng món
        foreach (var line in cart)
        {
            var product = menu[line.ProductSizeId];

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

        // Tính tổng
        order.Subtotal = order.OrderItems
            .Sum(i => i.LineTotal);

        order.TotalAmount = order.Subtotal;

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
        await tx.CommitAsync();

        return order;
    }

    // ==============================
    // 2. CẬP NHẬT TRẠNG THÁI PHA CHẾ
    // ==============================

    public async Task UpdatePreparationAsync(
        int orderItemId,
        string next,
        int employeeId)
    {
        if (next is not ("Preparing" or "Ready"))
            throw new InvalidOperationException(
                "Trạng thái pha chế không hợp lệ.");

        await using var db = _repo.CreateContext();

        int? orderId = await db.OrderItems
            .AsNoTracking()
            .Where(x => x.OrderItemId == orderItemId)
            .Select(x => (int?)x.OrderId)
            .FirstOrDefaultAsync();

        if (orderId == null)
            throw new InvalidOperationException(
                "Không tìm thấy món.");

        await using var tx =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        await RequireEmployeeAsync(db, employeeId);
        await LockOrderAsync(db, orderId.Value);

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .SingleAsync(o => o.OrderId == orderId.Value);

        if (order.OrderStatus is "Completed" or "Cancelled")
            throw new InvalidOperationException(
                "Đơn đã kết thúc.");

        var item = order.OrderItems
            .Single(i => i.OrderItemId == orderItemId);

        // Kiểm tra chuyển trạng thái hợp lệ
        bool valid =
            (item.PreparationStatus == "Queued" &&
             next == "Preparing")
            ||
            (item.PreparationStatus == "Preparing" &&
             next == "Ready");

        if (!valid)
            throw new InvalidOperationException(
                $"Không thể chuyển {item.PreparationStatus} sang {next}.");

        // Cập nhật trạng thái món
        item.PreparationStatus = next;

        // Cập nhật trạng thái toàn bộ đơn
        string newOrderStatus =
            order.OrderItems.All(i =>
                i.PreparationStatus == "Ready")
                ? "Ready"
                : "Preparing";

        SetOrderStatus(
            db,
            order,
            newOrderStatus,
            employeeId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ==============================
    // 3. BÀN GIAO ĐƠN
    // ==============================

    public async Task CompleteOrderAsync(
        int orderId,
        int employeeId)
    {
        await using var db = _repo.CreateContext();

        await using var tx =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        await RequireEmployeeAsync(db, employeeId);
        await LockOrderAsync(db, orderId);

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .SingleOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new InvalidOperationException(
                "Không tìm thấy đơn.");

        if (order.OrderStatus != "Ready" ||
            order.OrderItems.Count == 0 ||
            order.OrderItems.Any(i =>
                i.PreparationStatus != "Ready"))
        {
            throw new InvalidOperationException(
                "Chỉ bàn giao khi tất cả món đã Ready.");
        }

        foreach (var item in order.OrderItems)
        {
            item.PreparationStatus = "Served";
        }

        order.CompletedAt = DateTime.Now;

        SetOrderStatus(
            db,
            order,
            "Completed",
            employeeId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ==============================
    // 4. HỦY ĐƠN MỚI
    // ==============================

    public async Task CancelNewOrderAsync(
        int orderId,
        int employeeId)
    {
        await using var db = _repo.CreateContext();

        await using var tx =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        await RequireEmployeeAsync(db, employeeId);
        await LockOrderAsync(db, orderId);

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .SingleOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new InvalidOperationException(
                "Không tìm thấy đơn.");

        if (order.OrderStatus != "New" ||
            order.OrderItems.Any(i =>
                i.PreparationStatus != "Queued"))
        {
            throw new InvalidOperationException(
                "Chỉ hủy đơn mới chưa pha chế.");
        }

        // Chỉ kiểm tra payment, không xử lý thanh toán
        bool paid = await db.Payments.AnyAsync(p =>
            p.OrderId == orderId &&
            p.PaymentStatus == "Success");

        if (paid)
            throw new InvalidOperationException(
                "Đơn đã thanh toán. Liên hệ bộ phận Payment.");

        foreach (var item in order.OrderItems)
        {
            item.PreparationStatus = "Cancelled";
        }

        SetOrderStatus(
            db,
            order,
            "Cancelled",
            employeeId);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ==============================
    // 5. KIỂM TRA NHÂN VIÊN
    // ==============================

    private static async Task RequireEmployeeAsync(
        CoffeeRuuDbContext db,
        int employeeId)
    {
        bool exists = employeeId > 0 &&
            await db.Employees.AnyAsync(e =>
                e.EmployeeId == employeeId &&
                e.IsActive);

        if (!exists)
            throw new InvalidOperationException(
                "EmployeeId không hợp lệ.");
    }

    // ==============================
    // 6. KHÓA ĐƠN TRONG TRANSACTION
    // ==============================

    private static async Task LockOrderAsync(
        CoffeeRuuDbContext db,
        int orderId)
    {
        var connection = db.Database.GetDbConnection();

        await using var cmd = connection.CreateCommand();

        cmd.Transaction =
            db.Database.CurrentTransaction!
                .GetDbTransaction();

        cmd.CommandText = @"
            SELECT OrderId
            FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
            WHERE OrderId = @orderId";

        var parameter = cmd.CreateParameter();

        parameter.ParameterName = "@orderId";
        parameter.Value = orderId;

        cmd.Parameters.Add(parameter);

        if (await cmd.ExecuteScalarAsync() is null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy đơn.");
        }
    }

    // ==============================
    // 7. LƯU LỊCH SỬ TRẠNG THÁI
    // ==============================

    private static void SetOrderStatus(
        CoffeeRuuDbContext db,
        Order order,
        string next,
        int employeeId)
    {
        if (order.OrderStatus == next)
            return;

        string old = order.OrderStatus;

        order.OrderStatus = next;

        db.OrderStatusHistories.Add(
            new OrderStatusHistory
            {
                OrderId = order.OrderId,
                ChangedBy = employeeId,
                OldStatus = old,
                NewStatus = next,
                ChangedAt = DateTime.Now,
                Note = "Cập nhật trạng thái đơn"
            });
    }
}