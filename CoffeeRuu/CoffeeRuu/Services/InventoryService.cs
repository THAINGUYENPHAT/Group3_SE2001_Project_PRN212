
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    public class InventoryService
    {
        private readonly IngredientRepository _ingredientRepository;

        public InventoryService(
            IngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository;
        }

        // 1. Lấy danh sách nguyên liệu
        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            return await _ingredientRepository.GetAllAsync();
        }

        // 2. Thêm nguyên liệu mới
        public async Task AddIngredientAsync(
            string name,
            string unit,
            decimal minimumStock)
        {
            ValidateIngredient(name, unit, minimumStock);

            name = name.Trim();
            unit = unit.Trim();

            bool exists = await _ingredientRepository
                .NameExistsAsync(name);

            if (exists)
                throw new InvalidOperationException(
                    "Tên nguyên liệu đã tồn tại.");

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
            catch (DbUpdateException ex)
                when (IsDuplicateKeyError(ex))
            {
                throw new InvalidOperationException(
                    "Tên nguyên liệu đã tồn tại.", ex);
            }
        }

        // 3. Cập nhật nguyên liệu
        public async Task UpdateIngredientAsync(
            int id,
            string name,
            string unit,
            decimal minimumStock)
        {
            ValidateIngredient(name, unit, minimumStock);

            name = name.Trim();
            unit = unit.Trim();

            var ingredient = await _ingredientRepository
                .GetByIdAsync(id);

            if (ingredient == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy nguyên liệu.");

            bool exists = await _ingredientRepository
                .NameExistsAsync(name, id);

            if (exists)
                throw new InvalidOperationException(
                    "Tên nguyên liệu đã tồn tại.");

            try
            {
                bool updated = await _ingredientRepository
                    .UpdateInfoAsync(id, name, unit, minimumStock);

                if (!updated)
                    throw new KeyNotFoundException(
                        "Không tìm thấy nguyên liệu.");
            }
            catch (DbUpdateException ex)
                when (IsDuplicateKeyError(ex))
            {
                throw new InvalidOperationException(
                    "Tên nguyên liệu đã tồn tại.", ex);
            }
        }

        // 4. Kích hoạt hoặc ngừng sử dụng nguyên liệu
        public async Task SetIngredientActiveAsync(
            int id, bool isActive)
        {
            bool updated = await _ingredientRepository
                .SetActiveAsync(id, isActive);

            if (!updated)
                throw new KeyNotFoundException(
                    "Không tìm thấy nguyên liệu.");
        }

        // 5. Kiểm tra dữ liệu đầu vào
        private void ValidateIngredient(
            string name,
            string unit,
            decimal minimumStock)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Tên nguyên liệu không được để trống.");

            if (name.Trim().Length > 120)
                throw new ArgumentException(
                    "Tên nguyên liệu không được vượt quá 120 ký tự.");

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException(
                    "Đơn vị tính không được để trống.");

            if (unit.Trim().Length > 30)
                throw new ArgumentException(
                    "Đơn vị tính không được vượt quá 30 ký tự.");

            if (minimumStock < 0)
                throw new ArgumentException(
                    "Tồn kho tối thiểu không được âm.");

            if (minimumStock > 999999999.999m ||
                decimal.Round(minimumStock, 3) != minimumStock)
                throw new ArgumentException(
                    "Tồn kho tối thiểu phải phù hợp decimal(12,3).");
        }

        // 6. Nhận diện lỗi trùng khóa từ SQL Server
        private static bool IsDuplicateKeyError(
            DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 ||
                    sqlEx.Number == 2627);
        }
    }
}
