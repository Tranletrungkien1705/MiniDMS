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
