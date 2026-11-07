using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class InventoryService
    {
        private readonly IngredientRepository _ingredientRepository;
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        // Constructor 1 tham số dành cho các View/ViewModel khởi tạo trực tiếp
        // Sử dụng khởi tạo CoffeeRuuDbContext trực tiếp thay vì gọi qua ingredientRepository.CreateContext
        public InventoryService(IngredientRepository ingredientRepository)
            : this(ingredientRepository, () => new CoffeeRuuDbContext())
        {
        }


        // Constructor 2 tham số (đã có sẵn)
        public InventoryService(
            IngredientRepository ingredientRepository,
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _ingredientRepository = ingredientRepository;
            _contextFactory = contextFactory;
        }
        

        // ==========================================
        // 1. QUẢN LÝ NGUYÊN LIỆU (INGREDIENTS)
        // ==========================================

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            return await _ingredientRepository.GetAllAsync();
        }

        public async Task AddIngredientAsync(
            string name,
            string unit,
            decimal minimumStock)
        {
            ValidateIngredient(name, unit, minimumStock);

            name = name.Trim();
            unit = unit.Trim();

            bool exists = await _ingredientRepository.NameExistsAsync(name);

            if (exists)
                throw new InvalidOperationException("Tên nguyên liệu đã tồn tại.");

            var ingredient = new Ingredient
            {
                IngredientName = name,
                Unit = unit,
                CurrentStock = 0,
                MinimumStock = minimumStock,
                IsActive = true
            };

            try
            {
                await _ingredientRepository.AddAsync(ingredient);
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyError(ex))
            {
                throw new InvalidOperationException("Tên nguyên liệu đã tồn tại.", ex);
            }
        }

        public async Task UpdateIngredientAsync(
            int id,
            string name,
            string unit,
            decimal minimumStock)
        {
            ValidateIngredient(name, unit, minimumStock);

            name = name.Trim();
            unit = unit.Trim();

            var ingredient = await _ingredientRepository.GetByIdAsync(id);

            if (ingredient == null)
                throw new KeyNotFoundException("Không tìm thấy nguyên liệu.");

            bool exists = await _ingredientRepository.NameExistsAsync(name, id);

            if (exists)
                throw new InvalidOperationException("Tên nguyên liệu đã tồn tại.");

            try
            {
                bool updated = await _ingredientRepository.UpdateInfoAsync(id, name, unit, minimumStock);

                if (!updated)
                    throw new KeyNotFoundException("Không tìm thấy nguyên liệu.");
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyError(ex))
            {
                throw new InvalidOperationException("Tên nguyên liệu đã tồn tại.", ex);
            }
        }

        public async Task SetIngredientActiveAsync(int id, bool isActive)
        {
            bool updated = await _ingredientRepository.SetActiveAsync(id, isActive);

            if (!updated)
                throw new KeyNotFoundException("Không tìm thấy nguyên liệu.");
        }

        // ==========================================
        // 2. CÔNG THỨC MÓN (PRODUCT RECIPES)
        // ==========================================

        /// <summary>
        /// Lấy danh sách ProductSize hiển thị lên giao diện chọn công thức
        /// </summary>
        public async Task<List<ProductSize>> GetProductSizesAsync()
        {
            await using var db = _contextFactory();
            return await db.ProductSizes
                .Include(ps => ps.Product)
                .Where(ps => ps.IsAvailable && ps.Product.IsAvailable)
                .OrderBy(ps => ps.Product.ProductName)
                .ThenBy(ps => ps.SizeName)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách định lượng công thức theo ProductSizeId
        /// </summary>
        public async Task<List<RecipeItemDto>> GetRecipeByProductSizeIdAsync(int productSizeId)
        {
            await using var db = _contextFactory();
            return await db.ProductRecipes
                .Include(r => r.Ingredient)
                .Where(r => r.ProductSizeId == productSizeId)
                .Select(r => new RecipeItemDto
                {
                    ProductSizeId = r.ProductSizeId,
                    IngredientId = r.IngredientId,
                    IngredientName = r.Ingredient.IngredientName,
                    Unit = r.Ingredient.Unit,
                    QuantityUsed = r.QuantityUsed
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lưu hoặc cập nhật toàn bộ công thức định lượng cho 1 ProductSizeId
        /// </summary>
        public async Task SaveProductRecipeAsync(int productSizeId, List<ProductRecipe> recipes)
        {
            await using var db = _contextFactory();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var existingRecipes = await db.ProductRecipes
                    .Where(r => r.ProductSizeId == productSizeId)
                    .ToListAsync();

                db.ProductRecipes.RemoveRange(existingRecipes);

                if (recipes != null && recipes.Any())
                {
                    foreach (var item in recipes)
                    {
                        if (item.QuantityUsed <= 0)
                            throw new ArgumentException("Số lượng sử dụng nguyên liệu phải lớn hơn 0.");

                        db.ProductRecipes.Add(new ProductRecipe
                        {
                            ProductSizeId = productSizeId,
                            IngredientId = item.IngredientId,
                            QuantityUsed = item.QuantityUsed
                        });
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==========================================
        // 3. VALIDATION & UTILITIES
        // ==========================================

        private void ValidateIngredient(
            string name,
            string unit,
            decimal minimumStock)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên nguyên liệu không được để trống.");

            if (name.Trim().Length > 120)
                throw new ArgumentException("Tên nguyên liệu không được vượt quá 120 ký tự.");

            string cleanName = name.Trim();

            if (!Regex.IsMatch(cleanName, @"\p{L}"))
            {
                throw new ArgumentException("Tên nguyên liệu phải chứa ít nhất một chữ cái.");
            }

            if (!Regex.IsMatch(cleanName, @"^[\p{L}\p{M}0-9 .()/+%-]+$"))
            {
                throw new ArgumentException("Tên nguyên liệu chứa ký tự không hợp lệ.");
            }

            if (!char.IsLetterOrDigit(cleanName[0]))
            {
                throw new ArgumentException("Tên nguyên liệu không được bắt đầu bằng ký tự đặc biệt.");
            }

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Đơn vị tính không được để trống.");

            if (unit.Trim().Length > 30)
                throw new ArgumentException("Đơn vị tính không được vượt quá 30 ký tự.");

            string cleanUnit = unit.Trim();

            if (!Regex.IsMatch(cleanUnit, @"^[\p{L}\p{M}0-9\s./%°²³-]+$"))
            {
                throw new ArgumentException("Đơn vị tính chứa ký tự không hợp lệ.");
            }

            if (!Regex.IsMatch(cleanUnit, @"[\p{L}%]"))
            {
                throw new ArgumentException("Đơn vị tính không hợp lệ.");
            }

            if (minimumStock < 0)
                throw new ArgumentException("Tồn kho tối thiểu không được âm.");

            if (minimumStock > 999999999.999m || decimal.Round(minimumStock, 3) != minimumStock)
                throw new ArgumentException("Tồn kho tối thiểu phải phù hợp decimal(12,3).");
        }

        private static bool IsDuplicateKeyError(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}