using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Repositories;

public class OrderRepository
{
    private readonly DbContextOptions<CoffeeRuuDbContext> _options;

    public OrderRepository(
        DbContextOptions<CoffeeRuuDbContext> options)
    {
        _options = options;
    }

    public CoffeeRuuDbContext CreateContext()
    {
        return new CoffeeRuuDbContext(_options);
    }

    // Lấy menu từ database
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

    // Lấy danh sách bàn
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

    // Lấy 100 đơn hàng gần nhất
    public async Task<List<OrderListRow>> GetOrdersAsync()
    {
        await using var db = CreateContext();

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

    // Lấy danh sách món cần hiển thị lên KDS
    public async Task<List<KdsItemRow>> GetKdsAsync()
    {
        await using var db = CreateContext();

        return await db.OrderItems
            .AsNoTracking()
            .Where(i =>
                (i.Order.OrderStatus == "New" ||
                 i.Order.OrderStatus == "Preparing" ||
                 i.Order.OrderStatus == "Ready")
                &&
                (i.PreparationStatus == "Queued" ||
                 i.PreparationStatus == "Preparing" ||
                 i.PreparationStatus == "Ready"))
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
}