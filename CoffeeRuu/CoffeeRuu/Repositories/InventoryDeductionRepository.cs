
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeRuu.Repositories;

public class InventoryDeductionRepository
{
    private readonly Func<CoffeeRuuDbContext> _contextFactory;

    public InventoryDeductionRepository(
        Func<CoffeeRuuDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> DeductOrderItemAsync(
        int orderItemId,
        int employeeId,
        bool toppingQuantityPerDrink)
    {
        await using var db = _contextFactory();

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        try
        {
            // 1. Khóa OrderItem để tránh xử lý đồng thời
            // SQL Server: giữ UPDLOCK đến hết transaction
            var items = await db.OrderItems
                .FromSqlInterpolated(
                    $"SELECT * FROM dbo.OrderItems WITH (UPDLOCK, HOLDLOCK) WHERE OrderItemId = {orderItemId}")
                .AsNoTracking()
                .ToListAsync();

            var orderItem = items.SingleOrDefault()
                ?? throw new InvalidOperationException(
                    "Không tìm thấy OrderItem.");

            if (orderItem.Quantity <= 0)
                throw new InvalidOperationException(
                    "Số lượng món không hợp lệ.");

            // 2. Kiểm tra đã trừ kho chưa
            bool alreadyDeducted =
                await db.InventoryTransactions.AnyAsync(t =>
                    t.TransactionType == "SaleDeduction" &&
                    t.ReferenceType == "OrderItem" &&
                    t.ReferenceId == orderItemId);

            if (alreadyDeducted)
            {
                await transaction.CommitAsync();
                return false;
            }

            // 3. Kiểm tra nhân viên
            bool employeeExists = await db.Employees
                .AnyAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

            if (!employeeExists)
                throw new InvalidOperationException(
                    "Nhân viên không hợp lệ.");

            // 4. Lấy công thức của sản phẩm theo size
            var productRecipes = await db.ProductRecipes
                .AsNoTracking()
                .Where(r =>
                    r.ProductSizeId == orderItem.ProductSizeId)
                .ToListAsync();

            if (productRecipes.Count == 0)
                throw new InvalidOperationException(
                    "Sản phẩm chưa được thiết lập công thức.");

            // 5. Lấy topping của OrderItem
            var toppings = await db.OrderItemToppings
                .AsNoTracking()
                .Where(t => t.OrderItemId == orderItemId)
                .ToListAsync();

            var toppingIds = toppings
                .Select(t => t.ToppingId)
                .Distinct()
                .ToArray();

            // 6. Lấy công thức topping
            var toppingRecipes = await db.ToppingRecipes
                .AsNoTracking()
                .Where(r => toppingIds.Contains(r.ToppingId))
                .ToListAsync();

            // IngredientId -> Tổng số lượng cần trừ
            var required = new Dictionary<int, decimal>();

            void AddRequired(int ingredientId, decimal quantity)
            {
                if (quantity <= 0)
                    throw new InvalidOperationException(
                        "Định lượng nguyên liệu phải lớn hơn 0.");

                if (!required.TryAdd(ingredientId, quantity))
                    required[ingredientId] += quantity;
            }

            // 7. Tính nguyên liệu của món chính
            foreach (var recipe in productRecipes)
            {
                decimal quantity =
                    recipe.QuantityUsed * orderItem.Quantity;

                AddRequired(recipe.IngredientId, quantity);
            }

            // 8. Tính nguyên liệu của topping
            foreach (var topping in toppings)
            {
                if (topping.Quantity <= 0)
                    throw new InvalidOperationException(
                        "Số lượng topping không hợp lệ.");

                var recipes = toppingRecipes
                    .Where(r => r.ToppingId == topping.ToppingId)
                    .ToList();

                if (recipes.Count == 0)
                    throw new InvalidOperationException(
                        $"Topping ID {topping.ToppingId} chưa có công thức.");

                decimal multiplier = toppingQuantityPerDrink
                    ? orderItem.Quantity
                    : 1;

                foreach (var recipe in recipes)
                {
                    decimal quantity =
                        recipe.QuantityUsed *
                        topping.Quantity *
                        multiplier;

                    AddRequired(recipe.IngredientId, quantity);
                }
            }

            // 9. Kiểm tra tồn kho trước khi cập nhật
            var ingredientIds = required.Keys.ToArray();

            var ingredients = await db.Ingredients
                .AsNoTracking()
                .Where(i => ingredientIds.Contains(i.IngredientId))
                .ToDictionaryAsync(i => i.IngredientId);

            foreach (var pair in required)
            {
                if (!ingredients.TryGetValue(
                    pair.Key, out var ingredient))
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy nguyên liệu ID {pair.Key}.");
                }

                if (!ingredient.IsActive)
                    throw new InvalidOperationException(
                        $"Nguyên liệu {ingredient.IngredientName} đã ngừng hoạt động.");

                if (ingredient.CurrentStock < pair.Value)
                    throw new InvalidOperationException(
                        $"Không đủ {ingredient.IngredientName}. " +
                        $"Hiện có: {ingredient.CurrentStock}, " +
                        $"cần: {pair.Value}.");
            }

            // 10. Trừ kho và ghi lịch sử
            foreach (var pair in required)
            {
                int ingredientId = pair.Key;
                decimal quantity = pair.Value;

                // Điều kiện CurrentStock >= quantity
                // giúp ngăn trừ kho xuống âm khi có
                // nhiều thao tác cập nhật đồng thời.
                int updatedRows = await db.Ingredients
                    .Where(i =>
                        i.IngredientId == ingredientId &&
                        i.IsActive &&
                        i.CurrentStock >= quantity)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            i => i.CurrentStock,
                            i => i.CurrentStock - quantity));

                if (updatedRows != 1)
                    throw new InvalidOperationException(
                        $"Không thể trừ kho nguyên liệu ID {ingredientId}. " +
                        "Tồn kho có thể đã thay đổi.");

                db.InventoryTransactions.Add(
                    new InventoryTransaction
                    {
                        IngredientId = ingredientId,
                        EmployeeId = employeeId,
                        TransactionType = "SaleDeduction",
                        QuantityChange = -quantity,
                        ReferenceType = "OrderItem",
                        ReferenceId = orderItemId,
                        Note = $"Trừ kho cho OrderItem #{orderItemId}"
                    });
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
