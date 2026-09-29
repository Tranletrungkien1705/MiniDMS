using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Models.Entities;

namespace MiniDMS.Data;

public class DbSeeder(
    UserManager<ApplicationUser> userMgr,
    RoleManager<IdentityRole> roleMgr,
    AppDbContext db)
{
    public static readonly string[] Roles = ["Admin", "Warehouse", "Sales", "Accounting"];

    public async Task SeedAsync()
    {
        await db.Database.EnsureCreatedAsync(); // [local run] tao schema tu model (khong can migration)
        await CreateMissingTablesAsync();       // DB DA TON TAI: EnsureCreated la NO-OP => tu tao bang MOI con thieu
        await MigratePostgresAsync();           // DB cloud cu: them Orgs + cot OrgId neu thieu

        // Org mặc định (dữ liệu + user seed)
        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo DMS", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }

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

        // ===== Seed Dai ly - port 1:1 Mst_Dealer (MstDealerController.cs:1695 Create) =====
        // Co dung '1'/'0' theo Const.Main.BE.cs:266-274, KHONG dung 'Y'/'N'.
        if (!db.Dealers.Any())
        {
            db.Dealers.AddRange(
                new Dealer
                {
                    DealerCode = "DL001", DealerType = "3S", ProvinceCode = "01",
                    AreaCode = "HN", AreaRootCode = "MB", BUCode = "HQ.MB.HN", BUPattern = "HQ.MB.%",
                    DealerName = "Dai ly Thang Long", CompanyName = "Cong ty CP Oto Thang Long",
                    CompanyAddress = "Km 10 Nguyen Trai, Ha Dong, Ha Noi",
                    ShowroomAddress = "Km 10 Nguyen Trai, Ha Dong, Ha Noi",
                    TaxCode = "0101234567", DealerPhoneNo = "02433553355",
                    DirectorName = "Nguyen Van A", DirectorEmail = "gd@thanglong.vn",
                    Signer = "Nguyen Van A", SignerPosition = "Giam doc",
                    DealerScale = "LON",
                    FlagDirect = "1", FlagActive = "1", FlagTCG = "1",
                    FlagAutoLXX = "1", FlagAutoMapVIN = "1", FlagAutoSOAppr = "0"
                },
                new Dealer
                {
                    DealerCode = "DL002", DealerType = "2S", ProvinceCode = "79",
                    AreaCode = "HCM", AreaRootCode = "MN", BUCode = "HQ.MN.HCM", BUPattern = "HQ.MN.%",
                    DealerName = "Dai ly Sai Gon", CompanyName = "Cong ty TNHH Oto Sai Gon",
                    CompanyAddress = "215 Dien Bien Phu, Q.3, TP.HCM",
                    TaxCode = "0302345678", DealerPhoneNo = "02839303030",
                    DirectorName = "Tran Thi B", DealerScale = "VUA",
                    FlagDirect = "0", FlagActive = "1", FlagTCG = "0",
                    FlagAutoLXX = "0", FlagAutoMapVIN = "1", FlagAutoSOAppr = "1"
                },
                new Dealer
                {
                    DealerCode = "DL003", DealerType = "1S", ProvinceCode = "48",
                    AreaCode = "DN", AreaRootCode = "MT", BUCode = "HQ.MT.DN", BUPattern = "HQ.MT.%",
                    DealerName = "Dai ly Da Nang (da ngung)", DealerScale = "NHO",
                    FlagDirect = "0", FlagActive = "0", FlagTCG = "0",
                    FlagAutoLXX = "0", FlagAutoMapVIN = "0", FlagAutoSOAppr = "0"
                }
            );
            await db.SaveChangesAsync();
        }

        // ===== Seed Phien ban xe - port 1:1 Mst_CarSpec (MstCarSpecController.cs:1577 Create) =====
        // Bo du lieu ton trong 2 rang buoc cheo cua nguon:
        //  - CBU bat buoc co OriginNo            (MstCarSpecController.cs:1862)
        //  - CKD bat buoc FlagInvoiceFactory='1' (MstCarSpecController.cs:1876)
        if (!db.CarSpecs.Any())
        {
            db.CarSpecs.AddRange(
                new CarSpec
                {
                    SpecCode = "SPEC-CKD-001", RootSpec = "RS-A", ModelCode = "MD-A",
                    SpecDescription = "Model A 1.5 MT - lap rap trong nuoc",
                    StdOptCode = "STD-1", OCNCode = "OCN-1", GradeCode = "MT",
                    AssemblyStatus = "CKD", FlagInvoiceFactory = "1", FlagAmbulance = "0",
                    NumberOfSeats = 5, UnitPrice = 499000000m,
                    FlagDepositPmt = "0", FlagActive = "1"
                },
                new CarSpec
                {
                    SpecCode = "SPEC-CBU-002", RootSpec = "RS-B", ModelCode = "MD-B",
                    SpecDescription = "Model B 2.0 AT - nhap nguyen chiec",
                    StdOptCode = "STD-2", OCNCode = "OCN-2", GradeCode = "AT",
                    AssemblyStatus = "CBU", OriginNo = "TH", FlagInvoiceFactory = "0", FlagAmbulance = "0",
                    NumberOfSeats = 7, UnitPrice = 899000000m,
                    FlagDepositPmt = "1", FlagActive = "1"
                },
                new CarSpec
                {
                    SpecCode = "SPEC-CKD-003", RootSpec = "RS-A", ModelCode = "MD-A",
                    SpecDescription = "Model A cuu thuong",
                    StdOptCode = "STD-1", OCNCode = "OCN-3", GradeCode = "AMB",
                    AssemblyStatus = "CKD", FlagInvoiceFactory = "1", FlagAmbulance = "1",
                    NumberOfSeats = 3, UnitPrice = 650000000m,
                    FlagDepositPmt = "0", FlagActive = "1"
                }
            );
            await db.SaveChangesAsync();
        }

        // ===== Seed Thanh toan - port 1:1 Pmt_Payment + Pmt_PaymentDetail =====
        // Trang thai lay tu PmtStatus (Const.Main.BE.cs:794-804).
        // Phieu 1: TTC     (detail.GuaranteeNo = null)      - PmtPaymentController.cs:12157
        // Phieu 2: TTBL/LC (detail.GuaranteeNo co gia tri)  - PmtPaymentController.cs:12161
        if (!db.Payments.Any())
        {
            db.Payments.AddRange(
                new Payment
                {
                    PaymentNo = "DL001-202609-0001", DealerCode = "DL001", PaymentType = "TTC",
                    TotalAmount = 499000000m, BankPaymentNo = "UNC0001",
                    BankCodeSend = "MB", BankAccountSend = "0011000123456",
                    BankCodeReceive = "VCB", BankAccountReceive = "0021000987654",
                    Funds = "TUCO", Remark = "Thanh toan tien xe SPEC-CKD-001",
                    PaymentStatus = PmtStatus.Pending, CreatedDate = DateTime.Now.AddDays(-3),
                    Details =
                    [
                        new PaymentDetail
                        {
                            CarId = "CAR-0001", VIN = "RLHJF2810KY000001",
                            ModelCode = "MD-A", SpecCode = "SPEC-CKD-001",
                            ColorCode = "WHT", VINColorCode = "WHT", SOCode = "SO-0001",
                            DlrCtrNo = "PLHD-0001", GuaranteeNo = null,   // TTC = khong co so bao lanh
                            Amount = 499000000m, UnitPriceActual = 499000000m,
                            PMT_Percent_Current = 100m, PMT_Percent_Accum = 100m,
                            FlagisHTC = "1", FlagHasDeliveryOrder = "0"
                        }
                    ]
                },
                new Payment
                {
                    PaymentNo = "DL002-202609-0001", DealerCode = "DL002", PaymentType = "TTBL",
                    GuaranteeType = "BL", TotalAmount = 899000000m, BankPaymentNo = "UNC0002",
                    BankCodeSend = "TCB", BankAccountSend = "1901000111222",
                    BankCodeReceive = "VCB", BankAccountReceive = "0021000987654",
                    Funds = "VAY", BankLending = "TCB", InterestRate = 8.5m, LoanPeriod = 6,
                    Remark = "Thanh toan bao lanh xe SPEC-CBU-002",
                    PaymentStatus = PmtStatus.Approve, ApprovedBy = "admin@minidms.com",
                    ApprovedDate = DateTime.Now.AddDays(-1), CreatedDate = DateTime.Now.AddDays(-2),
                    Details =
                    [
                        new PaymentDetail
                        {
                            CarId = "CAR-0002", VIN = "RLHJF2810KY000002",
                            ModelCode = "MD-B", SpecCode = "SPEC-CBU-002",
                            ColorCode = "BLK", VINColorCode = "BLK", SOCode = "SO-0002",
                            DlrCtrNo = "PLHD-0002",
                            GuaranteeNo = "BL-0002", BankGuaranteeNo = "TCB-BL-0002", GuaranteeType = "BL",
                            Amount = 899000000m, UnitPriceActual = 899000000m,
                            PMT_Percent_Current = 100m, PMT_Percent_Accum = 100m,
                            InterestRate = 8.5m, LoanPeriod = 6,
                            FlagisHTC = "1", FlagHasDeliveryOrder = "1"   // co LXX => canh bao khi huy xac nhan
                        }
                    ]
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private async Task EnsureUser(string email, string password, string role)
    {
        var user = await userMgr.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, OrgId = TenantContext.DefaultOrgId };
            await userMgr.CreateAsync(user, password);
        }
        if (!await userMgr.IsInRoleAsync(user, role))
            await userMgr.AddToRoleAsync(user, role);
    }

    /// <summary>
    /// Tạo các bảng MỚI còn thiếu trên DB đã tồn tại.
    /// LÝ DO (bẫy runtime, build KHÔNG bắt được): <c>EnsureCreatedAsync</c> chỉ tạo schema khi database
    /// CHƯA tồn tại; với DB đã có (minidms.db local, hoặc Postgres trên Render) nó là NO-OP, nên 4 bảng
    /// port mới (Dealers/CarSpecs/Payments/PaymentDetails) sẽ KHÔNG được tạo và mọi trang mới đổ lỗi
    /// "no such table" lúc chạy — trong khi `dotnet build` vẫn 0 Errors.
    /// Cách làm: lấy đúng script tạo bảng do EF sinh RA TỪ MODEL (không gõ tay DDL, tránh lệch kiểu cột),
    /// đổi mọi CREATE thành CREATE IF NOT EXISTS rồi chạy từng câu — idempotent, chạy lại vô hại.
    /// </summary>
    private async Task CreateMissingTablesAsync()
    {
        string script;
        try { script = db.Database.GenerateCreateScript(); }
        catch { return; }

        script = script
            .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ")
            .Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ")
            .Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ")
            .Replace("CREATE SCHEMA ", "CREATE SCHEMA IF NOT EXISTS ");

        foreach (var stmt in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var sql = stmt.Trim();
            if (sql.Length == 0 || !sql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase)) continue;
            // Bảng/chỉ mục đã có sẽ bị IF NOT EXISTS bỏ qua; câu nào vẫn lỗi thì không được làm chết app khởi động.
            try { await db.Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    /// <summary>DB Postgres cloud cũ: tạo Orgs + thêm cột OrgId (bảng nghiệp vụ + AspNetUsers) nếu thiếu. Idempotent.</summary>
    private async Task MigratePostgresAsync()
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        // 4 bang cuoi la port 1:1 tu 2023.3.DMS.Sales (Mst_Dealer / Mst_CarSpec / Pmt_Payment / Pmt_PaymentDetail).
        var tables = new[] { "ProductCategories", "Products", "StockTransactions", "Customers", "Orders", "OrderLines",
                             "Dealers", "CarSpecs", "Payments", "PaymentDetails" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minidms.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minidms.\"Orgs\" (\"ApiKey\")",
            $"ALTER TABLE minidms.\"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
        };
        foreach (var t in tables)
            sql.Add($"ALTER TABLE minidms.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
