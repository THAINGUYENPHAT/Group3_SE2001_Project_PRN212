using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

/// <summary>Một dòng doanh thu theo ngày.</summary>
public class DailyRevenueRow
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>Một dòng trong bảng sản phẩm bán chạy.</summary>
public class BestSellingProductRow
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>Kết quả báo cáo cho một khoảng ngày (KPI + hai danh sách).</summary>
public class RevenueReport
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public int OrderCount { get; init; }
    public decimal AverageOrderValue { get; init; }
    public IReadOnlyList<DailyRevenueRow> DailyRows { get; init; } = Array.Empty<DailyRevenueRow>();
    public IReadOnlyList<BestSellingProductRow> ProductRows { get; init; } = Array.Empty<BestSellingProductRow>();
}
