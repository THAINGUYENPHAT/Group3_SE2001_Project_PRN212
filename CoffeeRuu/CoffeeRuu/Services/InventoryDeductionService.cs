
using CoffeeRuu.Repositories;
using System;
using System.Threading.Tasks;

namespace CoffeeRuu.Services;

// Quy ước về số lượng topping
public enum ToppingQuantityScope
{
    PerDrink,      // Số topping trên mỗi ly
    PerOrderLine    // Tổng số topping của cả dòng
}

public class InventoryDeductionService
{
    private readonly InventoryDeductionRepository _repository;
    private readonly ToppingQuantityScope _toppingScope;

    public InventoryDeductionService(
        InventoryDeductionRepository repository,
        ToppingQuantityScope toppingScope)
    {
        _repository = repository;
        _toppingScope = toppingScope;
    }

    public async Task<bool> DeductStockForOrderItemAsync(
        int orderItemId,
        int employeeId)
    {
        if (orderItemId <= 0)
            throw new ArgumentException(
                "OrderItemId không hợp lệ.");

        if (employeeId <= 0)
            throw new ArgumentException(
                "EmployeeId không hợp lệ.");

        bool toppingPerDrink =
            _toppingScope == ToppingQuantityScope.PerDrink;

        return await _repository.DeductOrderItemAsync(
            orderItemId,
            employeeId,
            toppingPerDrink);
    }
}
