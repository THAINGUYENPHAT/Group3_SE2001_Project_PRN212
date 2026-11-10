
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class RecipeService
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public RecipeService(Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            using var db = _contextFactory();

            return await db.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.IngredientName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            using var db = _contextFactory();

            return await db.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<ProductSize>> GetSizesAsync(int productId)
        {
            using var db = _contextFactory();

            return await db.ProductSizes
                .Where(s => s.ProductId == productId)
                .AsNoTracking()
                .OrderBy(s => s.ProductSizeId)
                .ToListAsync();
        }

        public async Task<List<ProductRecipe>> GetRecipesAsync(int productSizeId)
        {
            using var db = _contextFactory();

            return await db.ProductRecipes
                .Include(r => r.Ingredient)
                .Where(r => r.ProductSizeId == productSizeId)
                .AsNoTracking()
                .ToListAsync();
        }

        // Trả về null nếu sản phẩm có size nhưng người dùng chưa chọn size.
        public async Task<string?> SaveRecipeAsync(
            int productId,
            int? selectedSizeId,
            int ingredientId,
            decimal quantity)
        {
            using var db = _contextFactory();

            ProductSize? productSize = null;

            if (selectedSizeId.HasValue)
            {
                productSize = await db.ProductSizes
                    .FirstOrDefaultAsync(s =>
                        s.ProductSizeId == selectedSizeId.Value &&
                        s.ProductId == productId);

                if (productSize == null)
                {
                    throw new InvalidOperationException(
                        "Size được chọn không thuộc sản phẩm.");
                }
            }

            if (productSize == null)
            {
                bool hasSize = await db.ProductSizes
                    .AnyAsync(s => s.ProductId == productId);

                if (hasSize)
                {
                    return null;
                }

                productSize = new ProductSize
                {
                    ProductId = productId,
                    SizeName = "Standard",
                    Price = 0
                };

                db.ProductSizes.Add(productSize);
                await db.SaveChangesAsync();
            }

            var existingRecipe = await db.ProductRecipes
                .FirstOrDefaultAsync(r =>
                    r.ProductSizeId == productSize.ProductSizeId &&
                    r.IngredientId == ingredientId);

            string message;

            if (existingRecipe != null)
            {
                existingRecipe.QuantityUsed = quantity;
                message = "Cập nhật định lượng thành công!";
            }
            else
            {
                var newRecipe = new ProductRecipe
                {
                    ProductSizeId = productSize.ProductSizeId,
                    IngredientId = ingredientId,
                    QuantityUsed = quantity
                };

                db.ProductRecipes.Add(newRecipe);
                message = "Thêm nguyên liệu vào công thức thành công!";
            }

            await db.SaveChangesAsync();

            return message;
        }

        public async Task<bool> DeleteRecipeAsync(
            int productSizeId,
            int ingredientId)
        {
            using var db = _contextFactory();

            var recipe = await db.ProductRecipes.FindAsync(
                productSizeId,
                ingredientId);

            if (recipe == null)
            {
                return false;
            }

            db.ProductRecipes.Remove(recipe);
            await db.SaveChangesAsync();

            return true;
        }
    }
}
