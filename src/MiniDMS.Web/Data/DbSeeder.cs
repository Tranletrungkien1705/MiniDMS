using Microsoft.AspNetCore.Identity;
using MiniDMS.Models.Entities;

namespace MiniDMS.Data;

public class DbSeeder(
    UserManager<IdentityUser> userMgr,
    RoleManager<IdentityRole> roleMgr,
    AppDbContext db)
{
    public static readonly string[] Roles = ["Admin", "Warehouse", "Sales", "Accounting"];

    public async Task SeedAsync()
    {
        await db.Database.EnsureCreatedAsync(); // [local run] tao schema tu model (khong can migration)
        // Roles
        foreach (var role in Roles)
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        // Demo users
        await EnsureUser("admin@minidms.com",      "Admin@123",  "Admin");
        await EnsureUser("warehouse@minidms.com",  "Kho@123",    "Warehouse");
        await EnsureUser("sales@minidms.com",      "Sales@123",  "Sales");
        await EnsureUser("accounting@minidms.com", "Acc@123",    "Accounting");

        // Seed categories & products if empty
        if (!db.ProductCategories.Any())
        {
            var cats = new[] {
                new ProductCategory { Name = "Áo", Code = "AO" },
                new ProductCategory { Name = "Quần", Code = "QUAN" },
                new ProductCategory { Name = "Phụ kiện", Code = "PK" },
            };
            db.ProductCategories.AddRange(cats);
            await db.SaveChangesAsync();

            db.Products.AddRange(
                new Product { SKU = "AO-001", Name = "Áo sơ mi trắng basic", CategoryId = cats[0].Id, CostPrice = 120000, SalePrice = 250000 },
                new Product { SKU = "AO-002", Name = "Áo polo nam",          CategoryId = cats[0].Id, CostPrice = 150000, SalePrice = 320000 },
                new Product { SKU = "QUAN-001", Name = "Quần jeans slim",    CategoryId = cats[1].Id, CostPrice = 200000, SalePrice = 450000 },
                new Product { SKU = "PK-001",   Name = "Thắt lưng da",       CategoryId = cats[2].Id, CostPrice = 80000,  SalePrice = 180000 }
            );
            await db.SaveChangesAsync();
        }

        // Seed khách hàng demo
        if (!db.Customers.Any())
        {
            db.Customers.AddRange(
                new Customer { Code = "KH001", Name = "Cửa hàng Minh Anh",   Phone = "0901234567", Address = "12 Lê Lợi, Q.1, TP.HCM" },
                new Customer { Code = "KH002", Name = "Shop thời trang Hà",   Phone = "0912345678", Address = "45 Nguyễn Huệ, Q.1, TP.HCM" },
                new Customer { Code = "KH003", Name = "Đại lý Phương Nam",    Phone = "0923456789", Address = "88 CMT8, Q.3, TP.HCM" }
            );
            await db.SaveChangesAsync();
        }

        // Nhập tồn kho đầu kỳ để tạo/xác nhận đơn hàng được (xác nhận sẽ trừ kho)
        if (!db.StockTransactions.Any())
        {
            foreach (var p in db.Products.ToList())
                db.StockTransactions.Add(new StockTransaction
                {
                    ProductId = p.Id, Type = TransactionType.In, Quantity = 100,
                    Note = "Tồn đầu kỳ", RefNo = "INIT", CreatedBy = "seed"
                });
            await db.SaveChangesAsync();
        }
    }

    private async Task EnsureUser(string email, string password, string role)
    {
        var user = await userMgr.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userMgr.CreateAsync(user, password);
        }
        if (!await userMgr.IsInRoleAsync(user, role))
            await userMgr.AddToRoleAsync(user, role);
    }
}
