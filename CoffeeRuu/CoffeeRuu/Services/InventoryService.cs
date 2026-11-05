
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

            string cleanName = name.Trim();

            // Tên phải chứa ít nhất một chữ cái
            if (!Regex.IsMatch(cleanName, @"\p{L}"))
            {
                throw new ArgumentException(
                    "Tên nguyên liệu phải chứa ít nhất một chữ cái.");
            }

            // Không cho ký tự ngoài danh sách quy định
            if (!Regex.IsMatch(
                cleanName,
                @"^[\p{L}\p{M}0-9 .()/+%-]+$"))
            {
                throw new ArgumentException(
                    "Tên nguyên liệu chứa ký tự không hợp lệ.");
            }

            // Ký tự đầu tiên phải là chữ hoặc số
            if (!char.IsLetterOrDigit(cleanName[0]))
            {
                throw new ArgumentException(
                    "Tên nguyên liệu không được bắt đầu bằng ký tự đặc biệt.");
            }

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException(
                    "Đơn vị tính không được để trống.");

            if (unit.Trim().Length > 30)
                throw new ArgumentException(
                    "Đơn vị tính không được vượt quá 30 ký tự.");


            string cleanUnit = unit.Trim();

            // Cho phép chữ cái, chữ số, khoảng trắng
            // và các ký hiệu đơn vị đo thông dụng.
            if (!Regex.IsMatch(
                cleanUnit,
                @"^[\p{L}\p{M}0-9\s./%°²³-]+$"))
            {
                throw new ArgumentException(
                    "Đơn vị tính chứa ký tự không hợp lệ.");
            }

            // Phải có chữ cái hoặc một ký hiệu đơn vị đo độc lập (%).
            // Các ký tự như /, -, . không đủ để tạo đơn vị.
            if (!Regex.IsMatch(cleanUnit, @"[\p{L}%]"))
            {
                throw new ArgumentException(
                    "Đơn vị tính không hợp lệ.");
            }


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
