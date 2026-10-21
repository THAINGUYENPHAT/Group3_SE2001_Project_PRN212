
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoffeeRuu.Models;
using CoffeeRuu.Data;

namespace CoffeeRuu.Repositories
{
    public class MenuRepository
    {
        private readonly CoffeeRuuDbContext _context;

        public MenuRepository(CoffeeRuuDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách món uống kèm danh mục và size
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsNoTracking()
                .ToListAsync();
        }

        // Lấy một món uống theo ID
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        // Thêm món uống mới
        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // Cập nhật món uống
        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        // Xóa món uống
        public async Task DeleteProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
