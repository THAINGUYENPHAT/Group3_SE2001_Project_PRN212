
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Repositories
{
    public class SupplierRepository
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public SupplierRepository(
            Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // 1. Lấy tất cả nhà cung cấp
        public async Task<List<Supplier>> GetAllAsync()
        {
            using var context = _contextFactory();

            return await context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
        }

        // 2. Tìm nhà cung cấp theo ID
        public async Task<Supplier?> GetByIdAsync(int id)
        {
            using var context = _contextFactory();

            return await context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierId == id);
        }

        // 3. Lấy danh sách nhà cung cấp đang hoạt động
        public async Task<List<Supplier>> GetActiveAsync()
        {
            using var context = _contextFactory();

            return await context.Suppliers
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
        }

        // 4. Thêm nhà cung cấp
        public async Task AddAsync(Supplier supplier)
        {
            using var context = _contextFactory();

            context.Suppliers.Add(supplier);

            await context.SaveChangesAsync();
        }

        // 5. Cập nhật thông tin nhà cung cấp
        public async Task<bool> UpdateInfoAsync(
            int id,
            string name,
            string? phone,
            string? email,
            string? address)
        {
            using var context = _contextFactory();

            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
                return false;

            supplier.SupplierName = name;
            supplier.Phone = phone;
            supplier.Email = email;
            supplier.Address = address;

            await context.SaveChangesAsync();

            return true;
        }

        // 6. Thay đổi trạng thái nhà cung cấp
        public async Task<bool> SetActiveAsync(
            int id, bool isActive)
        {
            using var context = _contextFactory();

            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
                return false;

            supplier.IsActive = isActive;

            await context.SaveChangesAsync();

            return true;
        }
    }
}
