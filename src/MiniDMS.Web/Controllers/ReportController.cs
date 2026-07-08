using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize(Roles = "Admin,Accounting")]
public class ReportController(IReportService report, IExcelService excel) : Controller
{
    // GET /Report/Debt
    public async Task<IActionResult> Debt(DateTime? from, DateTime? to)
    {
        var rows = await report.GetDebtReportAsync(from, to);
        ViewBag.From = from; ViewBag.To = to;
        return View(rows);
    }

    // GET /Report/ExportDebt
    public async Task<IActionResult> ExportDebt(DateTime? from, DateTime? to)
    {
        var rows = await report.GetDebtReportAsync(from, to);
        var bytes = excel.ExportDebtReport(rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CongNo_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // GET /Report/Sales
    public async Task<IActionResult> Sales(DateTime? from, DateTime? to)
    {
        from ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        to   ??= DateTime.Today;
        var rows = await report.GetSalesReportAsync(from.Value, to.Value);
        ViewBag.From = from; ViewBag.To = to;
        return View(rows);
    }
}
