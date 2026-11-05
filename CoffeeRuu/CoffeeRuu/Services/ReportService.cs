using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoffeeRuu.Data;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Services
{
    /// <summary>
    /// Truy vấn và tính toán báo cáo doanh thu / sản phẩm bán chạy.
    /// </summary>
    public class ReportService
    {
        private readonly Func<CoffeeRuuDbContext> _dbContextFactory;

        public ReportService(Func<CoffeeRuuDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory
                ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<RevenueReport> GetReportAsync(DateTime startDate, DateTime endDate)
        {
            DateTime start = startDate.Date;
            DateTime endExclusive = endDate.Date.AddDays(1);

            await using CoffeeRuuDbContext db = _dbContextFactory();

            // Quy tắc nghiệp vụ: chỉ tính đơn Completed có ít nhất một thanh toán Success
            // và không có thanh toán Refunded. Ngày báo cáo = CompletedAt (nếu null thì CreatedAt).
            var validOrders = await db.Orders
                .AsNoTracking()
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                .Where(o => o.OrderStatus == "Completed"
                    && o.Payments.Any(p => p.PaymentStatus == "Success")
                    && !o.Payments.Any(p => p.PaymentStatus == "Refunded"))
                .Where(o => (o.CompletedAt ?? o.CreatedAt) >= start
                    && (o.CompletedAt ?? o.CreatedAt) < endExclusive)
                .ToListAsync();

            decimal totalRevenue = validOrders.Sum(o => o.TotalAmount);
            int orderCount = validOrders.Count;
            decimal averageOrder = orderCount == 0 ? 0 : totalRevenue / orderCount;

            List<DailyRevenueRow> dailyRows = validOrders
                .GroupBy(o => (o.CompletedAt ?? o.CreatedAt).Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyRevenueRow
                {
                    Date = g.Key,
                    DateLabel = g.Key.ToString("dd/MM/yyyy"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Sản phẩm bán chạy: gộp theo TÊN sản phẩm (mọi size), bỏ các món đã Cancelled.
            List<BestSellingProductRow> productRows = validOrders
                .SelectMany(o => o.OrderItems)
                .Where(i => i.PreparationStatus != "Cancelled")
                .GroupBy(i => i.ProductNameSnapshot)
                .Select(g => new
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.LineTotal)
                })
                .OrderByDescending(p => p.QuantitySold)
                .ThenByDescending(p => p.Revenue)
                .ThenBy(p => p.ProductName)
                .Select((p, index) => new BestSellingProductRow
                {
                    Rank = index + 1,
                    ProductName = p.ProductName,
                    QuantitySold = p.QuantitySold,
                    Revenue = p.Revenue
                })
                .ToList();

            return new RevenueReport
            {
                StartDate = start,
                EndDate = endDate.Date,
                TotalRevenue = totalRevenue,
                OrderCount = orderCount,
                AverageOrderValue = averageOrder,
                DailyRows = dailyRows,
                ProductRows = productRows
            };
        }
    }
}
