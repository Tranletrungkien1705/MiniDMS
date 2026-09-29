using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

/// <summary>Bộ điều kiện tìm phiếu thanh toán — port 1:1 24 tham số của <c>Search</c> (PmtPaymentController.cs:7483-7510).</summary>
public class PaymentSearchFilter
{
    public string? PaymentType { get; set; }          // PmtPaymentController.cs:7484 — lọc "in"   (:12077)
    public string? PaymentNo { get; set; }            // :7485 — lọc "like" (:12083)
    public string? PaymentStatus { get; set; }        // :7486 — lọc "in"   (:12089)
    public DateTime? CreatedDateFrom { get; set; }    // :7487 — ">="       (:12095)
    public DateTime? CreatedDateTo { get; set; }      // :7488 — "<="       (:12101)
    public DateTime? PaymentEndDateFrom { get; set; } // :7489 — ">="       (:12107)
    public DateTime? PaymentEndDateTo { get; set; }   // :7490 — "<="       (:12113)
    public string? BankPaymentNo { get; set; }        // :7491 — "like"     (:12119)
    public string? VIN { get; set; }                  // :7492 — "like" trên Car_Car.VIN (:12126)
    public string? GuaranteeNo { get; set; }          // :7493 — "like" trên detail     (:12132)
    public decimal? TotalAmount { get; set; }         // :7494 — "in"                   (:12138)
    public string? AccountingRecordNo { get; set; }   // :7495 — "like"                 (:12210)
    public string? DealerCode { get; set; }           // :7496 — "in"                   (:12216)
    public string? CarId { get; set; }                // :7497 — "like" trên detail     (:12144)
    public string? SOCode { get; set; }               // :7498 — "like" trên Ord_SalesOrder (:12150)
    public string? ThanhToan { get; set; }            // :7499 — "TTC" | "TTBL/LC"      (:12155)
    public string? BankGuaranteeNo { get; set; }      // :7500 — "like" trên Pmt_Guarantee (:12170)
    public string? DlrCtrNo { get; set; }             // :7501 — "like" trên Car_Car    (:12176)
    public string? BankCodeSend { get; set; }         // :7502 — "like"                 (:12182)
    public string? BankCodeReceive { get; set; }      // :7503 — "like"                 (:12188)
    public string? Funds { get; set; }                // :7504 — "in"                   (:12192)
    public string? BankLending { get; set; }          // :7505 — "=" (KHÔNG phải "in")  (:12198)
    public string? FlagisHTC { get; set; }            // :7506 — "=" trên Car_Car       (:12204)
    /// <summary>
    /// Cờ dữ liệu Warehouse — PmtPaymentController.cs:7507. Ở nguồn, bật cờ này thì gọi
    /// <c>_biz.Pmt_Payment_GetWH</c> (DB Warehouse) thay cho <c>_biz.Pmt_Payment_Get</c> (DB Main)
    /// — PmtPaymentController.cs:7709 vs :7736. MiniDMS chỉ có 1 DB nên cờ được giữ nhưng KHÔNG đổi nguồn dữ liệu.
    /// </summary>
    public string? FlagDataWH { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Nghiệp vụ Thanh toán — port 1:1 <c>PmtPaymentController</c> của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/PmtPaymentController.cs
/// </summary>
public interface IPaymentService
{
    Task<(List<Payment> Rows, int ItemCount, int PageCount)> SearchAsync(PaymentSearchFilter f);
    Task<Payment?> GetByPaymentNoAsync(string paymentNo);
    Task<Payment?> GetByIdAsync(int id);
    Task<bool> AccountingRecordNoExistsAsync(string accountingRecordNo);
    Task<string> GetSeqForPaymentNoAsync(string dealerCode);
    Task ApproveAsync(int id, string userCode);
    Task RejectAsync(int id, string reason, string userCode);
    Task CancelAsync(int id, string reason, string userCode);
    Task ConfirmAccountingRecordNoAsync(int id, string accountingRecordNo, string userCode);
    Task<(bool HasDeliveryOrder, string? CarId)> UndoConfirmAsync(int id, string userCode);
}

public class PaymentService(AppDbContext db) : IPaymentService
{
    /// <summary>
    /// port 1:1 <c>Search</c> (PmtPaymentController.cs:7483) + bộ dựng điều kiện
    /// <c>strWhereClause_Pmt_Payment</c> (PmtPaymentController.cs:12063-12221).
    /// Toán tử giữ NGUYÊN theo nguồn (in / like / &gt;= / &lt;= / = ) — xem chú thích từng cột trong
    /// <see cref="PaymentSearchFilter"/>.
    /// </summary>
    public async Task<(List<Payment> Rows, int ItemCount, int PageCount)> SearchAsync(PaymentSearchFilter f)
    {
        var q = db.Payments.AsNoTracking().Include(x => x.Details).AsQueryable();

        // --- Điều kiện trên bảng master Pmt_Payment ---
        var lstType = SplitIn(f.PaymentType);
        if (lstType.Count > 0) q = q.Where(x => lstType.Contains(x.PaymentType));              // :12077 "in"

        if (!string.IsNullOrWhiteSpace(f.PaymentNo))
            q = q.Where(x => EF.Functions.Like(x.PaymentNo, $"%{f.PaymentNo.Trim()}%"));       // :12083 "like"

        var lstStatus = SplitIn(f.PaymentStatus);
        if (lstStatus.Count > 0) q = q.Where(x => lstStatus.Contains(x.PaymentStatus));        // :12089 "in"

        if (f.CreatedDateFrom.HasValue) q = q.Where(x => x.CreatedDate >= f.CreatedDateFrom);  // :12095
        if (f.CreatedDateTo.HasValue) q = q.Where(x => x.CreatedDate <= f.CreatedDateTo);      // :12101
        if (f.PaymentEndDateFrom.HasValue) q = q.Where(x => x.PaymentEndDate >= f.PaymentEndDateFrom); // :12107
        if (f.PaymentEndDateTo.HasValue) q = q.Where(x => x.PaymentEndDate <= f.PaymentEndDateTo);     // :12113

        if (!string.IsNullOrWhiteSpace(f.BankPaymentNo))
            q = q.Where(x => x.BankPaymentNo != null && EF.Functions.Like(x.BankPaymentNo, $"%{f.BankPaymentNo.Trim()}%")); // :12119

        if (f.TotalAmount.HasValue) q = q.Where(x => x.TotalAmount == f.TotalAmount);          // :12138 "in" (1 giá trị)

        if (!string.IsNullOrWhiteSpace(f.AccountingRecordNo))
            q = q.Where(x => x.AccountingRecordNo != null
                          && EF.Functions.Like(x.AccountingRecordNo, $"%{f.AccountingRecordNo.Trim()}%")); // :12210

        var lstDealer = SplitIn(f.DealerCode);
        if (lstDealer.Count > 0) q = q.Where(x => lstDealer.Contains(x.DealerCode));           // :12216 "in"

        if (!string.IsNullOrWhiteSpace(f.BankCodeSend))
            q = q.Where(x => x.BankCodeSend != null && EF.Functions.Like(x.BankCodeSend, $"%{f.BankCodeSend.Trim()}%"));   // :12182
        if (!string.IsNullOrWhiteSpace(f.BankCodeReceive))
            q = q.Where(x => x.BankCodeReceive != null && EF.Functions.Like(x.BankCodeReceive, $"%{f.BankCodeReceive.Trim()}%")); // :12188

        var lstFunds = SplitIn(f.Funds);
        if (lstFunds.Count > 0) q = q.Where(x => x.Funds != null && lstFunds.Contains(x.Funds)); // :12192 "in"

        // :12198 — nguồn dùng "=" cho BankLending, KHÔNG phải "in". Giữ đúng để không nới rộng kết quả.
        if (!string.IsNullOrWhiteSpace(f.BankLending))
            q = q.Where(x => x.BankLending == f.BankLending);

        // --- Điều kiện trên bảng chi tiết Pmt_PaymentDetail (và các bảng nguồn join vào nó) ---
        if (!string.IsNullOrWhiteSpace(f.CarId))
            q = q.Where(x => x.Details.Any(d => EF.Functions.Like(d.CarId, $"%{f.CarId.Trim()}%")));   // :12144
        if (!string.IsNullOrWhiteSpace(f.GuaranteeNo))
            q = q.Where(x => x.Details.Any(d => d.GuaranteeNo != null
                          && EF.Functions.Like(d.GuaranteeNo, $"%{f.GuaranteeNo.Trim()}%")));          // :12132
        if (!string.IsNullOrWhiteSpace(f.VIN))
            q = q.Where(x => x.Details.Any(d => d.VIN != null && EF.Functions.Like(d.VIN, $"%{f.VIN.Trim()}%"))); // :12126
        if (!string.IsNullOrWhiteSpace(f.SOCode))
            q = q.Where(x => x.Details.Any(d => d.SOCode != null && EF.Functions.Like(d.SOCode, $"%{f.SOCode.Trim()}%"))); // :12150
        if (!string.IsNullOrWhiteSpace(f.BankGuaranteeNo))
            q = q.Where(x => x.Details.Any(d => d.BankGuaranteeNo != null
                          && EF.Functions.Like(d.BankGuaranteeNo, $"%{f.BankGuaranteeNo.Trim()}%")));  // :12170
        if (!string.IsNullOrWhiteSpace(f.DlrCtrNo))
            q = q.Where(x => x.Details.Any(d => d.DlrCtrNo != null
                          && EF.Functions.Like(d.DlrCtrNo, $"%{f.DlrCtrNo.Trim()}%")));                // :12176
        if (!string.IsNullOrWhiteSpace(f.FlagisHTC))
            q = q.Where(x => x.Details.Any(d => d.FlagisHTC == f.FlagisHTC));                          // :12204 "="

        // :12155-12163 — "TTC" = KHÔNG có số bảo lãnh (isnull); "TTBL/LC" = CÓ số bảo lãnh (isnotnull).
        if (string.Equals(f.ThanhToan, "TTC", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => x.Details.Any(d => d.GuaranteeNo == null));
        else if (string.Equals(f.ThanhToan, "TTBL/LC", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => x.Details.Any(d => d.GuaranteeNo != null));

        var itemCount = await q.CountAsync();
        var pageSize = f.PageSize <= 0 ? 10 : f.PageSize;
        var pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;

        var rows = await q.OrderByDescending(x => x.CreatedDate).ThenBy(x => x.PaymentNo)
                          .Skip(f.PageIndex * pageSize).Take(pageSize)
                          .ToListAsync();
        return (rows, itemCount, pageCount);
    }

    /// <summary>port 1:1 <c>GetByPaymentNo</c> (PmtPaymentController.cs:7894).</summary>
    public Task<Payment?> GetByPaymentNoAsync(string paymentNo)
        => db.Payments.AsNoTracking().Include(x => x.Details)
             .FirstOrDefaultAsync(x => x.PaymentNo == paymentNo);

    public Task<Payment?> GetByIdAsync(int id)
        => db.Payments.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>port 1:1 <c>CheckAccountingRecordNoExistHQ</c> (PmtPaymentController.cs:375).</summary>
    public Task<bool> AccountingRecordNoExistsAsync(string accountingRecordNo)
        => db.Payments.AnyAsync(x => x.AccountingRecordNo == accountingRecordNo);

    /// <summary>
    /// port 1:1 <c>GetSeqForPaymentNo</c> (PmtPaymentController.cs:55) — sinh số thanh toán kế tiếp.
    /// Nguồn lấy sequence từ DB; ở đây đếm theo tiền tố &lt;DealerCode&gt;-&lt;yyyyMM&gt; trong cùng tổ chức.
    /// </summary>
    public async Task<string> GetSeqForPaymentNoAsync(string dealerCode)
    {
        var prefix = $"{dealerCode}-{DateTime.Now:yyyyMM}-";
        var seq = await db.Payments.CountAsync(x => x.PaymentNo.StartsWith(prefix)) + 1;
        return $"{prefix}{seq:D4}";
    }

    /// <summary>
    /// port 1:1 <c>ApproveAHQ</c> (PmtPaymentController.cs:1985) — HQ duyệt phiếu.
    /// Chỉ phiếu đang PENDING mới duyệt được (PmtStatus — Const.Main.BE.cs:794-804).
    /// </summary>
    public async Task ApproveAsync(int id, string userCode)
    {
        var p = await Require(id);
        if (p.PaymentStatus != PmtStatus.Pending)
            throw new InvalidOperationException($"Chỉ duyệt được phiếu ở trạng thái {PmtStatus.Pending} (hiện tại: {p.PaymentStatus}).");
        p.PaymentStatus = PmtStatus.Approve;
        p.ApprovedBy = userCode;
        p.ApprovedDate = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// port 1:1 <c>RejectHQ</c> (PmtPaymentController.cs:2734) — từ chối, ghi lý do vào
    /// <c>RemarkReason</c> (Pmt_Payment.cs:42).
    /// </summary>
    public async Task RejectAsync(int id, string reason, string userCode)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Phải nhập lý do từ chối.");
        var p = await Require(id);
        if (p.PaymentStatus != PmtStatus.Pending)
            throw new InvalidOperationException($"Chỉ từ chối được phiếu ở trạng thái {PmtStatus.Pending} (hiện tại: {p.PaymentStatus}).");
        p.PaymentStatus = PmtStatus.Failed;
        p.RemarkReason = reason;
        p.ApprovedBy = userCode;
        p.ApprovedDate = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>port 1:1 <c>CancelHQ</c> (PmtPaymentController.cs:3071) — hủy phiếu, bắt buộc có lý do.</summary>
    public async Task CancelAsync(int id, string reason, string userCode)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Phải nhập lý do hủy.");
        var p = await Require(id);
        if (p.PaymentStatus is PmtStatus.Finish or PmtStatus.Cancel)
            throw new InvalidOperationException($"Phiếu đã {p.PaymentStatus}, không hủy được.");
        p.PaymentStatus = PmtStatus.Cancel;
        p.RemarkReason = reason;
        p.ApprovedBy = userCode;
        p.ApprovedDate = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// port 1:1 <c>ConfirmAccountingRecordNoHQ</c> (PmtPaymentController.cs:2901) — kế toán xác nhận
    /// số chứng từ; nguồn kiểm trùng bằng <c>CheckAccountingRecordNoExistHQ</c> (:375) nên guard trùng
    /// được giữ ở đây.
    /// </summary>
    public async Task ConfirmAccountingRecordNoAsync(int id, string accountingRecordNo, string userCode)
    {
        if (string.IsNullOrWhiteSpace(accountingRecordNo))
            throw new ArgumentException("Phải nhập số chứng từ kế toán.");
        var p = await Require(id);
        if (p.PaymentStatus != PmtStatus.Approve)
            throw new InvalidOperationException($"Chỉ xác nhận được phiếu đã {PmtStatus.Approve} (hiện tại: {p.PaymentStatus}).");
        if (await db.Payments.AnyAsync(x => x.AccountingRecordNo == accountingRecordNo && x.Id != id))
            throw new InvalidOperationException($"Số chứng từ kế toán {accountingRecordNo} đã tồn tại ở phiếu khác.");

        p.AccountingRecordNo = accountingRecordNo;
        p.PaymentStatus = PmtStatus.Finish;
        p.PaymentEndDate = DateTime.Now;
        p.ApprovedBy = userCode;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// port 1:1 <c>UndoConfirmHQ</c> (PmtPaymentController.cs:3238) — hủy xác nhận, quay về APPROVE.
    /// Trả kèm cảnh báo nếu có dòng chi tiết mang cờ <c>FlagHasDeliveryOrder = '1'</c>
    /// (xe đã có Lệnh xuất xe) — nguồn: Pmt_Payment.cs:78.
    /// </summary>
    public async Task<(bool HasDeliveryOrder, string? CarId)> UndoConfirmAsync(int id, string userCode)
    {
        var p = await Require(id);
        if (p.PaymentStatus != PmtStatus.Finish)
            throw new InvalidOperationException($"Chỉ hủy xác nhận được phiếu đã {PmtStatus.Finish} (hiện tại: {p.PaymentStatus}).");

        var dlvr = p.Details.FirstOrDefault(d => d.FlagHasDeliveryOrder == "1");

        p.PaymentStatus = PmtStatus.Approve;
        p.AccountingRecordNo = null;
        p.PaymentEndDate = null;
        p.ApprovedBy = userCode;
        await db.SaveChangesAsync();
        return (dlvr != null, dlvr?.CarId);
    }

    private async Task<Payment> Require(int id)
        => await db.Payments.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id)
           ?? throw new InvalidOperationException("Không tìm thấy phiếu thanh toán.");

    /// <summary>Tách chuỗi nhiều giá trị cho toán tử "in" — nguồn dùng CUtils.StrSplit rồi bọc nháy.</summary>
    private static List<string> SplitIn(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
