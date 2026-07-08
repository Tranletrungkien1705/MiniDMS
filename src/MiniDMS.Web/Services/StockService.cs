using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public interface IStockService
{
    Task<List<StockBalance>> GetBalancesAsync(string? skuFilter = null, int? categoryId = null);
    Task<StockBalance?> GetBalanceAsync(int productId);
    Task<List<StockTransaction>> GetHistoryAsync(int productId, int take = 50);
    Task<int> StockInAsync(int productId, int qty, string? note, string? refNo, string user);
    Task<int> StockOutAsync(int productId, int qty, string? note, string? refNo, string user);
    Task<(int ok, int fail, List<string> errors)> BulkImportAsync(Stream excelStream, string user);
}

public class StockService(AppDbContext db, IExcelService excel) : IStockService
{
    public async Task<List<StockBalance>> GetBalancesAsync(string? skuFilter = null, int? categoryId = null)
    {
        var q = db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(skuFilter))
            q = q.Where(p => p.SKU.Contains(skuFilter) || p.Name.Contains(skuFilter));
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);

        var products = await q.ToListAsync();
        var productIds = products.Select(p => p.Id).ToList();

        var txGroups = await db.StockTransactions
            .Where(t => productIds.Contains(t.ProductId))
            .GroupBy(t => new { t.ProductId, t.Type })
            .Select(g => new { g.Key.ProductId, g.Key.Type, Total = g.Sum(x => x.Quantity) })
            .ToListAsync();

        return products.Select(p => new StockBalance
        {
            ProductId       = p.Id,
            SKU             = p.SKU,
            ProductName     = p.Name,
            Category        = p.Category.Name,
            Unit            = p.Unit,
            LowStockThreshold = p.LowStockThreshold,
            TotalIn         = txGroups.FirstOrDefault(x => x.ProductId == p.Id && x.Type == TransactionType.In)?.Total ?? 0,
            TotalOut        = txGroups.FirstOrDefault(x => x.ProductId == p.Id && x.Type == TransactionType.Out)?.Total ?? 0,
        }).ToList();
    }

    public async Task<StockBalance?> GetBalanceAsync(int productId)
    {
        var balances = await GetBalancesAsync();
        return balances.FirstOrDefault(b => b.ProductId == productId);
    }

    public async Task<List<StockTransaction>> GetHistoryAsync(int productId, int take = 50) =>
        await db.StockTransactions
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<int> StockInAsync(int productId, int qty, string? note, string? refNo, string user)
    {
        if (qty <= 0) throw new ArgumentException("Số lượng nhập phải > 0");
        var tx = new StockTransaction
        {
            ProductId = productId, Type = TransactionType.In,
            Quantity = qty, Note = note, RefNo = refNo, CreatedBy = user
        };
        db.StockTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx.Id;
    }

    public async Task<int> StockOutAsync(int productId, int qty, string? note, string? refNo, string user)
    {
        if (qty <= 0) throw new ArgumentException("Số lượng xuất phải > 0");
        var balance = await GetBalanceAsync(productId);
        if (balance == null || balance.Balance < qty)
            throw new InvalidOperationException($"Tồn kho không đủ (hiện có: {balance?.Balance ?? 0})");

        var tx = new StockTransaction
        {
            ProductId = productId, Type = TransactionType.Out,
            Quantity = qty, Note = note, RefNo = refNo, CreatedBy = user
        };
        db.StockTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx.Id;
    }

    public async Task<(int ok, int fail, List<string> errors)> BulkImportAsync(Stream excelStream, string user)
    {
        var rows = excel.ReadStockImport(excelStream);
        int ok = 0, fail = 0;
        var errors = new List<string>();

        foreach (var (row, i) in rows.Select((r, i) => (r, i + 2)))
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.SKU == row.SKU && p.IsActive);
            if (product == null) { errors.Add($"Dòng {i}: SKU '{row.SKU}' không tồn tại"); fail++; continue; }
            if (row.Quantity <= 0)  { errors.Add($"Dòng {i}: Số lượng phải > 0"); fail++; continue; }

            db.StockTransactions.Add(new StockTransaction
            {
                ProductId = product.Id, Type = TransactionType.In,
                Quantity = row.Quantity, Note = row.Note, RefNo = $"IMPORT-{DateTime.Now:yyyyMMdd}",
                CreatedBy = user
            });
            ok++;
        }

        if (ok > 0) await db.SaveChangesAsync();
        return (ok, fail, errors);
    }
}
