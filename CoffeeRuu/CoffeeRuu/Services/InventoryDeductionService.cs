using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class InventoryDeductionService
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public InventoryDeductionService(Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Tự động trừ CurrentStock trong DB khi món được pha chế/bán ra
        /// </summary>
        public async Task DeductStockForOrderItemAsync(CoffeeRuuDbContext db, int orderItemId, int employeeId)
        {
            // 1. Lấy thông tin OrderItem cùng danh sách Topping đi kèm
            var orderItem = await db.OrderItems
                .Include(oi => oi.OrderItemToppings)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem == null) return;

            // 2. Lấy công thức món (ProductRecipe) theo ProductSizeId
            var productRecipes = await db.ProductRecipes
                .Where(pr => pr.ProductSizeId == orderItem.ProductSizeId)
                .ToListAsync();

            // 3. Lấy công thức Topping (ToppingRecipe) nếu món có chọn Topping
            var toppingIds = orderItem.OrderItemToppings.Select(t => t.ToppingId).ToList();
            var toppingRecipes = await db.ToppingRecipes
                .Where(tr => toppingIds.Contains(tr.ToppingId))
                .ToListAsync();

            // 4. Tổng hợp lượng nguyên liệu cần trừ: Key = IngredientId, Value = TotalQuantityNeeded
            var deductionMap = new Dictionary<int, decimal>();

            // Tính nguyên liệu từ Món chính (QuantityUsed * OrderItem.Quantity)
            foreach (var pr in productRecipes)
            {
                decimal amount = pr.QuantityUsed * orderItem.Quantity;
                if (deductionMap.ContainsKey(pr.IngredientId))
                    deductionMap[pr.IngredientId] += amount;
                else
                    deductionMap[pr.IngredientId] = amount;
            }

            // Tính nguyên liệu từ Topping đi kèm (QuantityUsed * Topping.Quantity * OrderItem.Quantity)
            foreach (var itemTopping in orderItem.OrderItemToppings)
            {
                var recipesForTopping = toppingRecipes.Where(tr => tr.ToppingId == itemTopping.ToppingId);
                foreach (var tr in recipesForTopping)
                {
                    decimal amount = tr.QuantityUsed * itemTopping.Quantity * orderItem.Quantity;
                    if (deductionMap.ContainsKey(tr.IngredientId))
                        deductionMap[tr.IngredientId] += amount;
                    else
                        deductionMap[tr.IngredientId] = amount;
                }
            }

            if (!deductionMap.Any()) return;

            // 5. Kiểm tra tồn kho có đủ không
            var ingredientIds = deductionMap.Keys.ToList();
            var ingredients = await db.Ingredients
                .Where(i => ingredientIds.Contains(i.IngredientId))
                .ToDictionaryAsync(i => i.IngredientId);

            foreach (var kvp in deductionMap)
            {
                if (ingredients.TryGetValue(kvp.Key, out var ing))
                {
                    if (ing.CurrentStock < kvp.Value)
                    {
                        throw new InvalidOperationException(
                            $"Không đủ nguyên liệu [{ing.IngredientName}]. Tồn kho: {ing.CurrentStock} {ing.Unit}, cần: {kvp.Value} {ing.Unit}");
                    }
                }
            }

            // 6. Thực hiện trừ kho và ghi Log nhật ký giao dịch kho (InventoryTransactions)
            foreach (var kvp in deductionMap)
            {
                var ing = ingredients[kvp.Key];

                // Trừ trực tiếp vào CurrentStock
                ing.CurrentStock -= kvp.Value;

                // Ghi nhật ký vào bảng InventoryTransactions
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    IngredientId = ing.IngredientId,
                    EmployeeId = employeeId,
                    TransactionType = "SaleDeduction",
                    QuantityChange = -kvp.Value, // Giá trị âm thể hiện xuất kho
                    ReferenceType = "OrderItem",
                    ReferenceId = orderItem.OrderItemId,
                    CreatedAt = DateTime.Now,
                    Note = $"Trừ kho tự động cho món #{orderItem.OrderItemId} ({orderItem.ProductNameSnapshot} - {orderItem.SizeNameSnapshot})"
                });
            }
        }
    }
}