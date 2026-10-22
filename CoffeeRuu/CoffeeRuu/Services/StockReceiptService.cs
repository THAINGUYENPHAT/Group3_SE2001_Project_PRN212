
using CoffeeRuu.Models;
using CoffeeRuu.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeRuu.Services;

public class StockReceiptService
{
    private readonly StockReceiptRepository _repository;

    public StockReceiptService(
        StockReceiptRepository repository)
    {
        _repository = repository;
    }

    // Lấy danh sách nhà cung cấp
    public Task<List<Supplier>> GetActiveSuppliersAsync()
    {
        return _repository.GetActiveSuppliersAsync();
    }

    // Lấy danh sách nguyên liệu
    public Task<List<Ingredient>> GetActiveIngredientsAsync()
    {
        return _repository.GetActiveIngredientsAsync();
    }

    // Lấy lịch sử nhập hàng
    public Task<List<StockReceipt>> GetRecentReceiptsAsync()
    {
        return _repository.GetRecentReceiptsAsync();
    }

    // Tính tổng tiền phiếu nhập
    public decimal CalculateTotalCost(
        IEnumerable<StockReceiptItemRequest> items)
    {
        if (items == null)
            return 0;

        decimal total = items.Sum(
            item => item.Quantity * item.UnitCost);

        return decimal.Round(
            total,
            2,
            MidpointRounding.AwayFromZero);
    }

    // Tạo phiếu nhập hàng
    public async Task<int> CreateStockReceiptAsync(
        CreateStockReceiptRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Kiểm tra nhà cung cấp
        if (request.SupplierId <= 0)
            throw new ArgumentException(
                "Vui lòng chọn nhà cung cấp.");

        // Kiểm tra nhân viên
        if (request.ReceivedBy <= 0)
            throw new ArgumentException(
                "Không xác định được nhân viên nhập hàng.");

        // Kiểm tra danh sách nguyên liệu
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException(
                "Phiếu nhập phải có ít nhất một nguyên liệu.");

        // Kiểm tra ghi chú
        if (request.Note?.Length > 500)
            throw new ArgumentException(
                "Ghi chú không được vượt quá 500 ký tự.");

        // Kiểm tra từng dòng nguyên liệu
        foreach (var item in request.Items)
        {
            if (item.IngredientId <= 0)
                throw new ArgumentException(
                    "Vui lòng chọn nguyên liệu hợp lệ.");

            if (item.Quantity <= 0)
                throw new ArgumentException(
                    "Số lượng nhập phải lớn hơn 0.");

            if (item.UnitCost < 0)
                throw new ArgumentException(
                    "Đơn giá không được âm.");

            if (decimal.Round(item.Quantity, 3) != item.Quantity)
                throw new ArgumentException(
                    "Số lượng chỉ được có tối đa 3 chữ số thập phân.");

            if (decimal.Round(item.UnitCost, 2) != item.UnitCost)
                throw new ArgumentException(
                    "Đơn giá chỉ được có tối đa 2 chữ số thập phân.");
        }

        // Kiểm tra tổng tiền
        decimal totalCost = CalculateTotalCost(request.Items);

        if (totalCost > 999999999999.99m)
            throw new ArgumentException(
                "Tổng tiền vượt quá giới hạn cho phép.");

        // Gọi Repository thực hiện transaction
        return await _repository.CreateAsync(request);
    }
}
