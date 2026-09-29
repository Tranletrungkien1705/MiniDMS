namespace MiniDMS.Models.Entities;

/// <summary>
/// Trạng thái phiếu thanh toán — port 1:1 class <c>PmtStatus</c>.
/// Nguồn: 12.Dev.Common/idn.DMSSales.Constants/Const.Main.BE.cs:794-804.
/// Giữ nguyên giá trị chuỗi của nguồn (KHÔNG đổi thành enum số) để so khớp dữ liệu thật.
/// </summary>
public static class PmtStatus
{
    public const string Pending = "PENDING";
    public const string Process = "PROCESS";
    public const string Approve = "APPROVE";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";
    public const string Cancel = "CANCEL";
    public const string Finish = "FINISH";
    public const string Error = "ERROR";

    public static readonly string[] All =
        [Pending, Process, Approve, Paid, Failed, Cancel, Finish, Error];
}

/// <summary>
/// Phiếu thanh toán (master) — port 1:1 bảng <c>Pmt_Payment</c> của hệ nguồn 2023.3.DMS.Sales.
/// Nguồn cột: 12.Dev.Common/idn.DMSSales.Common/Models/DMSSales/Pmt_Payment.cs:10-47 — đếm bằng máy: 36 cột (7 cột join không lưu ở đích).
/// Nguồn nghiệp vụ: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/PmtPaymentController.cs
///   - CreateDL                    : PmtPaymentController.cs:815   (đại lý tạo phiếu)
///   - UpdateDL                    : PmtPaymentController.cs:1394
///   - ApproveAHQ                  : PmtPaymentController.cs:1985  (HQ duyệt)
///   - RejectDL / RejectHQ         : PmtPaymentController.cs:2569 / :2734
///   - ConfirmAccountingRecordNoHQ : PmtPaymentController.cs:2901  (xác nhận số chứng từ kế toán)
///   - CancelHQ                    : PmtPaymentController.cs:3071
///   - UndoConfirmHQ               : PmtPaymentController.cs:3238
///   - Search                      : PmtPaymentController.cs:7483  (24 tham số lọc)
///   - GetByPaymentNo              : PmtPaymentController.cs:7894
/// GHI CHÚ HAI DB: Search/GetByPaymentNo nhận tham số <c>FlagDataWH</c>; khi bật thì nguồn gọi
/// <c>_biz.Pmt_Payment_GetWH</c> (DB Warehouse) thay vì <c>_biz.Pmt_Payment_Get</c> (DB Main)
/// — PmtPaymentController.cs:7709-7760. MiniDMS chỉ có MỘT DB nên giữ cờ để bảo toàn hợp đồng API,
/// KHÔNG gộp dữ liệu hai DB (xem mục 5 báo cáo AGENT_MiniDMS.md).
/// </summary>
public class Payment : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }

    /// <summary>Mã đại lý — Pmt_Payment.cs:12. Lọc toán tử "in": PmtPaymentController.cs:12216.</summary>
    public string DealerCode { get; set; } = "";
    /// <summary>Loại thanh toán — Pmt_Payment.cs:15. Lọc toán tử "in": PmtPaymentController.cs:12077.</summary>
    public string PaymentType { get; set; } = "";
    /// <summary>Số thanh toán (khoá nghiệp vụ) — Pmt_Payment.cs:17. Lọc "like": PmtPaymentController.cs:12083.</summary>
    public string PaymentNo { get; set; } = "";
    /// <summary>Loại bảo lãnh — Pmt_Payment.cs:18.</summary>
    public string? GuaranteeType { get; set; }
    /// <summary>Số ủy nhiệm chi (UNC) — Pmt_Payment.cs:19. Lọc "like": PmtPaymentController.cs:12119.</summary>
    public string? BankPaymentNo { get; set; }
    /// <summary>Số tiền thanh toán — Pmt_Payment.cs:20.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Mã ngân hàng tài khoản gửi — Pmt_Payment.cs:21.</summary>
    public string? BankCodeSend { get; set; }
    /// <summary>Số tài khoản gửi — Pmt_Payment.cs:23.</summary>
    public string? BankAccountSend { get; set; }
    /// <summary>Mã ngân hàng nhận — Pmt_Payment.cs:30.</summary>
    public string? BankCodeReceive { get; set; }
    /// <summary>Số tài khoản nhận — Pmt_Payment.cs:25.</summary>
    public string? BankAccountReceive { get; set; }

    /// <summary>Nguồn tiền — Pmt_Payment.cs:26. Lọc "in": PmtPaymentController.cs:12192.</summary>
    public string? Funds { get; set; }
    /// <summary>Ngân hàng cho vay — Pmt_Payment.cs:27. Lọc "=" (KHÔNG phải "in"): PmtPaymentController.cs:12198.</summary>
    public string? BankLending { get; set; }
    /// <summary>Nội dung thanh toán gợi ý — Pmt_Payment.cs:28.</summary>
    public string? Remark { get; set; }
    /// <summary>Lãi suất vay/trả chậm (%/năm) — Pmt_Payment.cs:29.</summary>
    public decimal? InterestRate { get; set; }
    /// <summary>Kỳ hạn vay/trả chậm (tháng) — Pmt_Payment.cs:30.</summary>
    public int? LoanPeriod { get; set; }

    /// <summary>Ngày tạo phiếu thanh toán — Pmt_Payment.cs:34. Lọc khoảng ">=" / "&lt;=": PmtPaymentController.cs:12095 và :12101.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    /// <summary>Ngày tiền về — Pmt_Payment.cs:35. Lọc khoảng: PmtPaymentController.cs:12107 và :12113.</summary>
    public DateTime? PaymentEndDate { get; set; }
    /// <summary>Trạng thái thanh toán — Pmt_Payment.cs:36. Giá trị theo <see cref="PmtStatus"/>. Lọc "in": PmtPaymentController.cs:12089.</summary>
    public string PaymentStatus { get; set; } = PmtStatus.Pending;

    /// <summary>Cờ thanh toán Ebank — Pmt_Payment.cs:37 (tên nguồn viết thiếu chữ n: PmtBakingStatus — giữ nguyên để khớp 1:1).</summary>
    public string? PmtBakingStatus { get; set; }
    /// <summary>Loại chuyển tiền — Pmt_Payment.cs:38.</summary>
    public string? TransferType { get; set; }
    /// <summary>Nội dung chuyển tiền (TCF) — Pmt_Payment.cs:39.</summary>
    public string? TCF_RemarkTranfer { get; set; }
    /// <summary>Trạng thái map TCF — Pmt_Payment.cs:40.</summary>
    public string? StatusMapTCF { get; set; }
    /// <summary>Mã giao dịch từ TCF — Pmt_Payment.cs:41.</summary>
    public string? TCF_MaGiaoDich { get; set; }

    /// <summary>Lý do hủy/từ chối — Pmt_Payment.cs:42. Ghi bởi RejectDL/RejectHQ/CancelHQ.</summary>
    public string? RemarkReason { get; set; }
    /// <summary>Số chứng từ kế toán — Pmt_Payment.cs:43. Lọc "like": PmtPaymentController.cs:12210.</summary>
    public string? AccountingRecordNo { get; set; }
    /// <summary>Số chứng từ kế toán mới — Pmt_Payment.cs:44 (dùng khi UpdateAccountingRecordNoHQ, PmtPaymentController.cs:5523).</summary>
    public string? NewAccountingRecordNo { get; set; }
    /// <summary>Ngày duyệt — Pmt_Payment.cs:45.</summary>
    public DateTime? ApprovedDate { get; set; }
    /// <summary>Người duyệt — Pmt_Payment.cs:46.</summary>
    public string? ApprovedBy { get; set; }
    /// <summary>Thanh toán TTC/TTBL — Pmt_Payment.cs:47.</summary>
    public string? PaymentType_TTCORTTBL { get; set; }

    public ICollection<PaymentDetail> Details { get; set; } = [];
}

/// <summary>
/// Dòng chi tiết phiếu thanh toán (theo xe) — port 1:1 bảng <c>Pmt_PaymentDetail</c>.
/// Nguồn cột: 12.Dev.Common/idn.DMSSales.Common/Models/DMSSales/Pmt_Payment.cs:49-79 — đếm bằng máy: 26 cột (6 cột join/thay bằng FK không lưu ở đích).
/// </summary>
public class PaymentDetail : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }

    /// <summary>Số thanh toán (khoá ngoại về phiếu) — Pmt_Payment.cs:56.</summary>
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    /// <summary>Mã xe — Pmt_Payment.cs:51. Lọc "like": PmtPaymentController.cs:12144.</summary>
    public string CarId { get; set; } = "";
    /// <summary>Loại bảo lãnh — Pmt_Payment.cs:53.</summary>
    public string? GuaranteeType { get; set; }
    /// <summary>
    /// Số bảo lãnh — Pmt_Payment.cs:54. Cột này PHÂN LOẠI phiếu: nguồn lọc
    /// "TTC" = GuaranteeNo IS NULL, "TTBL/LC" = IS NOT NULL — PmtPaymentController.cs:12155-12163.
    /// </summary>
    public string? GuaranteeNo { get; set; }
    /// <summary>Số phụ lục hợp đồng (PLHĐ) — Pmt_Payment.cs:55.</summary>
    public string? DlrCtrNo { get; set; }
    /// <summary>Số tiền thanh toán của dòng — Pmt_Payment.cs:56.</summary>
    public decimal Amount { get; set; }

    /// <summary>Mã model — Pmt_Payment.cs:58.</summary>
    public string? ModelCode { get; set; }
    /// <summary>Số VIN — Pmt_Payment.cs:59. Nguồn lọc VIN trên bảng Car_Car, KHÔNG trên detail — PmtPaymentController.cs:12126.</summary>
    public string? VIN { get; set; }
    /// <summary>Mã spec — Pmt_Payment.cs:60.</summary>
    public string? SpecCode { get; set; }
    /// <summary>Mã màu đặt hàng — Pmt_Payment.cs:62.</summary>
    public string? ColorCode { get; set; }
    /// <summary>Mã màu thực tế — Pmt_Payment.cs:63.</summary>
    public string? VINColorCode { get; set; }
    /// <summary>Số đơn hàng — Pmt_Payment.cs:65.</summary>
    public string? SOCode { get; set; }
    /// <summary>Số bảo lãnh ngân hàng — Pmt_Payment.cs:66. Nguồn lọc trên Pmt_Guarantee — PmtPaymentController.cs:12170.</summary>
    public string? BankGuaranteeNo { get; set; }

    /// <summary>Tổng % đã thanh toán (tích lũy) — Pmt_Payment.cs:71.</summary>
    public decimal? PMT_Percent_Accum { get; set; }
    /// <summary>Tổng số tiền đã thanh toán — Pmt_Payment.cs:72.</summary>
    public decimal? PMPDAmountTotal { get; set; }
    /// <summary>% thanh toán của dòng này — Pmt_Payment.cs:73.</summary>
    public decimal? PMT_Percent_Current { get; set; }
    /// <summary>Giá xe thực tế — Pmt_Payment.cs:74.</summary>
    public decimal? UnitPriceActual { get; set; }
    /// <summary>Lãi suất vay/trả chậm (%/năm) — Pmt_Payment.cs:75.</summary>
    public decimal? InterestRate { get; set; }
    /// <summary>Kỳ hạn vay/trả chậm (tháng) — Pmt_Payment.cs:76.</summary>
    public int? LoanPeriod { get; set; }
    /// <summary>Pháp nhân — Pmt_Payment.cs:77. Nguồn lọc trên Car_Car.FlagisHTC — PmtPaymentController.cs:12204.</summary>
    public string? FlagisHTC { get; set; }
    /// <summary>
    /// '1' = xe đã có Lệnh xuất xe (trạng thái ≠ R) — Pmt_Payment.cs:78.
    /// Nguồn dùng để cảnh báo khi Hủy xác nhận (UndoConfirmHQ, PmtPaymentController.cs:3238).
    /// </summary>
    public string? FlagHasDeliveryOrder { get; set; }
}
