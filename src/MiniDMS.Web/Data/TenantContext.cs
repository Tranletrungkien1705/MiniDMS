using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MiniDMS.Models.Entities;

namespace MiniDMS.Data;

/// <summary>Ngữ cảnh tenant của request. Middleware set OrgId (từ claim "OrgId" của user), DbContext lọc.</summary>
public interface ITenantContext
{
    Guid OrgId { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    /// <summary>Org mặc định (user/dữ liệu seed). Cố định để ổn định qua các lần khởi động.</summary>
    public static readonly Guid DefaultOrgId = new("44444444-4444-4444-4444-444444444444");
    public const string DefaultApiKey = "demo-minidms";
    public const string ClaimType = "OrgId";

    public Guid OrgId { get; set; } = DefaultOrgId;
}

/// <summary>Gắn claim "OrgId" vào principal khi đăng nhập → cookie mang OrgId, khỏi truy DB mỗi request.</summary>
public sealed class OrgClaimsPrincipalFactory(
    UserManager<ApplicationUser> userMgr,
    RoleManager<IdentityRole> roleMgr,
    Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userMgr, roleMgr, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        id.AddClaim(new Claim(TenantContext.ClaimType, user.OrgId.ToString()));
        return id;
    }
}
