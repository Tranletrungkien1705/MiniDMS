namespace MiniDMS.Models.Entities;

/// <summary>
/// Phiên bản xe (spec) — port 1:1 bảng <c>Mst_CarSpec</c> của hệ nguồn 2023.3.DMS.Sales.
/// Nguồn cột: 12.Dev.Common/idn.DMSSales.Common/Models/DMSSales/Mst_CarSpec.cs:10-31 — đếm bằng máy: 21 cột (2 cột join OCNDescription/OriginName không lưu ở đích).
/// Nguồn nghiệp vụ: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstCarSpecController.cs
///   - Search             : MstCarSpecController.cs:44   (4 tham số: SpecCode, SpecDescription, AssemblyStatus, FlagActive)
///   - GetAllActive       : MstCarSpecController.cs:256
///   - GetByCode          : MstCarSpecController.cs:617
///   - GetByModelCode     : MstCarSpecController.cs:792
///   - Create             : MstCarSpecController.cs:1577
///   - Update             : MstCarSpecController.cs:1969
///   - Delete             : MstCarSpecController.cs:2334
/// </summary>
public class CarSpec : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }

    /// <summary>Mã Model — Mst_CarSpec.cs:12. Bắt buộc: MstCarSpecController.cs:1725 (Mst_CarSpec_Create_InvalidModelCode).</summary>
    public string ModelCode { get; set; } = "";
    /// <summary>RootSpec (spec gốc, gom nhóm các spec cùng dòng) — Mst_CarSpec.cs:13. Bắt buộc: MstCarSpecController.cs:1713.</summary>
    public string RootSpec { get; set; } = "";
    /// <summary>Mã Spec — Mst_CarSpec.cs:14. Khoá nghiệp vụ. Bắt buộc: MstCarSpecController.cs:1702.</summary>
    public string SpecCode { get; set; } = "";
    /// <summary>Mô tả Spec — Mst_CarSpec.cs:15. Bắt buộc: MstCarSpecController.cs:1785.</summary>
    public string SpecDescription { get; set; } = "";
    /// <summary>Mã StandardOption — Mst_CarSpec.cs:16. Bắt buộc: MstCarSpecController.cs:1737.</summary>
    public string StdOptCode { get; set; } = "";
    /// <summary>Mã OCN — Mst_CarSpec.cs:17. Bắt buộc: MstCarSpecController.cs:1761.</summary>
    public string OCNCode { get; set; } = "";
    /// <summary>Mã Grade — Mst_CarSpec.cs:19. Bắt buộc: MstCarSpecController.cs:1749.</summary>
    public string GradeCode { get; set; } = "";

    /// <summary>
    /// Trạng thái lắp ráp — Mst_CarSpec.cs:20. Hai giá trị nghiệp vụ xuất hiện trong guard nguồn:
    /// "CBU" (xe nhập nguyên chiếc) và "CKD" (lắp ráp trong nước) — MstCarSpecController.cs:1862 và :1876.
    /// Bắt buộc: MstCarSpecController.cs:1773.
    /// </summary>
    public string AssemblyStatus { get; set; } = "";

    /// <summary>Xe cứu thương — Mst_CarSpec.cs:21. Chỉ nhận '1'/'0': MstCarSpecController.cs:1810.</summary>
    public string FlagAmbulance { get; set; } = "0";
    /// <summary>
    /// Cần số hoá đơn nhà máy — Mst_CarSpec.cs:22. Chỉ nhận '1'/'0': MstCarSpecController.cs:1797.
    /// RÀNG BUỘC CHÉO: AssemblyStatus = "CKD" thì KHÔNG được để '0' — MstCarSpecController.cs:1876-1888.
    /// </summary>
    public string FlagInvoiceFactory { get; set; } = "0";
    /// <summary>Số chỗ ngồi — Mst_CarSpec.cs:23. Guard: MstCarSpecController.cs:1823.</summary>
    public int? NumberOfSeats { get; set; }
    /// <summary>Ngày tính Quota map VIN — Mst_CarSpec.cs:24.</summary>
    public DateTime? QuotaDate { get; set; }
    /// <summary>Trạng thái — Mst_CarSpec.cs:25. Bắt buộc + chỉ nhận '1'/'0': MstCarSpecController.cs:1849.</summary>
    public string FlagActive { get; set; } = "1";

    /// <summary>Đơn giá (thêm ở nguồn 2023-09-14) — Mst_CarSpec.cs:28.</summary>
    public decimal? UnitPrice { get; set; }
    /// <summary>Cờ tính ngày thanh toán chậm theo ngày bán lẻ — Mst_CarSpec.cs:29. Chỉ nhận '1'/'0': MstCarSpecController.cs:1836.</summary>
    public string FlagDepositPmt { get; set; } = "0";

    /// <summary>
    /// Mã xuất xứ — Mst_CarSpec.cs:30.
    /// RÀNG BUỘC CHÉO: AssemblyStatus = "CBU" thì BẮT BUỘC có OriginNo — MstCarSpecController.cs:1862-1874.
    /// </summary>
    public string? OriginNo { get; set; }

    /// <summary>Ghi chú — Mst_CarSpec.cs:32.</summary>
    public string? Remark { get; set; }

    // --- Vết sửa (Mst_CarSpec.cs:26-27) ---
    /// <summary>LogLUDateTime — Mst_CarSpec.cs:26.</summary>
    public DateTime? LogLUDateTime { get; set; }
    /// <summary>LogLUBy — Mst_CarSpec.cs:27.</summary>
    public string? LogLUBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
