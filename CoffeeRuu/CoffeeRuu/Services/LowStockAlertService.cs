using System.Collections.Generic;
using System.Threading.Tasks;
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;

namespace CoffeeRuu.Services
{
    public class LowStockAlertService
    {
        private readonly IngredientRepository _ingredientRepository;

        public LowStockAlertService(
            IngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository;
        }

        public Task<List<LowStockAlertItem>> GetAlertsAsync()
        {
            return _ingredientRepository.GetLowStockAsync();
        }
    }
}