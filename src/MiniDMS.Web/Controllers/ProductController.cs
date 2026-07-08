using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Models.Entities;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize]
public class ProductController(IProductService products) : Controller
{
    // GET /Product
    public async Task<IActionResult> Index()
        => View(await products.GetAllAsync());

    // GET /Product/Create  — Admin only
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await products.GetCategoriesAsync();
        return View(new Product());
    }

    // POST /Product/Create
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Product model)
    {
        if (await products.SkuExistsAsync(model.SKU))
            ModelState.AddModelError("SKU", "SKU đã tồn tại");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await products.GetCategoriesAsync();
            return View(model);
        }

        await products.CreateAsync(model);
        TempData["Success"] = $"Đã thêm sản phẩm {model.SKU}.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Product/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await products.GetByIdAsync(id);
        if (p == null) return NotFound();
        ViewBag.Categories = await products.GetCategoriesAsync();
        return View(p);
    }

    // POST /Product/Edit
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Product model)
    {
        if (await products.SkuExistsAsync(model.SKU, model.Id))
            ModelState.AddModelError("SKU", "SKU đã tồn tại");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await products.GetCategoriesAsync();
            return View(model);
        }

        await products.UpdateAsync(model);
        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Product/Deactivate/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await products.DeactivateAsync(id);
        TempData["Success"] = "Đã vô hiệu sản phẩm.";
        return RedirectToAction(nameof(Index));
    }
}
