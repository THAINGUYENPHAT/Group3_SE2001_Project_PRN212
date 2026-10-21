using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Repositories
{
    public class TableRepository
    {
        private readonly Func<CoffeeRuuDbContext> _contextFactory;

        public TableRepository(Func<CoffeeRuuDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<CafeTable>> GetAllAsync()
        {
            using var context = _contextFactory();
            return await context.CafeTables
                .AsNoTracking()
                .OrderBy(t => t.TableName)
                .ToListAsync();
        }

        public async Task<CafeTable?> GetByIdAsync(int id)
        {
            using var context = _contextFactory();
            return await context.CafeTables
                .FirstOrDefaultAsync(t => t.TableId == id);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            using var context = _contextFactory();
            return await context.CafeTables
                .AnyAsync(t => t.TableName == name && (!excludeId.HasValue || t.TableId != excludeId.Value));
        }
        public async Task AddAsync(CafeTable table)
        {
            using var context = _contextFactory();
            context.CafeTables.Add(table);
            await context.SaveChangesAsync();
        }

        public async Task<bool> UpdateInfoAsync(int id, string name, int capacity)
        {
            using var context = _contextFactory();
            var table = await context.CafeTables.FirstOrDefaultAsync(t => t.TableId == id);
            if (table == null) return false;

            table.TableName = name;
            table.Capacity = capacity;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            using var context = _contextFactory();
            var table = await context.CafeTables.FirstOrDefaultAsync(t => t.TableId == id);
            if (table == null) return false;

            table.TableStatus = status;
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            using var context = _contextFactory();
            var table = await context.CafeTables.FirstOrDefaultAsync(t => t.TableId == id);

            if (table == null) return false;

            bool hasOrders = await context.Orders.AnyAsync(o => o.TableId == id);
            if (hasOrders)
            {
                throw new InvalidOperationException("Không thể xóa bàn này vì đã có lịch sử đơn hàng. Hãy đổi trạng thái thành 'OutOfService'.");
            }

            context.CafeTables.Remove(table);
            await context.SaveChangesAsync();
            return true;
        }
    }
}