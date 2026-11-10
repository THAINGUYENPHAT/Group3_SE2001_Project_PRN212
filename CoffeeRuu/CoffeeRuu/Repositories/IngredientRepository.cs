using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Repositories
{
    public class IngredientRepository
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public IngredientRepository(
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Ingredient>> GetAllAsync()
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.IngredientName)
                .ToListAsync();
        }

        public async Task<Ingredient?> GetByIdAsync(int id)
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IngredientId == id);
        }

        public async Task<bool> NameExistsAsync(
            string name, int? excludeId = null)
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AnyAsync(i =>
                    i.IngredientName == name &&
                    (!excludeId.HasValue ||
                     i.IngredientId != excludeId.Value));
        }

        public async Task AddAsync(Ingredient ingredient)
        {
            using var context = _contextFactory();

            context.Ingredients.Add(ingredient);

            await context.SaveChangesAsync();
        }

        public async Task<bool> UpdateInfoAsync(
            int id,
            string name,
            string unit,
            decimal minimumStock)
        {
            using var context = _contextFactory();

            var ingredient = await context.Ingredients
                .FirstOrDefaultAsync(i => i.IngredientId == id);

            if (ingredient == null)
                return false;

            ingredient.IngredientName = name;
            ingredient.Unit = unit;
            ingredient.MinimumStock = minimumStock;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetActiveAsync(
            int id, bool isActive)
        {
            using var context = _contextFactory();

            var ingredient = await context.Ingredients
                .FirstOrDefaultAsync(i => i.IngredientId == id);

            if (ingredient == null)
                return false;

            ingredient.IsActive = isActive;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<List<LowStockAlertItem>> GetLowStockAsync()
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AsNoTracking()
                .Where(i =>
                    i.IsActive &&
                    i.CurrentStock <= i.MinimumStock)
                .OrderBy(i => i.CurrentStock == 0 ? 0 : 1)
                .ThenBy(i => i.IngredientName)
                .Select(i => new LowStockAlertItem
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CurrentStock = i.CurrentStock,
                    MinimumStock = i.MinimumStock
                })
                .ToListAsync();
        }
    }
}
