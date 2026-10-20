
using System.Collections.Generic;
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

        // Lấy danh sách món uống
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _menuRepository.GetAllProductsAsync();
        }

        // Lấy thông tin một món uống theo ID
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _menuRepository.GetProductByIdAsync(productId);
        }

        // Thêm món uống mới
        public async Task AddProductAsync(Product product)
        {
            // Kiểm tra dữ liệu đầu vào
            if (product == null)
                throw new System.ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.ProductName))
                throw new System.ArgumentException("Tên món uống không được để trống.");

            await _menuRepository.AddProductAsync(product);
        }

        // Cập nhật thông tin món uống
        public async Task UpdateProductAsync(Product product)
        {
            if (product == null)
                throw new System.ArgumentNullException(nameof(product));

            if (product.ProductId <= 0)
                throw new System.ArgumentException("Mã món uống không hợp lệ.");

            if (string.IsNullOrWhiteSpace(product.ProductName))
                throw new System.ArgumentException("Tên món uống không được để trống.");

            await _menuRepository.UpdateProductAsync(product);
        }

        // Xóa món uống
        public async Task DeleteProductAsync(int productId)
        {
            if (productId <= 0)
                throw new System.ArgumentException("Mã món uống không hợp lệ.");

            await _menuRepository.DeleteProductAsync(productId);
        }
    }
}
