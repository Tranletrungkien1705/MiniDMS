using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;
using MiniDMS.Models.ViewModels;

namespace MiniDMS.Services;

public interface IReportService
{
    Task<List<DebtRow>> GetDebtReportAsync(DateTime? from, DateTime? to);
    Task<List<SalesReportRow>> GetSalesReportAsync(DateTime from, DateTime to);
    Task<DashboardSummary> GetDashboardAsync();
}

public record SalesRow(string OrderNo, string CustomerName, DateTime OrderDate, decimal TotalAmount, string Status);

public record DashboardSummary(
    int TotalProducts,
    int LowStockCount,
    decimal TodayRevenue,
    int PendingOrders,
    List<(string Label, decimal Value)> MonthlySales
);

public class ReportService(AppDbContext db) : IReportService
{
    public async Task<List<DebtRow>> GetDebtReportAsync(DateTime? from, DateTime? to)
    {
        var q = db.Orders
            .Include(o => o.Customer)
            .Where(o => o.Status != OrderStatus.Cancelled && o.DebtAmount > 0);

        if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value);
        if (to.HasValue)   q = q.Where(o => o.OrderDate <= to.Value);

        var orders = await q.ToListAsync();

        return orders
            .GroupBy(o => o.Customer)
            .Select(g => new DebtRow(
                g.Key.Code,
                g.Key.Name,
                g.Sum(o => o.TotalAmount),
                g.Sum(o => o.PaidAmount),
                g.Sum(o => o.DebtAmount)
            ))
            .OrderByDescending(r => r.Debt)
            .ToList();
    }

    public async Task<List<SalesReportRow>> GetSalesReportAsync(DateTime from, DateTime to)
    {
        // SQLite không SUM(decimal) server-side → lấy cột rồi group/sum ở client.
        var rows = await db.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.OrderDate.Year, o.OrderDate.Month, o.TotalAmount })
            .ToListAsync();
        var grouped = rows
            .GroupBy(o => new { o.Year, o.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .ToList();
        return grouped
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new SalesReportRow { Period = $"{x.Month:D2}/{x.Year}", OrderCount = x.Count, Revenue = x.Revenue })
            .ToList();
    }

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalProducts = await db.Products.CountAsync(p => p.IsActive);
        var pendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed);
        var todayRevenue = (await db.Orders
            .Where(o => o.OrderDate.Date == today && o.Status != OrderStatus.Cancelled)
            .Select(o => o.TotalAmount)
            .ToListAsync()).Sum();

        // Low stock: compute via grouped transactions
        var allBalances = await db.StockTransactions
            .GroupBy(t => t.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                Balance = g.Sum(x => x.Type == TransactionType.In ? x.Quantity : -x.Quantity)
            }).ToListAsync();

        var thresholds = await db.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.LowStockThreshold })
            .ToListAsync();

        var lowStockCount = thresholds
            .Count(t => (allBalances.FirstOrDefault(b => b.ProductId == t.Id)?.Balance ?? 0) <= t.LowStockThreshold);

        // Last 6 months sales
        var monthlySales = new List<(string, decimal)>();
        for (int m = 5; m >= 0; m--)
        {
            var d = today.AddMonths(-m);
            var ms = new DateTime(d.Year, d.Month, 1);
            var me = ms.AddMonths(1);
            var total = (await db.Orders
                .Where(o => o.OrderDate >= ms && o.OrderDate < me && o.Status != OrderStatus.Cancelled)
                .Select(o => o.TotalAmount)
                .ToListAsync()).Sum();
            monthlySales.Add(($"{d:MM/yyyy}", total));
        }

        return new DashboardSummary(totalProducts, lowStockCount, todayRevenue, pendingOrders, monthlySales);
    }
}
