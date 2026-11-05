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

        // Lấy danh sách món kèm danh mục và giá từng size.
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        // Lấy món theo ID.
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        // Lấy món theo danh mục.
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

        // Tìm món theo tên bắt đầu bằng từ khóa.
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

        // Thêm món mới cùng các size.
        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // Cập nhật thông tin món và các size.
        public async Task UpdateProductAsync(Product product)
        {
            // Lấy bản ghi đang được quản lý bởi DbContext.
            var existingProduct = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(
                    p => p.ProductId == product.ProductId);

            if (existingProduct == null)
            {
                throw new System.InvalidOperationException(
                    "Không tìm thấy món cần cập nhật.");
            }

            // Cập nhật thông tin cơ bản.
            existingProduct.ProductName = product.ProductName;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Description = product.Description;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.IsAvailable = product.IsAvailable;

            // Cập nhật từng size được gửi từ ProductDialog.
            foreach (var incomingSize in product.ProductSizes)
            {
                // Ưu tiên tìm theo ProductSizeId.
                var existingSize = incomingSize.ProductSizeId > 0
                    ? existingProduct.ProductSizes.FirstOrDefault(
                        s => s.ProductSizeId == incomingSize.ProductSizeId)
                    : null;

                // Nếu không tìm thấy theo ID, tìm theo tên size.
                if (existingSize == null)
                {
                    existingSize = existingProduct.ProductSizes
                        .FirstOrDefault(s =>
                            GetSizeCode(s.SizeName) ==
                            GetSizeCode(incomingSize.SizeName));
                }

                if (existingSize != null)
                {
                    // Cập nhật size đã tồn tại.
                    existingSize.SizeName = incomingSize.SizeName;
                    existingSize.Price = incomingSize.Price;
                    existingSize.IsAvailable = incomingSize.IsAvailable;
                }
                else
                {
                    // Thêm size mới nếu món chưa có size này.
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

        // Chuẩn hóa tên size để so sánh S, M, L.
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

        // Xóa mềm: chuyển món sang trạng thái ngừng bán.
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

            // Lấy danh sách danh mục từ database.
public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
    }
