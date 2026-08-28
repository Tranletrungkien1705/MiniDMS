using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    // Đăng nhập xong (Identity cookie) → SPA React. Login/authorize giữ nguyên; api/v1 [Authorize] dùng cookie.
    public IActionResult Index() => Redirect("/index.html");

    public IActionResult AccessDenied() => View();
}

[Authorize]
public class LegacyController(IReportService report) : Controller
{
    public async Task<IActionResult> Index() { var summary = await report.GetDashboardAsync(); return View("~/Views/Home/Index.cshtml", summary); }
}
