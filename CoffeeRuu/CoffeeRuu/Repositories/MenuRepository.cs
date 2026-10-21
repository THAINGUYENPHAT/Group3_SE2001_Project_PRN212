using System;
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

        // 1. Lấy toàn bộ món, danh mục và size/giá.
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsNoTracking()
                .ToListAsync();
        }

        // 2. Lấy món theo ID.
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        // 3. Thêm món mới cùng danh sách size/giá.
        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // 4. Cập nhật món và size/giá.
        public async Task UpdateProductAsync(Product product)
        {
            var existing = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(
                    p => p.ProductId == product.ProductId);

            if (existing == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy món cần cập nhật.");
            }

            existing.ProductName = product.ProductName;
            existing.CategoryId = product.CategoryId;
            existing.Description = product.Description;
            existing.ImageUrl = product.ImageUrl;
            existing.IsAvailable = product.IsAvailable;

            foreach (var incomingSize in product.ProductSizes)
            {
                if (incomingSize.ProductSizeId > 0)
                {
                    var existingSize = existing.ProductSizes
                        .FirstOrDefault(
                            s => s.ProductSizeId ==
                                 incomingSize.ProductSizeId);

                    if (existingSize == null)
                    {
                        throw new InvalidOperationException(
                            "Không tìm thấy size cần cập nhật.");
                    }

                    existingSize.SizeName = incomingSize.SizeName;
                    existingSize.Price = incomingSize.Price;
                    existingSize.IsAvailable =
                        incomingSize.IsAvailable;
                }
                else
                {
                    existing.ProductSizes.Add(new ProductSize
                    {
                        SizeName = incomingSize.SizeName,
                        Price = incomingSize.Price,
                        IsAvailable = incomingSize.IsAvailable
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // 5. Xóa món theo ID.
        public async Task DeleteProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}