using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Models.Entities;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

/// <summary>
/// Màn Đại lý — port 1:1 <c>MstDealerController</c> (bảng <c>Mst_Dealer</c>) của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstDealerController.cs
/// Hệ nguồn quy ước MỌI lời gọi ghi đều POST form-urlencoded (không GET query-string) — giữ đúng ở đây.
/// </summary>
[Authorize]
public class DealerController(IDealerService dealers) : Controller
{
    // GET /Dealer  — port 1:1 Search (MstDealerController.cs:42)
    public async Task<IActionResult> Index(
        string? dealerCode, string? dealerName,
        string? flagAutoLXX, string? flagAutoMapVIN, string? flagAutoSOAppr, string? flagActive,
        int pageIndex = 0, int pageSize = 20)
    {
        var (rows, itemCount, pageCount) = await dealers.SearchAsync(
            dealerCode, dealerName, flagAutoLXX, flagAutoMapVIN, flagAutoSOAppr, flagActive,
            pageIndex, pageSize);

        ViewBag.DealerCode = dealerCode;
        ViewBag.DealerName = dealerName;
        ViewBag.FlagActive = flagActive;
        ViewBag.FlagAutoLXX = flagAutoLXX;
        ViewBag.FlagAutoMapVIN = flagAutoMapVIN;
        ViewBag.FlagAutoSOAppr = flagAutoSOAppr;
        ViewBag.ItemCount = itemCount;
        ViewBag.PageCount = pageCount;
        ViewBag.PageIndex = pageIndex;
        ViewBag.PageSize = pageSize;
        return View(rows);
    }

    // GET /Dealer/Create  — form tạo mới (ghi qua POST)
    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Dealer());

    // POST /Dealer/Create  — port 1:1 Create (MstDealerController.cs:1695)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Dealer model)
    {
        if (await dealers.DealerCodeExistsAsync(model.DealerCode))
            ModelState.AddModelError(nameof(model.DealerCode), "Mã đại lý đã tồn tại.");

        if (ModelState.IsValid)
        {
            try
            {
                await dealers.CreateAsync(model, User.Identity?.Name ?? "");
                TempData["Success"] = $"Đã thêm đại lý {model.DealerCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }

    // GET /Dealer/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var d = await dealers.GetByIdAsync(id);
        return d == null ? NotFound() : View(d);
    }

    // POST /Dealer/Edit  — port 1:1 Update (MstDealerController.cs:2105)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Dealer model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await dealers.UpdateAsync(model, User.Identity?.Name ?? "");
                TempData["Success"] = $"Đã cập nhật đại lý {model.DealerCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { ModelState.AddModelError("", ex.Message); }
        }
        return View(model);
    }

    // POST /Dealer/Delete  — port 1:1 Delete (MstDealerController.cs:2510), định danh bằng DealerCode
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string dealerCode)
    {
        await dealers.DeleteAsync(dealerCode);
        TempData["Success"] = $"Đã xoá đại lý {dealerCode}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Dealer/ApiGetAllActive  — port 1:1 GetAllActive (MstDealerController.cs:279).
    // POST-hết theo quy ước API hệ nguồn (form-urlencoded), KHÔNG GET query-string.
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiGetAllActive([FromForm] string flagIncludeInactive = "")
    {
        var rows = await dealers.GetAllActiveAsync(flagIncludeInactive == "1");
        return Json(rows.Select(x => new { x.DealerCode, x.DealerName, x.ProvinceCode, x.FlagActive }));
    }

    // POST /Dealer/ApiGetByDealerCode  — port 1:1 GetByDealerCode (MstDealerController.cs:859)
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiGetByDealerCode([FromForm] string dealerCode)
    {
        var d = await dealers.GetByDealerCodeAsync(dealerCode);
        return d == null ? NotFound(new { error = "Không tìm thấy đại lý " + dealerCode }) : Json(d);
    }
}
