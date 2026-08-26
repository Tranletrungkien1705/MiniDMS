namespace MiniDMS.Models.Entities;

/// <summary>Tổ chức/khách hàng thuê bao (multi-tenant). Mỗi Org dữ liệu DMS riêng, cô lập; user thuộc 1 Org.</summary>
public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Bảng dữ liệu thuộc về 1 Org — bị lọc theo tenant hiện tại + tự đóng dấu OrgId khi tạo.</summary>
public interface IOrgOwned
{
    Guid OrgId { get; set; }
}
