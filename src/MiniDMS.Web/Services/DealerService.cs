using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

/// <summary>
/// Nghiệp vụ Đại lý — port 1:1 <c>MstDealerController</c> của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstDealerController.cs
/// </summary>
public interface IDealerService
{
    Task<(List<Dealer> Rows, int ItemCount, int PageCount)> SearchAsync(
        string? dealerCode, string? dealerName,
        string? flagAutoLXX, string? flagAutoMapVIN, string? flagAutoSOAppr, string? flagActive,
        int pageIndex, int pageSize);

    Task<List<Dealer>> GetAllActiveAsync(bool includeInactive);
    Task<Dealer?> GetByDealerCodeAsync(string dealerCode);
    Task<Dealer?> GetByIdAsync(int id);
    Task<bool> DealerCodeExistsAsync(string dealerCode, int? exceptId = null);
    Task CreateAsync(Dealer model, string userCode);
    Task UpdateAsync(Dealer model, string userCode);
    Task DeleteAsync(string dealerCode);
}

public class DealerService(AppDbContext db) : IDealerService
{
    /// <summary>Tập giá trị hợp lệ của mọi cờ trong Mst_Dealer — Const.Main.BE.cs:266-274 (Flag.Active="1"/Inactive="0").</summary>
    public static readonly string[] FlagValues = ["1", "0"];

    /// <summary>
    /// Tìm kiếm — port 1:1 <c>Search</c> (MstDealerController.cs:42) và bộ dựng điều kiện
    /// <c>strWhereClause_Mst_Dealer</c> (MstDealerController.cs:3687-3738).
    /// Toán tử giữ NGUYÊN theo nguồn: DealerName/DealerCode dùng "like %...%" (:3694, :3698);
    /// 4 cờ dùng "in" trên danh sách tách bằng dấu phẩy (:3707, :3716, :3725, :3734).
    /// </summary>
    public async Task<(List<Dealer> Rows, int ItemCount, int PageCount)> SearchAsync(
        string? dealerCode, string? dealerName,
        string? flagAutoLXX, string? flagAutoMapVIN, string? flagAutoSOAppr, string? flagActive,
        int pageIndex, int pageSize)
    {
        var q = db.Dealers.AsNoTracking();

        // MstDealerController.cs:3694 — like %DealerName%
        if (!string.IsNullOrWhiteSpace(dealerName))
            q = q.Where(x => EF.Functions.Like(x.DealerName, $"%{dealerName.Trim()}%"));

        // MstDealerController.cs:3698 — like %DealerCode%  (nguồn dùng LIKE, KHÔNG phải "=")
        if (!string.IsNullOrWhiteSpace(dealerCode))
            q = q.Where(x => EF.Functions.Like(x.DealerCode, $"%{dealerCode.Trim()}%"));

        // MstDealerController.cs:3700-3708 — FlagActive "in" (chuỗi "1,0" tách bằng dấu phẩy)
        var lstActive = SplitFlag(flagActive);
        if (lstActive.Count > 0) q = q.Where(x => lstActive.Contains(x.FlagActive));

        var lstLXX = SplitFlag(flagAutoLXX);                     // :3709-3717
        if (lstLXX.Count > 0) q = q.Where(x => lstLXX.Contains(x.FlagAutoLXX));

        var lstMapVIN = SplitFlag(flagAutoMapVIN);               // :3718-3726
        if (lstMapVIN.Count > 0) q = q.Where(x => lstMapVIN.Contains(x.FlagAutoMapVIN));

        var lstSOAppr = SplitFlag(flagAutoSOAppr);               // :3727-3735
        if (lstSOAppr.Count > 0) q = q.Where(x => lstSOAppr.Contains(x.FlagAutoSOAppr));

        // Phân trang: nguồn tính itemCount từ bảng MySummaryTable rồi
        // pageCount = (itemCount % pageSize == 0) ? itemCount/pageSize : itemCount/pageSize + 1  — MstDealerController.cs:243
        var itemCount = await q.CountAsync();
        if (pageSize <= 0) pageSize = 10;                        // nguồn: PageSizeConfig khi pageSize = 0 (:239)
        var pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;

        var rows = await q.OrderBy(x => x.DealerCode)
                          .Skip(pageIndex * pageSize).Take(pageSize)
                          .ToListAsync();
        return (rows, itemCount, pageCount);
    }

    /// <summary>
    /// port 1:1 <c>GetAllActive</c> (MstDealerController.cs:279) — mặc định chỉ trả đại lý FlagActive='1';
    /// tham số <c>FlagIncludeInactive</c> mới cho lấy cả bản ngừng.
    /// </summary>
    public Task<List<Dealer>> GetAllActiveAsync(bool includeInactive)
        => db.Dealers.AsNoTracking()
             .Where(x => includeInactive || x.FlagActive == "1")
             .OrderBy(x => x.DealerCode).ToListAsync();

    /// <summary>port 1:1 <c>GetByDealerCode</c> (MstDealerController.cs:859).</summary>
    public Task<Dealer?> GetByDealerCodeAsync(string dealerCode)
        => db.Dealers.AsNoTracking().FirstOrDefaultAsync(x => x.DealerCode == dealerCode);

    public Task<Dealer?> GetByIdAsync(int id)
        => db.Dealers.FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> DealerCodeExistsAsync(string dealerCode, int? exceptId = null)
        => db.Dealers.AnyAsync(x => x.DealerCode == dealerCode && (exceptId == null || x.Id != exceptId));

    /// <summary>
    /// port 1:1 <c>Create</c> (MstDealerController.cs:1695). Guard theo đúng thứ tự nguồn:
    /// DealerCode (:1844) → DealerType (:1855) → ProvinceCode (:1866) → DealerName (:1877)
    /// → FlagDirect rỗng (:1888) / không thuộc {'1','0'} (:1899)
    /// → FlagTCG (:1910 / :1921) → FlagAutoLXX (:1932 / :1943)
    /// → FlagAutoMapVIN (:1954 / :1967) → FlagAutoSOAppr (:1979 / :1989).
    /// LƯU Ý 1:1: Create của nguồn KHÔNG nhận FlagActive / AreaCode / BUCode / BUPattern
    /// (xem danh sách tham số truyền sang biz — MstDealerController.cs:2006-2057).
    /// </summary>
    public async Task CreateAsync(Dealer model, string userCode)
    {
        Validate(model, isCreate: true);
        model.FlagActive = "1";                 // nguồn không cho nhập khi tạo → bản ghi mới luôn đang hoạt động
        model.LogLUBy = userCode;
        model.LogLUDateTime = DateTime.Now;
        db.Dealers.Add(model);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// port 1:1 <c>Update</c> (MstDealerController.cs:2105). Khác Create ở chỗ nguồn CÓ kiểm FlagActive:
    /// rỗng (:2275) và không thuộc {'1','0'} (:2287); KHÔNG kiểm DealerType/ProvinceCode
    /// (grep guard Update: chỉ có DealerCode, DealerName, FlagActive, FlagDirect, FlagTCG,
    /// FlagAutoLXX, FlagAutoMapVIN, FlagAutoSOAppr).
    /// </summary>
    public async Task UpdateAsync(Dealer model, string userCode)
    {
        Validate(model, isCreate: false);
        var cur = await db.Dealers.FirstOrDefaultAsync(x => x.Id == model.Id)
            ?? throw new InvalidOperationException("Không tìm thấy đại lý cần sửa.");

        // Nguồn dùng DealerCode làm khoá, không cho đổi khi sửa (biz Mst_Dealer_Update định vị theo DealerCode).
        cur.DealerName = model.DealerName;
        cur.DealerType = model.DealerType;
        cur.ProvinceCode = model.ProvinceCode;
        cur.AreaCode = model.AreaCode;
        cur.AreaRootCode = model.AreaRootCode;
        cur.BUCode = model.BUCode;
        cur.BUPattern = model.BUPattern;
        cur.DealerScale = model.DealerScale;
        cur.DealerPhoneNo = model.DealerPhoneNo;
        cur.DealerFaxNo = model.DealerFaxNo;
        cur.CompanyName = model.CompanyName;
        cur.CompanyAddress = model.CompanyAddress;
        cur.ShowroomAddress = model.ShowroomAddress;
        cur.GarageAddress = model.GarageAddress;
        cur.GaragePhoneNo = model.GaragePhoneNo;
        cur.GarageFaxNo = model.GarageFaxNo;
        cur.DirectorName = model.DirectorName;
        cur.DirectorPhoneNo = model.DirectorPhoneNo;
        cur.DirectorEmail = model.DirectorEmail;
        cur.SalesManagerName = model.SalesManagerName;
        cur.SalesManagerPhoneNo = model.SalesManagerPhoneNo;
        cur.SalesManagerEmail = model.SalesManagerEmail;
        cur.GarageManagerName = model.GarageManagerName;
        cur.GarageManagerPhoneNo = model.GarageManagerPhoneNo;
        cur.GarageManagerEmail = model.GarageManagerEmail;
        cur.TaxCode = model.TaxCode;
        cur.ContactName = model.ContactName;
        cur.Signer = model.Signer;
        cur.SignerPosition = model.SignerPosition;
        cur.CtrNoSigner = model.CtrNoSigner;
        cur.CtrNoSignerPosition = model.CtrNoSignerPosition;
        cur.HTCStaffInCharge = model.HTCStaffInCharge;
        cur.Remark = model.Remark;
        cur.DealerAddress01 = model.DealerAddress01;
        cur.DealerAddress02 = model.DealerAddress02;
        cur.DealerAddress03 = model.DealerAddress03;
        cur.DealerAddress04 = model.DealerAddress04;
        cur.DealerAddress05 = model.DealerAddress05;
        cur.FlagDirect = model.FlagDirect;
        cur.FlagActive = model.FlagActive;       // chỉ Update mới đổi được — xem guard :2275
        cur.FlagTCG = model.FlagTCG;
        cur.FlagAutoLXX = model.FlagAutoLXX;
        cur.FlagAutoMapVIN = model.FlagAutoMapVIN;
        cur.FlagAutoSOAppr = model.FlagAutoSOAppr;
        cur.LogLUBy = userCode;
        cur.LogLUDateTime = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>port 1:1 <c>Delete</c> (MstDealerController.cs:2510) — xoá theo DealerCode.</summary>
    public async Task DeleteAsync(string dealerCode)
    {
        var cur = await db.Dealers.FirstOrDefaultAsync(x => x.DealerCode == dealerCode);
        if (cur == null) return;
        db.Dealers.Remove(cur);
        await db.SaveChangesAsync();
    }

    /// <summary>Guard chung — thứ tự và điều kiện bám đúng nguồn (xem chú thích Create/Update).</summary>
    private static void Validate(Dealer m, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(m.DealerCode))
            throw new ArgumentException("Mã đại lý không được để trống.");           // :1844 / :2253

        if (isCreate)
        {
            if (string.IsNullOrWhiteSpace(m.DealerType))
                throw new ArgumentException("Loại đại lý không được để trống.");     // :1855
            if (string.IsNullOrWhiteSpace(m.ProvinceCode))
                throw new ArgumentException("Mã tỉnh không được để trống.");         // :1866
        }

        if (string.IsNullOrWhiteSpace(m.DealerName))
            throw new ArgumentException("Tên đại lý không được để trống.");          // :1877 / :2264

        CheckFlag(m.FlagDirect, "FlagDirect");                                       // :1888 / :1899
        if (!isCreate) CheckFlag(m.FlagActive, "FlagActive");                        // :2275 / :2287 (Update mới có)
        CheckFlag(m.FlagTCG, "FlagTCG");                                             // :1910 / :1921
        CheckFlag(m.FlagAutoLXX, "FlagAutoLXX");                                     // :1932 / :1943
        CheckFlag(m.FlagAutoMapVIN, "FlagAutoMapVIN");                               // :1954 / :1967
        CheckFlag(m.FlagAutoSOAppr, "FlagAutoSOAppr");                               // :1979 / :1989
    }

    private static void CheckFlag(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} không được để trống.");
        if (!FlagValues.Contains(value))
            throw new ArgumentException($"{name} chỉ nhận '1' hoặc '0' (không dùng 'Y'/'N').");
    }

    /// <summary>Tách chuỗi cờ nhiều giá trị "1,0" — nguồn dùng CUtils.StrSplit rồi bọc nháy để dựng IN (:3702-3707).</summary>
    private static List<string> SplitFlag(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
