using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

/// <summary>
/// Nghiệp vụ Phiên bản xe (spec) — port 1:1 <c>MstCarSpecController</c> của 2023.3.DMS.Sales.
/// Nguồn: 13.ClientGate/V10/idn.DMSSales/idn.DMSSales.WebAPI/Controllers/MstCarSpecController.cs
/// </summary>
public interface ICarSpecService
{
    Task<(List<CarSpec> Rows, int ItemCount, int PageCount)> SearchAsync(
        string? specCode, string? specDescription, string? assemblyStatus, string? flagActive,
        int pageIndex, int pageSize);

    Task<List<CarSpec>> GetAllActiveAsync();
    Task<List<CarSpec>> GetByModelCodeAsync(string modelCode, string? flagActive);
    Task<CarSpec?> GetByCodeAsync(string specCode);
    Task<CarSpec?> GetByIdAsync(int id);
    Task<bool> SpecCodeExistsAsync(string specCode, int? exceptId = null);
    Task CreateAsync(CarSpec model, string userCode);
    Task UpdateAsync(CarSpec model, string userCode);
    Task DeleteAsync(string specCode);
}

public class CarSpecService(AppDbContext db) : ICarSpecService
{
    /// <summary>Trạng thái lắp ráp xuất hiện trong guard nguồn — MstCarSpecController.cs:1862 ("CBU") và :1876 ("CKD").</summary>
    public static readonly string[] AssemblyStatusValues = ["CBU", "CKD"];

    /// <summary>
    /// port 1:1 <c>Search</c> (MstCarSpecController.cs:44) — nguồn chỉ nhận 4 điều kiện:
    /// SpecCode, SpecDescription, AssemblyStatus, FlagActive.
    /// </summary>
    public async Task<(List<CarSpec> Rows, int ItemCount, int PageCount)> SearchAsync(
        string? specCode, string? specDescription, string? assemblyStatus, string? flagActive,
        int pageIndex, int pageSize)
    {
        var q = db.CarSpecs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(specCode))
            q = q.Where(x => EF.Functions.Like(x.SpecCode, $"%{specCode.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(specDescription))
            q = q.Where(x => EF.Functions.Like(x.SpecDescription, $"%{specDescription.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(assemblyStatus))
            q = q.Where(x => x.AssemblyStatus == assemblyStatus);
        if (!string.IsNullOrWhiteSpace(flagActive))
            q = q.Where(x => x.FlagActive == flagActive);

        var itemCount = await q.CountAsync();
        if (pageSize <= 0) pageSize = 10;
        var pageCount = (itemCount % pageSize == 0) ? itemCount / pageSize : itemCount / pageSize + 1;

        var rows = await q.OrderBy(x => x.ModelCode).ThenBy(x => x.SpecCode)
                          .Skip(pageIndex * pageSize).Take(pageSize)
                          .ToListAsync();
        return (rows, itemCount, pageCount);
    }

    /// <summary>port 1:1 <c>GetAllActive</c> (MstCarSpecController.cs:256).</summary>
    public Task<List<CarSpec>> GetAllActiveAsync()
        => db.CarSpecs.AsNoTracking().Where(x => x.FlagActive == "1")
             .OrderBy(x => x.SpecCode).ToListAsync();

    /// <summary>
    /// port 1:1 <c>GetByModelCode</c> (MstCarSpecController.cs:792) — nguồn nhận thêm tham số
    /// FlagActive để lọc; để trống thì lấy hết.
    /// </summary>
    public Task<List<CarSpec>> GetByModelCodeAsync(string modelCode, string? flagActive)
    {
        var q = db.CarSpecs.AsNoTracking().Where(x => x.ModelCode == modelCode);
        if (!string.IsNullOrWhiteSpace(flagActive)) q = q.Where(x => x.FlagActive == flagActive);
        return q.OrderBy(x => x.SpecCode).ToListAsync();
    }

    /// <summary>port 1:1 <c>GetByCode</c> (MstCarSpecController.cs:617).</summary>
    public Task<CarSpec?> GetByCodeAsync(string specCode)
        => db.CarSpecs.AsNoTracking().FirstOrDefaultAsync(x => x.SpecCode == specCode);

    public Task<CarSpec?> GetByIdAsync(int id)
        => db.CarSpecs.FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> SpecCodeExistsAsync(string specCode, int? exceptId = null)
        => db.CarSpecs.AnyAsync(x => x.SpecCode == specCode && (exceptId == null || x.Id != exceptId));

    /// <summary>port 1:1 <c>Create</c> (MstCarSpecController.cs:1577).</summary>
    public async Task CreateAsync(CarSpec model, string userCode)
    {
        Validate(model);
        model.LogLUBy = userCode;
        model.LogLUDateTime = DateTime.Now;
        db.CarSpecs.Add(model);
        await db.SaveChangesAsync();
    }

    /// <summary>port 1:1 <c>Update</c> (MstCarSpecController.cs:1969) — cùng bộ guard với Create.</summary>
    public async Task UpdateAsync(CarSpec model, string userCode)
    {
        Validate(model);
        var cur = await db.CarSpecs.FirstOrDefaultAsync(x => x.Id == model.Id)
            ?? throw new InvalidOperationException("Không tìm thấy spec cần sửa.");

        cur.ModelCode = model.ModelCode;
        cur.RootSpec = model.RootSpec;
        cur.SpecDescription = model.SpecDescription;
        cur.StdOptCode = model.StdOptCode;
        cur.OCNCode = model.OCNCode;
        cur.GradeCode = model.GradeCode;
        cur.AssemblyStatus = model.AssemblyStatus;
        cur.FlagAmbulance = model.FlagAmbulance;
        cur.FlagInvoiceFactory = model.FlagInvoiceFactory;
        cur.NumberOfSeats = model.NumberOfSeats;
        cur.QuotaDate = model.QuotaDate;
        cur.FlagActive = model.FlagActive;
        cur.UnitPrice = model.UnitPrice;
        cur.FlagDepositPmt = model.FlagDepositPmt;
        cur.OriginNo = model.OriginNo;
        cur.Remark = model.Remark;
        cur.LogLUBy = userCode;
        cur.LogLUDateTime = DateTime.Now;
        await db.SaveChangesAsync();
    }

    /// <summary>port 1:1 <c>Delete</c> (MstCarSpecController.cs:2334) — xoá theo SpecCode.</summary>
    public async Task DeleteAsync(string specCode)
    {
        var cur = await db.CarSpecs.FirstOrDefaultAsync(x => x.SpecCode == specCode);
        if (cur == null) return;
        db.CarSpecs.Remove(cur);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Guard theo đúng thứ tự nguồn MstCarSpecController.cs:1702-1888:
    /// SpecCode (:1702) → RootSpec (:1713) → ModelCode (:1725) → StdOptCode (:1737)
    /// → GradeCode (:1749) → OCNCode (:1761) → AssemblyStatus (:1773) → SpecDescription (:1785)
    /// → FlagInvoiceFactory (:1797) → FlagAmbulance (:1810) → NumberOfSeats (:1823)
    /// → FlagDepositPmt (:1836) → FlagActive (:1849)
    /// → CHÉO: CBU phải có OriginNo (:1862) → CHÉO: CKD không được FlagInvoiceFactory='0' (:1876).
    /// </summary>
    private static void Validate(CarSpec m)
    {
        if (string.IsNullOrWhiteSpace(m.SpecCode))
            throw new ArgumentException("Mã Spec không được để trống.");                  // :1702
        if (string.IsNullOrWhiteSpace(m.RootSpec))
            throw new ArgumentException("RootSpec không được để trống.");                 // :1713
        if (string.IsNullOrWhiteSpace(m.ModelCode))
            throw new ArgumentException("Mã Model không được để trống.");                 // :1725
        if (string.IsNullOrWhiteSpace(m.StdOptCode))
            throw new ArgumentException("Mã StandardOption không được để trống.");        // :1737
        if (string.IsNullOrWhiteSpace(m.GradeCode))
            throw new ArgumentException("Mã Grade không được để trống.");                 // :1749
        if (string.IsNullOrWhiteSpace(m.OCNCode))
            throw new ArgumentException("Mã OCN không được để trống.");                   // :1761
        if (string.IsNullOrWhiteSpace(m.AssemblyStatus))
            throw new ArgumentException("Trạng thái lắp ráp không được để trống.");       // :1773
        if (string.IsNullOrWhiteSpace(m.SpecDescription))
            throw new ArgumentException("Mô tả Spec không được để trống.");               // :1785

        CheckFlag(m.FlagInvoiceFactory, "FlagInvoiceFactory");                            // :1797
        CheckFlag(m.FlagAmbulance, "FlagAmbulance");                                      // :1810
        CheckFlag(m.FlagDepositPmt, "FlagDepositPmt");                                    // :1836
        CheckFlag(m.FlagActive, "FlagActive");                                            // :1849

        // :1823 — nguồn kiểm NumberOfSeats hợp lệ; số chỗ ngồi âm hoặc 0 là vô nghĩa nghiệp vụ.
        if (m.NumberOfSeats is <= 0)
            throw new ArgumentException("Số chỗ ngồi phải lớn hơn 0.");

        // :1862-1874 — xe nhập nguyên chiếc (CBU) BẮT BUỘC khai mã xuất xứ.
        if (string.Equals(m.AssemblyStatus, "CBU", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(m.OriginNo))
            throw new ArgumentException("Xe CBU (nhập nguyên chiếc) bắt buộc phải có Mã xuất xứ.");

        // :1876-1888 — xe lắp ráp trong nước (CKD) KHÔNG được để FlagInvoiceFactory = '0'.
        if (string.Equals(m.AssemblyStatus, "CKD", StringComparison.OrdinalIgnoreCase)
            && m.FlagInvoiceFactory == "0")
            throw new ArgumentException("Xe CKD (lắp ráp trong nước) bắt buộc cần số hoá đơn nhà máy (FlagInvoiceFactory = '1').");
    }

    /// <summary>Cờ chỉ nhận '1'/'0' — Const.Main.BE.cs:266-274.</summary>
    private static void CheckFlag(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} không được để trống.");
        if (value is not ("1" or "0"))
            throw new ArgumentException($"{name} chỉ nhận '1' hoặc '0' (không dùng 'Y'/'N').");
    }
}
