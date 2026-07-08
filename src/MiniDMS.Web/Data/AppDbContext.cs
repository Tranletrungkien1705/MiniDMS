using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Models.Entities;

namespace MiniDMS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext(options)
{
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Product>(e =>
        {
            e.HasIndex(x => x.SKU).IsUnique();
            e.Property(x => x.CostPrice).HasPrecision(18, 2);
            e.Property(x => x.SalePrice).HasPrecision(18, 2);
        });

        b.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderNo).IsUnique();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
        });

        b.Entity<OrderLine>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.DebtBalance).HasPrecision(18, 2);
        });
    }
}
