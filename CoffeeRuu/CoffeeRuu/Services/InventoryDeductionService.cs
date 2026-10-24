
using CoffeeRuu.Data;
using CoffeeRuu.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CoffeeRuu.Services;

// Quy ước về số lượng topping
public enum ToppingQuantityScope
{
    PerDrink,
    PerOrderLine
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

    // Phương thức F6.2 gốc
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

    // F6.2 + F4: sử dụng transaction của KDS
    public async Task<bool> DeductStockForOrderItemAsync(
        CoffeeRuuDbContext db,
        int orderItemId,
        int employeeId)
    {
        if (db == null)
            throw new ArgumentNullException(nameof(db));

        if (orderItemId <= 0)
            throw new ArgumentException(
                "OrderItemId không hợp lệ.");

        if (employeeId <= 0)
            throw new ArgumentException(
                "EmployeeId không hợp lệ.");

        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException(
                "Trừ kho phải được thực hiện trong transaction.");

        bool toppingPerDrink =
            _toppingScope == ToppingQuantityScope.PerDrink;

        return await _repository.DeductOrderItemAsync(
            db,
            orderItemId,
            employeeId,
            toppingPerDrink);
    }
}
