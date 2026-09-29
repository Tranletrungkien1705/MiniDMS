using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Models.Entities;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

/// <summary>
/// Màn Phiên bản xe (spec) — port 1:1 <c>MstCarSpecController</c> (bảng <c>Mst_CarSpec</c>) của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstCarSpecController.cs
/// </summary>
[Authorize]
public class CarSpecController(ICarSpecService specs) : Controller
{
    // GET /CarSpec  — port 1:1 Search (MstCarSpecController.cs:44)
    public async Task<IActionResult> Index(
        string? specCode, string? specDescription, string? assemblyStatus, string? flagActive,
        int pageIndex = 0, int pageSize = 20)
    {
        var (rows, itemCount, pageCount) = await specs.SearchAsync(
            specCode, specDescription, assemblyStatus, flagActive, pageIndex, pageSize);

        ViewBag.SpecCode = specCode;
        ViewBag.SpecDescription = specDescription;
        ViewBag.AssemblyStatus = assemblyStatus;
        ViewBag.FlagActive = flagActive;
        ViewBag.ItemCount = itemCount;
        ViewBag.PageCount = pageCount;
        ViewBag.PageIndex = pageIndex;
        ViewBag.PageSize = pageSize;
        return View(rows);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new CarSpec());

    // POST /CarSpec/Create  — port 1:1 Create (MstCarSpecController.cs:1577)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CarSpec model)
    {
        if (await specs.SpecCodeExistsAsync(model.SpecCode))
            ModelState.AddModelError(nameof(model.SpecCode), "Mã Spec đã tồn tại.");

        if (ModelState.IsValid)
        {
            try
            {
                await specs.CreateAsync(model, User.Identity?.Name ?? "");
                TempData["Success"] = $"Đã thêm spec {model.SpecCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await specs.GetByIdAsync(id);
        return s == null ? NotFound() : View(s);
    }

    // POST /CarSpec/Edit  — port 1:1 Update (MstCarSpecController.cs:1969)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(CarSpec model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await specs.UpdateAsync(model, User.Identity?.Name ?? "");
                TempData["Success"] = $"Đã cập nhật spec {model.SpecCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }

    // POST /CarSpec/Delete  — port 1:1 Delete (MstCarSpecController.cs:2334), theo SpecCode
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string specCode)
    {
        await specs.DeleteAsync(specCode);
        TempData["Success"] = $"Đã xoá spec {specCode}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /CarSpec/ApiGetAllActive  — port 1:1 GetAllActive (MstCarSpecController.cs:256)
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiGetAllActive()
    {
        var rows = await specs.GetAllActiveAsync();
        return Json(rows.Select(x => new { x.SpecCode, x.SpecDescription, x.ModelCode, x.AssemblyStatus }));
    }

    // POST /CarSpec/ApiGetByModelCode  — port 1:1 GetByModelCode (MstCarSpecController.cs:792)
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiGetByModelCode([FromForm] string modelCode, [FromForm] string flagActive = "")
    {
        var rows = await specs.GetByModelCodeAsync(modelCode, flagActive);
        return Json(rows.Select(x => new { x.SpecCode, x.SpecDescription, x.GradeCode, x.OCNCode, x.FlagActive }));
    }
}
