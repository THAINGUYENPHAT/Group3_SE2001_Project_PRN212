
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

        // Lấy tất cả món đang kinh doanh
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _menuRepository.GetAllProductsAsync();
        }

        // Lấy món theo danh mục
        public async Task<List<Product>> GetProductsByCategoryAsync(
            int categoryId)
        {
            return await _menuRepository
                .GetProductsByCategoryAsync(categoryId);
        }

        // Tìm món theo tên
        public async Task<List<Product>> SearchProductsAsync(
            string keyword)
        {
            return await _menuRepository
                .SearchProductsAsync(keyword);
        }

        // Lấy chi tiết món theo ID
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _menuRepository
                .GetProductByIdAsync(productId);
        }
        // Thêm sản phẩm
        public async Task AddProductAsync(Product product)
        {
            await _menuRepository.AddProductAsync(product);
        }

        // Cập nhật sản phẩm
        public async Task UpdateProductAsync(Product product)
        {
            await _menuRepository.UpdateProductAsync(product);
        }

        // Xóa sản phẩm
        public async Task DeleteProductAsync(int productId)
        {
            await _menuRepository.DeleteProductAsync(productId);
        }
    }
}
