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

        // 1. Lấy tất cả nguyên liệu
        public async Task<List<Ingredient>> GetAllAsync()
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.IngredientName)
                .ToListAsync();
        }

        // 2. Tìm nguyên liệu theo ID
        public async Task<Ingredient?> GetByIdAsync(int id)
        {
            using var context = _contextFactory();

            return await context.Ingredients
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IngredientId == id);
        }

        // 3. Kiểm tra tên nguyên liệu đã tồn tại
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

        // 4. Thêm nguyên liệu
        public async Task AddAsync(Ingredient ingredient)
        {
            using var context = _contextFactory();

            context.Ingredients.Add(ingredient);

            await context.SaveChangesAsync();
        }

        // 5. Cập nhật thông tin nguyên liệu
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

        // 6. Thay đổi trạng thái hoạt động
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
    }
}
