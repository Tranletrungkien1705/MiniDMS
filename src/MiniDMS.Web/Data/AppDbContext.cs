using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Models.Entities;

namespace MiniDMS.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // Giữ REFERENCE tenant (không chốt giá trị ở ctor): AppDbContext có thể bị dựng trong UseAuthentication
    // (Identity validate security-stamp) TRƯỚC khi middleware set OrgId. Đọc _tenant.OrgId lazy → EF
    // re-evaluate query filter lúc chạy query (trong controller, sau middleware) nên luôn đúng tenant.
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
        => _tenant = tenant;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    // --- Port 1:1 từ 2023.3.DMS.Sales (bảng nguồn ghi trong chú thích từng entity) ---
    public DbSet<Dealer> Dealers => Set<Dealer>();                       // Mst_Dealer
    public DbSet<CarSpec> CarSpecs => Set<CarSpec>();                    // Mst_CarSpec
    public DbSet<Payment> Payments => Set<Payment>();                    // Pmt_Payment
    public DbSet<PaymentDetail> PaymentDetails => Set<PaymentDetail>();  // Pmt_PaymentDetail

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minidms");
        base.OnModelCreating(b);

        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<ProductCategory>().HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        b.Entity<StockTransaction>().HasQueryFilter(x => x.OrgId == _tenant.OrgId);

        b.Entity<Product>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.SKU }).IsUnique();   // SKU duy nhất trong 1 tổ chức
            e.Property(x => x.CostPrice).HasPrecision(18, 2);
            e.Property(x => x.SalePrice).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        b.Entity<Order>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.OrderNo }).IsUnique();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        b.Entity<OrderLine>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Property(x => x.DebtBalance).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        // ===== Mst_Dealer — MstDealerController.cs:1695 (Create) / :2105 (Update) =====
        b.Entity<Dealer>(e =>
        {
            // DealerCode là khoá nghiệp vụ của Mst_Dealer (nguồn: MstDealerController.cs:859 GetByDealerCode,
            // :2510 Delete đều định danh bằng DealerCode) → duy nhất trong 1 tổ chức.
            e.HasIndex(x => new { x.OrgId, x.DealerCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        // ===== Mst_CarSpec — MstCarSpecController.cs:1577 (Create) / :1969 (Update) =====
        b.Entity<CarSpec>(e =>
        {
            // SpecCode là khoá nghiệp vụ (nguồn: MstCarSpecController.cs:617 GetByCode, :2334 Delete theo SpecCode).
            e.HasIndex(x => new { x.OrgId, x.SpecCode }).IsUnique();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        // ===== Pmt_Payment — PmtPaymentController.cs:815 (CreateDL) / :7483 (Search) =====
        b.Entity<Payment>(e =>
        {
            // PaymentNo là khoá nghiệp vụ (nguồn: PmtPaymentController.cs:7894 GetByPaymentNo).
            e.HasIndex(x => new { x.OrgId, x.PaymentNo }).IsUnique();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestRate).HasPrecision(18, 4);   // lãi suất %/năm — nguồn cho nhập số thập phân
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });

        b.Entity<PaymentDetail>(e =>
        {
            e.HasOne(x => x.Payment).WithMany(x => x.Details)
             .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PMPDAmountTotal).HasPrecision(18, 2);
            e.Property(x => x.UnitPriceActual).HasPrecision(18, 2);
            e.Property(x => x.PMT_Percent_Accum).HasPrecision(18, 4);
            e.Property(x => x.PMT_Percent_Current).HasPrecision(18, 4);
            e.Property(x => x.InterestRate).HasPrecision(18, 4);
            e.HasQueryFilter(x => x.OrgId == _tenant.OrgId);
        });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }

    private void StampOrg()
    {
        foreach (var entry in ChangeTracker.Entries<IOrgOwned>())
            if (entry.State == EntityState.Added && entry.Entity.OrgId == Guid.Empty)
                entry.Entity.OrgId = _tenant.OrgId;
    }
}
