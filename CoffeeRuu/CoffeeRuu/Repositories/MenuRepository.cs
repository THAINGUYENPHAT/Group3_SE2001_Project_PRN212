
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoffeeRuu.Data;
using CoffeeRuu.Models;

namespace CoffeeRuu.Repositories
{
    public class MenuRepository
    {
        private readonly CoffeeRuuDbContext _context;

        public MenuRepository(CoffeeRuuDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách sản phẩm kèm danh mục và giá từng size
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        // Lấy sản phẩm theo ID, kèm danh mục và size
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        // Lấy sản phẩm theo danh mục
        public async Task<List<Product>> GetProductsByCategoryAsync(
            int categoryId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Where(p => p.IsAvailable &&
                            p.CategoryId == categoryId)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        // Tìm sản phẩm theo tên bắt đầu bằng từ khóa
        public async Task<List<Product>> SearchProductsAsync(string keyword)
        {
            keyword = keyword?.Trim() ?? "";

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Where(p => p.IsAvailable);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.ProductName.StartsWith(keyword));
            }

            return await query
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }
        // Thêm sản phẩm
        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // Cập nhật sản phẩm
        public async Task UpdateProductAsync(Product product)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

            if (existingProduct == null)
                return;

            // Chỉ cập nhật các thuộc tính của Product
            existingProduct.ProductName = product.ProductName;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Description = product.Description;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.IsAvailable = product.IsAvailable;

            // Không thay thế ProductSizes ở đây.
            // Các size hiện có được giữ nguyên.

            await _context.SaveChangesAsync();
        }
        // Xóa sản phẩm khỏi database
        public async Task DeleteProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return;

            // Không xóa dữ liệu có thể liên quan đến công thức/order.
            product.IsAvailable = false;

            await _context.SaveChangesAsync();
        }
    }
}
