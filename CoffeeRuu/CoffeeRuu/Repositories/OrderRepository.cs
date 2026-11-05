
using CoffeeRuu.Data;
using CoffeeRuu.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeRuu.Repositories
{
    public class OrderRepository
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public OrderRepository(
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(
                    nameof(contextFactory));
        }

        public CoffeeRuuDbContext CreateContext()
        {
            return _contextFactory();
        }

        // ============================
        // 1. DANH SÁCH SẢN PHẨM
        // ============================

        public async Task<List<MenuChoice>> GetMenuAsync()
        {
            await using var db = CreateContext();

            return await db.ProductSizes
                .AsNoTracking()
                .Where(s =>
                    s.IsAvailable &&
                    s.Product.IsAvailable &&
                    s.Product.Category.IsActive)
                .OrderBy(s => s.Product.ProductName)
                .ThenBy(s => s.SizeName)
                .Select(s => new MenuChoice
                {
                    ProductSizeId = s.ProductSizeId,
                    ProductName = s.Product.ProductName,
                    SizeName = s.SizeName,
                    Price = s.Price
                })
                .ToListAsync();
        }

        // ============================
        // 2. DANH SÁCH BÀN
        // ============================

        public async Task<List<TableChoice>> GetTablesAsync()
        {
            await using var db = CreateContext();

            return await db.CafeTables
                .AsNoTracking()
                .Where(t =>
                    t.IsActive &&
                    (t.TableStatus == "Available" ||
                     t.TableStatus == "Occupied"))
                .OrderBy(t => t.TableName)
                .Select(t => new TableChoice
                {
                    TableId = t.TableId,
                    TableName = t.TableName
                })
                .ToListAsync();
        }

        // ============================
        // 3. DANH SÁCH ĐƠN HÀNG
        // ============================

        public async Task<List<OrderListRow>> GetOrdersAsync()
        {
            await using var db = CreateContext();

            // Vẫn lấy tất cả trạng thái để
            // có thể xem lịch sử bằng bộ lọc.
            return await db.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.OrderId)
                .Take(100)
                .Select(o => new OrderListRow
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,

                    TableName = o.Table != null
                        ? o.Table.TableName
                        : "Mang đi",

                    OrderType = o.OrderType,
                    OrderStatus = o.OrderStatus,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }

        // ============================
        // 4. DANH SÁCH KDS
        // ============================

        public async Task<List<KdsItemRow>> GetKdsAsync()
        {
            await using var db = CreateContext();

            return await db.OrderItems
                .AsNoTracking()
                .Where(i =>
                    (
                        i.Order.OrderStatus == "New" ||
                        i.Order.OrderStatus == "Preparing" ||
                        i.Order.OrderStatus == "Ready"
                    )
                    &&
                    (
                        i.PreparationStatus == "Queued" ||
                        i.PreparationStatus == "Preparing"
                    ))
                .OrderBy(i => i.Order.CreatedAt)
                .ThenBy(i => i.OrderItemId)
                .Select(i => new KdsItemRow
                {
                    OrderItemId = i.OrderItemId,
                    OrderId = i.OrderId,
                    OrderCode = i.Order.OrderCode,

                    TableName = i.Order.Table != null
                        ? i.Order.Table.TableName
                        : "Mang đi",

                    ProductName = i.ProductNameSnapshot,
                    SizeName = i.SizeNameSnapshot,
                    Quantity = i.Quantity,
                    Note = i.Note ?? "",
                    PreparationStatus = i.PreparationStatus,
                    CreatedAt = i.Order.CreatedAt
                })
                .ToListAsync();
        }

        // ============================
        // 5. DANH SÁCH NHÂN VIÊN
        // ============================

        public async Task<List<EmployeeChoice>> GetEmployeesAsync()
        {
            await using var db = CreateContext();

            return await db.Employees
                .AsNoTracking()
                .Where(e => e.IsActive)
                .Join(
                    db.Roles,
                    e => e.RoleId,
                    r => r.RoleId,
                    (e, r) => new EmployeeChoice
                    {
                        EmployeeId = e.EmployeeId,
                        FullName = e.FullName,
                        RoleName = r.RoleName
                    })
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();
        }
    }
}
