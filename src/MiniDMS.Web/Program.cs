using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Services;

var builder = WebApplication.CreateBuilder(args);
// Cloud host (Render/Koyeb) cấp cổng qua biến PORT; local mặc định 8080
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
{
    o.Password.RequireDigit = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

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
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/healthz", () => "ok");

app.Run();
