using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class StockController(IStockService stock, IProductService products, IExcelService excel) : Controller
{
    // GET /Stock
    public async Task<IActionResult> Index(string? sku, int? categoryId)
    {
        var balances = await stock.GetBalancesAsync(sku, categoryId);
        ViewBag.Categories = await products.GetCategoriesAsync();
        ViewBag.Filter = new { sku, categoryId };
        return View(balances);
    }

    // GET /Stock/History/5
    public async Task<IActionResult> History(int id)
    {
        var product = await products.GetByIdAsync(id);
        if (product == null) return NotFound();
        var history = await stock.GetHistoryAsync(id, 100);
        ViewBag.Product = product;
        return View(history);
    }

    // GET /Stock/In
    public async Task<IActionResult> In()
    {
        ViewBag.Products = await products.GetAllAsync();
        return View();
    }

    // POST /Stock/In
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> In(int productId, int quantity, string? note, string? refNo)
    {
        try
        {
            await stock.StockInAsync(productId, quantity, note, refNo, User.Identity!.Name!);
            TempData["Success"] = $"Nhập kho thành công {quantity} sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Products = await products.GetAllAsync();
            return View();
        }
    }

    // GET /Stock/Out
    public async Task<IActionResult> Out()
    {
        ViewBag.Products = await products.GetAllAsync();
        return View();
    }

    // POST /Stock/Out
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Out(int productId, int quantity, string? note, string? refNo)
    {
        try
        {
            await stock.StockOutAsync(productId, quantity, note, refNo, User.Identity!.Name!);
            TempData["Success"] = $"Xuất kho thành công {quantity} sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Products = await products.GetAllAsync();
            return View();
        }
    }

    // GET /Stock/Import
    public IActionResult Import() => View();

    // POST /Stock/Import
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file Excel.";
            return View();
        }

        using var stream = file.OpenReadStream();
        var (ok, fail, errors) = await stock.BulkImportAsync(stream, User.Identity!.Name!);

        TempData["ImportResult"] = $"Import thành công: {ok} dòng. Lỗi: {fail} dòng.";
        if (errors.Any())
            TempData["ImportErrors"] = string.Join("\n", errors.Take(10));

        return RedirectToAction(nameof(Index));
    }

    // GET /Stock/ExportBalance
    [Authorize(Roles = "Admin,Warehouse,Accounting")]
    public async Task<IActionResult> ExportBalance()
    {
        var balances = await stock.GetBalancesAsync();
        var bytes = excel.ExportStockBalance(balances);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"TonKho_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
