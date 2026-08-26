using Microsoft.AspNetCore.Identity;

namespace MiniDMS.Models.Entities;

/// <summary>User Identity gắn với 1 tổ chức (multi-tenant). OrgId → claim "OrgId" → lọc dữ liệu.</summary>
public class ApplicationUser : IdentityUser
{
    public Guid OrgId { get; set; }
}
