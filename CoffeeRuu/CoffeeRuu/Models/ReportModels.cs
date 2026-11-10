using System;
using System.Collections.Generic;

namespace CoffeeRuu.Models;

public class DailyRevenueRow
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class BestSellingProductRow
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

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
