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

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

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

        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            var existingProduct = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(
                    p => p.ProductId == product.ProductId);

            if (existingProduct == null)
            {
                throw new System.InvalidOperationException(
                    "Không tìm thấy món cần cập nhật.");
            }

            existingProduct.ProductName = product.ProductName;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Description = product.Description;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.IsAvailable = product.IsAvailable;

            foreach (var incomingSize in product.ProductSizes)
            {
                var existingSize = incomingSize.ProductSizeId > 0
                    ? existingProduct.ProductSizes.FirstOrDefault(
                        s => s.ProductSizeId == incomingSize.ProductSizeId)
                    : null;

                if (existingSize == null)
                {
                    existingSize = existingProduct.ProductSizes
                        .FirstOrDefault(s =>
                            GetSizeCode(s.SizeName) ==
                            GetSizeCode(incomingSize.SizeName));
                }

                if (existingSize != null)
                {
                    existingSize.SizeName = incomingSize.SizeName;
                    existingSize.Price = incomingSize.Price;
                    existingSize.IsAvailable = incomingSize.IsAvailable;
                }
                else
                {
                    existingProduct.ProductSizes.Add(new ProductSize
                    {
                        ProductId = existingProduct.ProductId,
                        SizeName = incomingSize.SizeName,
                        Price = incomingSize.Price,
                        IsAvailable = incomingSize.IsAvailable
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private static string GetSizeCode(string sizeName)
        {
            if (string.IsNullOrWhiteSpace(sizeName))
                return "";

            string normalized = sizeName.Trim().ToUpperInvariant();

            if (normalized.EndsWith("(S)"))
                return "S";

            if (normalized.EndsWith("(M)"))
                return "M";

            if (normalized.EndsWith("(L)"))
                return "L";

            return normalized;
        }

        public async Task DeleteProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                throw new System.InvalidOperationException(
                    "Không tìm thấy món cần ngừng bán.");
            }

            product.IsAvailable = false;

            await _context.SaveChangesAsync();
        }

public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
    }
