namespace MiniDMS.Models.Entities;

/// <summary>
/// Đại lý — port 1:1 bảng <c>Mst_Dealer</c> của hệ nguồn 2023.3.DMS.Sales.
/// Nguồn cột: 12.Dev.Common/idn.DMSSales.Common/Models/DMSSales/Mst_Dealer.cs:10-54 — đếm bằng máy: 47 cột (2 cột join AreaName/AreaRootName không lưu ở đích).
/// Nguồn nghiệp vụ (guard + endpoint): 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstDealerController.cs
///   - Search              : MstDealerController.cs:42
///   - GetAllActive        : MstDealerController.cs:279
///   - GetByDealerCode     : MstDealerController.cs:859
///   - Create              : MstDealerController.cs:1695
///   - Update              : MstDealerController.cs:2105
///   - Delete              : MstDealerController.cs:2510
/// Cờ dùng '1'/'0' (KHÔNG phải 'Y'/'N') — nguồn: 12.Dev.Common/idn.DMSSales.Constants/Const.Main.BE.cs:266-274
///   (class Flag: Active = "1", Inactive = "0").
/// </summary>
public class Dealer : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }

    // --- Khoá + phân loại (Mst_Dealer.cs:10-12) ---
    /// <summary>Mã đại lý — Mst_Dealer.cs:10. Bắt buộc khi tạo: MstDealerController.cs:1844.</summary>
    public string DealerCode { get; set; } = "";
    /// <summary>Loại đại lý — Mst_Dealer.cs:11. Bắt buộc khi tạo: MstDealerController.cs:1855.</summary>
    public string DealerType { get; set; } = "";
    /// <summary>Mã tỉnh — Mst_Dealer.cs:12. Bắt buộc khi tạo: MstDealerController.cs:1866.</summary>
    public string ProvinceCode { get; set; } = "";

    // --- Khu vực (Mst_Dealer.cs:13-16) ---
    /// <summary>Mã khu vực — Mst_Dealer.cs:13.</summary>
    public string? AreaCode { get; set; }
    /// <summary>Mã khu vực cha (miền MB/MT/MN) — Mst_Dealer.cs:15.</summary>
    public string? AreaRootCode { get; set; }

    // --- Phạm vi tổ chức (Mst_Dealer.cs:17-18) ---
    /// <summary>BUCode — Mst_Dealer.cs:17. Hệ nguồn dùng để giới hạn phạm vi xem theo đơn vị (BU scope).</summary>
    public string? BUCode { get; set; }
    /// <summary>BUPattern — Mst_Dealer.cs:18. Mẫu BUCode dùng cho điều kiện `like @BUPattern` khi lọc phạm vi.</summary>
    public string? BUPattern { get; set; }

    // --- Tên + cờ trạng thái (Mst_Dealer.cs:19-22) ---
    /// <summary>Tên đại lý — Mst_Dealer.cs:19. Bắt buộc khi tạo: MstDealerController.cs:1877; khi sửa: MstDealerController.cs:2264.</summary>
    public string DealerName { get; set; } = "";
    /// <summary>FlagDirect (đại lý trực tiếp) — Mst_Dealer.cs:20. Bắt buộc + chỉ nhận '1'/'0': MstDealerController.cs:1888 và :1899.</summary>
    public string FlagDirect { get; set; } = "0";
    /// <summary>FlagActive — Mst_Dealer.cs:21. Create KHÔNG nhận cột này (xem MstDealerController.cs:2006-2057); Update mới kiểm: MstDealerController.cs:2275/:2287.</summary>
    public string FlagActive { get; set; } = "1";
    /// <summary>Quy mô đại lý — Mst_Dealer.cs:22.</summary>
    public string? DealerScale { get; set; }

    // --- Liên lạc đại lý (Mst_Dealer.cs:23-24) ---
    /// <summary>Tel đại lý — Mst_Dealer.cs:23.</summary>
    public string? DealerPhoneNo { get; set; }
    /// <summary>Fax đại lý — Mst_Dealer.cs:24.</summary>
    public string? DealerFaxNo { get; set; }

    // --- Pháp nhân + địa chỉ (Mst_Dealer.cs:25-29) ---
    /// <summary>Tên công ty — Mst_Dealer.cs:25.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Địa chỉ ĐKKD — Mst_Dealer.cs:26.</summary>
    public string? CompanyAddress { get; set; }
    /// <summary>Địa chỉ showroom — Mst_Dealer.cs:27.</summary>
    public string? ShowroomAddress { get; set; }
    /// <summary>Địa chỉ xưởng dịch vụ — Mst_Dealer.cs:28.</summary>
    public string? GarageAddress { get; set; }
    /// <summary>Tel xưởng dịch vụ — Mst_Dealer.cs:29.</summary>
    public string? GaragePhoneNo { get; set; }
    /// <summary>Fax xưởng dịch vụ — Mst_Dealer.cs:30.</summary>
    public string? GarageFaxNo { get; set; }

    // --- Nhân sự đầu mối (Mst_Dealer.cs:31-39) ---
    /// <summary>Giám đốc — Mst_Dealer.cs:31.</summary>
    public string? DirectorName { get; set; }
    /// <summary>Mobile giám đốc — Mst_Dealer.cs:32.</summary>
    public string? DirectorPhoneNo { get; set; }
    /// <summary>Email giám đốc — Mst_Dealer.cs:33.</summary>
    public string? DirectorEmail { get; set; }
    /// <summary>Trưởng phòng bán hàng — Mst_Dealer.cs:34.</summary>
    public string? SalesManagerName { get; set; }
    /// <summary>Mobile TP bán hàng — Mst_Dealer.cs:35.</summary>
    public string? SalesManagerPhoneNo { get; set; }
    /// <summary>Email TP bán hàng — Mst_Dealer.cs:36.</summary>
    public string? SalesManagerEmail { get; set; }
    /// <summary>Trưởng phòng dịch vụ — Mst_Dealer.cs:37.</summary>
    public string? GarageManagerName { get; set; }
    /// <summary>Mobile TP dịch vụ — Mst_Dealer.cs:38.</summary>
    public string? GarageManagerPhoneNo { get; set; }
    /// <summary>Email TP dịch vụ — Mst_Dealer.cs:39.</summary>
    public string? GarageManagerEmail { get; set; }

    // --- Thuế + người ký (Mst_Dealer.cs:40-46) ---
    /// <summary>Mã số thuế — Mst_Dealer.cs:40.</summary>
    public string? TaxCode { get; set; }
    /// <summary>Người liên lạc — Mst_Dealer.cs:41.</summary>
    public string? ContactName { get; set; }
    /// <summary>Đại diện ký hợp đồng — Mst_Dealer.cs:42.</summary>
    public string? Signer { get; set; }
    /// <summary>Chức vụ đại diện ký hợp đồng — Mst_Dealer.cs:43.</summary>
    public string? SignerPosition { get; set; }
    /// <summary>Người đại diện ký phụ lục hợp đồng (PLHĐ) — Mst_Dealer.cs:44.</summary>
    public string? CtrNoSigner { get; set; }
    /// <summary>Chức vụ đại diện ký PLHĐ — Mst_Dealer.cs:45.</summary>
    public string? CtrNoSignerPosition { get; set; }
    /// <summary>Nhân viên HTC phụ trách — Mst_Dealer.cs:46 (tên cột nguồn viết HOA: HTCSTAFFINCHARGE).</summary>
    public string? HTCStaffInCharge { get; set; }

    /// <summary>Ghi chú — Mst_Dealer.cs:47.</summary>
    public string? Remark { get; set; }

    // --- 5 dòng địa chỉ in ấn (Mst_Dealer.cs:48-52) ---
    /// <summary>Địa chỉ 1 — Mst_Dealer.cs:48.</summary>
    public string? DealerAddress01 { get; set; }
    /// <summary>Địa chỉ 2 — Mst_Dealer.cs:49.</summary>
    public string? DealerAddress02 { get; set; }
    /// <summary>Địa chỉ 3 — Mst_Dealer.cs:50.</summary>
    public string? DealerAddress03 { get; set; }
    /// <summary>Địa chỉ 4 — Mst_Dealer.cs:51.</summary>
    public string? DealerAddress04 { get; set; }
    /// <summary>Địa chỉ 5 — Mst_Dealer.cs:52.</summary>
    public string? DealerAddress05 { get; set; }

    // --- 4 cờ nghiệp vụ, đều bắt buộc + chỉ nhận '1'/'0' ---
    /// <summary>FlagTCG (xe đã qua sử dụng) — Mst_Dealer.cs:53. Guard: MstDealerController.cs:1910 và :1921.</summary>
    public string FlagTCG { get; set; } = "0";
    /// <summary>Cờ sinh Lệnh xuất xe tự động — Mst_Dealer.cs:54. Guard: MstDealerController.cs:1932 và :1943.</summary>
    public string FlagAutoLXX { get; set; } = "0";
    /// <summary>Cờ map VIN tự động — Mst_Dealer.cs:55. Guard: MstDealerController.cs:1954 và :1967.</summary>
    public string FlagAutoMapVIN { get; set; } = "0";
    /// <summary>Cờ duyệt đơn hàng tự động — Mst_Dealer.cs:56. Guard: MstDealerController.cs:1979 và :1989.</summary>
    public string FlagAutoSOAppr { get; set; } = "0";

    // --- Vết sửa (hệ nguồn có LogLUDateTime/LogLUBy ở hầu hết bảng master) ---
    public DateTime? LogLUDateTime { get; set; }
    public string? LogLUBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
