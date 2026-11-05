using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;

namespace CoffeeRuu.Services
{
    public class MenuService
    {
        private readonly MenuRepository _menuRepository;

        public MenuService(MenuRepository menuRepository)
        {
            _menuRepository = menuRepository;
        }

        // Lấy tất cả món trong thực đơn.
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _menuRepository.GetAllProductsAsync();
        }

        // Lấy món theo danh mục.
        public async Task<List<Product>> GetProductsByCategoryAsync(
            int categoryId)
        {
            return await _menuRepository.GetProductsByCategoryAsync(
                categoryId);
        }

        // Tìm món theo tên.
        public async Task<List<Product>> SearchProductsAsync(
            string keyword)
        {
            return await _menuRepository.SearchProductsAsync(keyword);
        }

        // Lấy chi tiết món theo ID.
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _menuRepository.GetProductByIdAsync(productId);
        }

        // Kiểm tra các size đang được bán phải có giá lớn hơn 1.000đ.
        private static void ValidatePrices(Product product)
        {
            if (product.ProductSizes == null ||
                !product.ProductSizes.Any(s => s.IsAvailable))
            {
                throw new InvalidOperationException(
                    "Vui lòng nhập ít nhất một kích cỡ và giá bán.");
            }

            foreach (var size in product.ProductSizes.Where(s => s.IsAvailable))
            {
                if (size.Price <= 1000)
                {
                    throw new InvalidOperationException(
                        $"Giá size {size.SizeName} phải lớn hơn 1.000đ.");
                }
            }
        }

        // Kiểm tra tên món có bị trùng hay không.
        private async Task ValidateProductNameAsync(
            string productName,
            int? excludeProductId = null)
        {
            string normalizedName = (productName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new InvalidOperationException(
                    "Tên món không được để trống.");
            }

            var products = await _menuRepository.GetAllProductsAsync();

            bool isDuplicate = products.Any(p =>
                (!excludeProductId.HasValue ||
                 p.ProductId != excludeProductId.Value) &&
                string.Equals(
                    (p.ProductName ?? "").Trim(),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                throw new InvalidOperationException(
                    $"Món \"{normalizedName}\" đã tồn tại. " +
                    "Vui lòng nhập tên món khác.");
            }
        }

        // Thêm món mới.
        public async Task AddProductAsync(Product product)
        {
            await ValidateProductNameAsync(product.ProductName);

            ValidatePrices(product);

            product.ProductName = product.ProductName.Trim();

            await _menuRepository.AddProductAsync(product);
        }

        // Cập nhật thông tin món và giá các size.
        public async Task UpdateProductAsync(Product product)
        {
            await ValidateProductNameAsync(
                product.ProductName,
                product.ProductId);

            // Chỉ kiểm tra giá nếu có danh sách size được gửi lên.
            if (product.ProductSizes != null &&
                product.ProductSizes.Any())
            {
                ValidatePrices(product);
            }

            product.ProductName = product.ProductName.Trim();

            await _menuRepository.UpdateProductAsync(product);
        }

        // Ngừng bán món theo ID.
        public async Task DeleteProductAsync(int productId)
        {
            await _menuRepository.DeleteProductAsync(productId);
        }
    

    // Lấy danh sách danh mục thông qua Repository.
public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _menuRepository.GetAllCategoriesAsync();
        }
    }
}