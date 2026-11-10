using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoffeeRuu.Repositories;

public class StockReceiptRepository
{
    private readonly CoffeeRuuDbContext _context;

    public StockReceiptRepository(CoffeeRuuDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(
        CreateStockReceiptRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException(
                "Phiếu nhập phải có ít nhất một nguyên liệu.");

        if (request.SupplierId <= 0 || request.ReceivedBy <= 0)
            throw new ArgumentException(
                "Nhà cung cấp hoặc nhân viên không hợp lệ.");

        foreach (var item in request.Items)
        {
            if (item.IngredientId <= 0 ||
                item.Quantity <= 0 ||
                item.UnitCost < 0)
            {
                throw new ArgumentException(
                    "Thông tin nguyên liệu nhập không hợp lệ.");
            }

            if (item.Quantity != decimal.Round(item.Quantity, 3) ||
                item.UnitCost != decimal.Round(item.UnitCost, 2))
            {
                throw new ArgumentException(
                    "Số lượng tối đa 3 chữ số thập phân; đơn giá tối đa 2.");
            }
        }

        decimal totalCost = decimal.Round(
            request.Items.Sum(x => x.Quantity * x.UnitCost),
            2,
            MidpointRounding.AwayFromZero);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            bool supplierExists = await _context.Suppliers
                .AnyAsync(s =>
                    s.SupplierId == request.SupplierId &&
                    s.IsActive);

            if (!supplierExists)
                throw new InvalidOperationException(
                    "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động.");

            bool employeeExists = await _context.Employees
                .AnyAsync(e =>
                    e.EmployeeId == request.ReceivedBy &&
                    e.IsActive);

            if (!employeeExists)
                throw new InvalidOperationException(
                    "Nhân viên nhập kho không hợp lệ.");

            var receipt = new StockReceipt
            {
                SupplierId = request.SupplierId,
                ReceivedBy = request.ReceivedBy,
                Note = request.Note?.Trim(),
                TotalCost = totalCost
            };

            _context.StockReceipts.Add(receipt);

            await _context.SaveChangesAsync();


            foreach (var item in request.Items)
            {
                _context.StockReceiptDetails.Add(
                    new StockReceiptDetail
                    {
                        StockReceiptId = receipt.StockReceiptId,
                        IngredientId = item.IngredientId,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost
                    });
            }

            var groupedItems = request.Items
                .GroupBy(x => x.IngredientId)
                .Select(g => new
                {
                    IngredientId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            foreach (var item in groupedItems)
            {
                int updatedRows = await _context.Ingredients
                    .Where(i =>
                        i.IngredientId == item.IngredientId &&
                        i.IsActive)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            i => i.CurrentStock,
                            i => i.CurrentStock + item.TotalQuantity));

                if (updatedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Nguyên liệu ID {item.IngredientId} không hợp lệ.");
                }

                _context.InventoryTransactions.Add(
                    new InventoryTransaction
                    {
                        IngredientId = item.IngredientId,
                        EmployeeId = request.ReceivedBy,
                        TransactionType = "StockIn",
                        QuantityChange = item.TotalQuantity,
                        ReferenceType = "StockReceipt",
                        ReferenceId = receipt.StockReceiptId,
                        Note = $"Nhập kho từ phiếu #{receipt.StockReceiptId}"
                    });
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return receipt.StockReceiptId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<List<Supplier>> GetActiveSuppliersAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SupplierName)
            .ToListAsync();
    }

    public async Task<List<Ingredient>> GetActiveIngredientsAsync()
    {
        return await _context.Ingredients
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.IngredientName)
            .ToListAsync();
    }

    public async Task<List<StockReceipt>> GetRecentReceiptsAsync()
    {
        return await _context.StockReceipts
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.ReceivedByNavigation)
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.StockReceiptId)
            .Take(50)
            .ToListAsync();
    }

}
