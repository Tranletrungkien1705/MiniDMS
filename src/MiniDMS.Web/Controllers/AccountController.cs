using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MiniDMS.Controllers;

public class AccountController(SignInManager<IdentityUser> signIn) : Controller
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
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Login");
    }
}
