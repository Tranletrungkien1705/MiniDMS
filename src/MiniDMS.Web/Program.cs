using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Services;
using Serilog;

// Npgsql: DateTime (Kind Local/Unspecified) '' timestamp without time zone (khong phai timestamptz)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("minidms");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
// Cloud host (Render/Koyeb) cấp cổng qua biến PORT; local mặc định 8080
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=minidms.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (MiniDMS.Data.DbUtil.IsPostgres(conn)) o.UseNpgsql(MiniDMS.Data.DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});

builder.Services.AddScoped<MiniDMS.Data.ITenantContext, MiniDMS.Data.TenantContext>();   // multi-tenant

builder.Services.AddIdentity<MiniDMS.Models.Entities.ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequireDigit = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddClaimsPrincipalFactory<MiniDMS.Data.OrgClaimsPrincipalFactory>()   // gắn claim OrgId vào cookie đăng nhập
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DbSeeder>();

// Client HĐĐT (QinvoiceLite). Timeout cao cho cold-start Render free.
builder.Services.AddHttpClient<IQinvoiceClient, QinvoiceClient>(c => c.Timeout = TimeSpan.FromSeconds(120));
// Client kế toán (MiniAccounting) — tự sinh bút toán khi xác nhận đơn.
builder.Services.AddHttpClient<IMiniAccountingClient, MiniAccountingClient>(c => c.Timeout = TimeSpan.FromSeconds(120));
// Client kho (MiniWMS) — xuất kho khi giao đơn.
builder.Services.AddHttpClient<IWmsClient, WmsClient>(c => c.Timeout = TimeSpan.FromSeconds(20));
// Client đối soát (MiniReconcile) — đẩy công nợ khi xác nhận đơn.
builder.Services.AddHttpClient<IReconcileClient, ReconcileClient>(c => c.Timeout = TimeSpan.FromSeconds(20));

builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();
app.UseFleetObs();

// Seed roles + default users on startup
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// Multi-tenant: OrgId của request = claim "OrgId" của user đăng nhập. Đặt TRƯỚC khi controller dùng AppDbContext.
app.Use(async (ctx, next) =>
{
    var claim = ctx.User?.FindFirst(MiniDMS.Data.TenantContext.ClaimType)?.Value;
    if (Guid.TryParse(claim, out var orgId))
        ctx.RequestServices.GetRequiredService<MiniDMS.Data.ITenantContext>().OrgId = orgId;
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/healthz", () => "ok");

app.Run();
