using System.Collections.Generic;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;

namespace CoffeeRuu.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _categoryRepository;

        public CategoryService(CategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            return await _categoryRepository.GetActiveCategoriesAsync();
        }
    }
}