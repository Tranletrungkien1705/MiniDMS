using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

/// <summary>
/// Màn Thanh toán — port 1:1 <c>PmtPaymentController</c> (bảng <c>Pmt_Payment</c> + <c>Pmt_PaymentDetail</c>)
/// của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/PmtPaymentController.cs
/// Mọi hành động ghi (duyệt/từ chối/hủy/xác nhận) đều POST — đúng quy ước API hệ nguồn.
/// </summary>
[Authorize]
public class PaymentController(IPaymentService payments) : Controller
{
    // GET /Payment  — port 1:1 Search (PmtPaymentController.cs:7483)
    public async Task<IActionResult> Index([FromQuery] PaymentSearchFilter f)
    {
        if (f.PageSize <= 0) f.PageSize = 20;
        var (rows, itemCount, pageCount) = await payments.SearchAsync(f);
        ViewBag.Filter = f;
        ViewBag.ItemCount = itemCount;
        ViewBag.PageCount = pageCount;
        return View(rows);
    }

    // GET /Payment/Details/5  — port 1:1 GetByPaymentNo (PmtPaymentController.cs:7894)
    public async Task<IActionResult> Details(int id)
    {
        var p = await payments.GetByIdAsync(id);
        return p == null ? NotFound() : View(p);
    }

    // POST /Payment/Approve  — port 1:1 ApproveAHQ (PmtPaymentController.cs:1985)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Accounting")]
    public Task<IActionResult> Approve(int id)
        => Run(id, () => payments.ApproveAsync(id, User.Identity?.Name ?? ""), "Đã duyệt phiếu thanh toán.");

    // POST /Payment/Reject  — port 1:1 RejectHQ (PmtPaymentController.cs:2734)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Accounting")]
    public Task<IActionResult> Reject(int id, string reason)
        => Run(id, () => payments.RejectAsync(id, reason, User.Identity?.Name ?? ""), "Đã từ chối phiếu thanh toán.");

    // POST /Payment/Cancel  — port 1:1 CancelHQ (PmtPaymentController.cs:3071)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Accounting")]
    public Task<IActionResult> Cancel(int id, string reason)
        => Run(id, () => payments.CancelAsync(id, reason, User.Identity?.Name ?? ""), "Đã hủy phiếu thanh toán.");

    // POST /Payment/ConfirmAccountingRecordNo  — port 1:1 ConfirmAccountingRecordNoHQ (PmtPaymentController.cs:2901)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Accounting")]
    public Task<IActionResult> ConfirmAccountingRecordNo(int id, string accountingRecordNo)
        => Run(id,
               () => payments.ConfirmAccountingRecordNoAsync(id, accountingRecordNo, User.Identity?.Name ?? ""),
               $"Đã xác nhận số chứng từ kế toán {accountingRecordNo}.");

    // POST /Payment/UndoConfirm  — port 1:1 UndoConfirmHQ (PmtPaymentController.cs:3238)
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Accounting")]
    public async Task<IActionResult> UndoConfirm(int id)
    {
        try
        {
            var (hasDlvr, carId) = await payments.UndoConfirmAsync(id, User.Identity?.Name ?? "");
            TempData["Success"] = hasDlvr
                // Cảnh báo theo cờ FlagHasDeliveryOrder — nguồn Pmt_Payment.cs:78
                ? $"Đã hủy xác nhận. CẢNH BÁO: xe {carId} đã có Lệnh xuất xe."
                : "Đã hủy xác nhận phiếu thanh toán.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Payment/ApiGetSeqForPaymentNo  — port 1:1 GetSeqForPaymentNo (PmtPaymentController.cs:55)
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiGetSeqForPaymentNo([FromForm] string dealerCode)
        => Json(new { paymentNo = await payments.GetSeqForPaymentNoAsync(dealerCode) });

    // POST /Payment/ApiCheckAccountingRecordNoExist  — port 1:1 CheckAccountingRecordNoExistHQ (PmtPaymentController.cs:375)
    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> ApiCheckAccountingRecordNoExist([FromForm] string accountingRecordNo)
        => Json(new { exists = await payments.AccountingRecordNoExistsAsync(accountingRecordNo) });

    private async Task<IActionResult> Run(int id, Func<Task> act, string okMessage)
    {
        try { await act(); TempData["Success"] = okMessage; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
