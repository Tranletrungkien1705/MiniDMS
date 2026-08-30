using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;
using MiniDMS.Services;
using Xunit;

namespace MiniDMS.Web.Tests;

/// <summary>Test DMS: đơn hàng (tổng tiền + vòng đời Draft→Confirmed→Delivered), tồn kho (nhập/xuất + guard), thanh toán.</summary>
public class DmsServiceTests
{
    private static (AppDbContext db, IOrderService orders, IStockService stock, SqliteConnection conn) NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo", ApiKey = "demo" });
        db.SaveChanges();
        var stock = new StockService(db, new ExcelService());
        return (db, new OrderService(db, stock, new StubWmsClient(), new StubReconClient()), stock, conn);
    }

    // Tích hợp fleet khi giao/xác nhận đơn là best-effort → test không chạm mạng.
    private sealed class StubWmsClient : IWmsClient
    {
        public string BaseUrl => "stub";
        public Task<WmsIssueResult?> IssueOrderAsync(Order order, System.Threading.CancellationToken ct = default)
            => Task.FromResult<WmsIssueResult?>(null);
    }
    private sealed class StubReconClient : IReconcileClient
    {
        public string BaseUrl => "stub";
        public Task PostDebtAsync(Order order, Customer? customer, System.Threading.CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private static async Task<(int productId, int customerId)> Seed(AppDbContext db, IStockService stock)
    {
        var cat = new ProductCategory { Name = "Nhóm A", OrgId = TenantContext.DefaultOrgId };
        db.ProductCategories.Add(cat); await db.SaveChangesAsync();
        var p = new Product { SKU = "SP1", Name = "Sản phẩm 1", CategoryId = cat.Id, SalePrice = 100_000, CostPrice = 60_000, OrgId = TenantContext.DefaultOrgId };
        db.Products.Add(p); await db.SaveChangesAsync();
        var c = new Customer { Code = "KH1", Name = "Khách 1", OrgId = TenantContext.DefaultOrgId };
        db.Customers.Add(c); await db.SaveChangesAsync();
        await stock.StockInAsync(p.Id, 100, "đầu kỳ", null, "seed");
        return (p.Id, c.Id);
    }

    [Fact]
    public async Task CreateOrder_ComputesTotal_AndDraft()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, cid) = await Seed(db, stock);
            var id = await orders.CreateAsync(new Order { CustomerId = cid }, new() { new OrderLine { ProductId = pid, Quantity = 3, UnitPrice = 100_000 } });
            var o = await orders.GetByIdAsync(id);
            Assert.Equal(OrderStatus.Draft, o!.Status);
            Assert.Equal(300_000, o.TotalAmount);
            Assert.StartsWith("ORD-", o.OrderNo);
        }
    }

    [Fact]
    public async Task Confirm_FromDraft_SetsConfirmed()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, cid) = await Seed(db, stock);
            var id = await orders.CreateAsync(new Order { CustomerId = cid }, new() { new OrderLine { ProductId = pid, Quantity = 2, UnitPrice = 100_000 } });
            await orders.ConfirmAsync(id, "test");
            Assert.Equal(OrderStatus.Confirmed, (await orders.GetByIdAsync(id))!.Status);
        }
    }

    [Fact]
    public async Task Confirm_NonDraft_Throws()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, cid) = await Seed(db, stock);
            var id = await orders.CreateAsync(new Order { CustomerId = cid }, new() { new OrderLine { ProductId = pid, Quantity = 1, UnitPrice = 100_000 } });
            await orders.ConfirmAsync(id, "t");
            await Assert.ThrowsAsync<InvalidOperationException>(() => orders.ConfirmAsync(id, "t"));
        }
    }

    [Fact]
    public async Task StockIn_IncreasesBalance()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, _) = await Seed(db, stock);   // đã nhập 100
            await stock.StockInAsync(pid, 50, "bổ sung", null, "t");
            var bal = await stock.GetBalanceAsync(pid);
            Assert.Equal(150, bal!.Balance);
        }
    }

    [Fact]
    public async Task StockOut_OverBalance_Throws()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, _) = await Seed(db, stock);   // tồn 100
            await Assert.ThrowsAnyAsync<Exception>(() => stock.StockOutAsync(pid, 200, "quá tồn", null, "t"));
        }
    }

    [Fact]
    public async Task RecordPayment_UpdatesPaymentStatus()
    {
        var (db, orders, stock, conn) = NewSvc(); using (conn)
        {
            var (pid, cid) = await Seed(db, stock);
            var id = await orders.CreateAsync(new Order { CustomerId = cid }, new() { new OrderLine { ProductId = pid, Quantity = 2, UnitPrice = 100_000 } });
            await orders.ConfirmAsync(id, "t");
            await orders.RecordPaymentAsync(id, 200_000, "t");   // trả đủ
            Assert.Equal(PaymentStatus.Paid, (await orders.GetByIdAsync(id))!.PaymentStatus);
        }
    }
}
