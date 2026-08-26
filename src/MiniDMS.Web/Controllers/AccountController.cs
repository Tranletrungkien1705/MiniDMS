using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> userMgr,
    AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var result = await signIn.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
            // returnUrl có thể là chuỗi rỗng khi đăng nhập trực tiếp (form gửi "") →
            // LocalRedirect("") ném "URL is not local". Chỉ dùng khi thực sự có + là local.
            return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");

        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ── Đăng ký doanh nghiệp mới (nhận khách): tạo tổ chức + tài khoản Admin, dữ liệu DMS cô lập ──
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string companyName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Cần tên doanh nghiệp, email và mật khẩu.");
            return View();
        }
        if (await userMgr.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError("", "Email đã được đăng ký.");
            return View();
        }

        // 1) Tạo tổ chức mới
        var org = new Org { Name = companyName.Trim(), ApiKey = "dms_" + Guid.NewGuid().ToString("N") };
        db.Orgs.Add(org);
        await db.SaveChangesAsync();

        // 2) Tạo user Admin thuộc tổ chức đó
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, OrgId = org.Id };
        var created = await userMgr.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors) ModelState.AddModelError("", e.Description);
            db.Orgs.Remove(org);
            await db.SaveChangesAsync();
            return View();
        }
        await userMgr.AddToRoleAsync(user, "Admin");

        // 3) Đăng nhập luôn (cookie mang claim OrgId → dữ liệu cô lập)
        await signIn.SignInAsync(user, isPersistent: false);
        TempData["Success"] = $"Đã tạo doanh nghiệp \"{org.Name}\". Bắt đầu với dữ liệu trống của riêng bạn.";
        return LocalRedirect("/");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Login");
    }
}
