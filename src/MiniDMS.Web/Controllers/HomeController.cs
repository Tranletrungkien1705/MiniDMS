using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize]
public class HomeController(IReportService report) : Controller
{
    public async Task<IActionResult> Index()
    {
        var summary = await report.GetDashboardAsync();
        return View(summary);
    }

    public IActionResult AccessDenied() => View();
}
