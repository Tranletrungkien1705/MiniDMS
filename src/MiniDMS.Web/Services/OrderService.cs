using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync(DateTime? from, DateTime? to, OrderStatus? status);
    Task<Order?> GetByIdAsync(int id);
    Task<int> CreateAsync(Order order, List<OrderLine> lines);
    Task ConfirmAsync(int id, string user);
    Task MarkDeliveredAsync(int id, string user);
    Task RecordPaymentAsync(int id, decimal amount, string user);
    Task CancelAsync(int id, string user);
    Task<List<Customer>> GetCustomersAsync(string? search = null);
    Task<int> CreateCustomerAsync(Customer customer);
    Task UpdateEInvoiceAsync(int orderId, Guid eid, string series, long? number, string status, string? code);
    Task UpdateAccountingAsync(int orderId, string entryNo);
}

public class OrderService(AppDbContext db, IStockService stock) : IOrderService
{
    public Task<List<Order>> GetAllAsync(DateTime? from, DateTime? to, OrderStatus? status)
    {
        var q = db.Orders.Include(o => o.Customer).AsQueryable();
        if (from.HasValue)   q = q.Where(o => o.OrderDate >= from.Value);
        if (to.HasValue)     q = q.Where(o => o.OrderDate <= to.Value);
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);
        return q.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    public Task<Order?> GetByIdAsync(int id) =>
        db.Orders.Include(o => o.Customer).Include(o => o.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<int> CreateAsync(Order order, List<OrderLine> lines)
    {
        order.OrderNo = $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}";
        order.TotalAmount = lines.Sum(l => l.LineTotal);
        order.Lines = lines;
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public async Task ConfirmAsync(int id, string user)
    {
        var o = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException();
        if (o.Status != OrderStatus.Draft) throw new InvalidOperationException("Chỉ xác nhận đơn ở trạng thái Nháp");
        o.Status = OrderStatus.Confirmed;
        // Deduct stock for each line
        foreach (var l in o.Lines)
            await stock.StockOutAsync(l.ProductId, l.Quantity, $"Đơn hàng {o.OrderNo}", o.OrderNo, user);
        await db.SaveChangesAsync();
    }

    public async Task MarkDeliveredAsync(int id, string user)
    {
        var o = await db.Orders.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (o.Status != OrderStatus.Confirmed) throw new InvalidOperationException("Chỉ giao đơn đã xác nhận");
        o.Status = OrderStatus.Delivered;
        await db.SaveChangesAsync();
    }

    public async Task RecordPaymentAsync(int id, decimal amount, string user)
    {
        var o = await db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException();
        o.PaidAmount = Math.Min(o.TotalAmount, o.PaidAmount + amount);
        o.PaymentStatus = o.PaidAmount >= o.TotalAmount ? PaymentStatus.Paid : PaymentStatus.PartialPaid;
        o.Customer.DebtBalance = Math.Max(0, o.Customer.DebtBalance - amount);
        await db.SaveChangesAsync();
    }

    public async Task CancelAsync(int id, string user)
    {
        var o = await db.Orders.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (o.Status == OrderStatus.Delivered) throw new InvalidOperationException("Không hủy đơn đã giao");
        o.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<Customer>> GetCustomersAsync(string? search = null)
    {
        var q = db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        return await q.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<int> CreateCustomerAsync(Customer customer)
    {
        customer.Code = $"KH{DateTime.Now:yyMMddHHmm}";
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    public async Task UpdateEInvoiceAsync(int orderId, Guid eid, string series, long? number, string status, string? code)
    {
        var o = await db.Orders.FirstOrDefaultAsync(x => x.Id == orderId) ?? throw new KeyNotFoundException();
        o.EInvoiceId = eid;
        o.EInvoiceSeries = series;
        o.EInvoiceNumber = number;
        o.EInvoiceStatus = status;
        o.EInvoiceCode = code;
        o.EInvoiceIssuedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task UpdateAccountingAsync(int orderId, string entryNo)
    {
        var o = await db.Orders.FirstOrDefaultAsync(x => x.Id == orderId) ?? throw new KeyNotFoundException();
        o.AccountingEntryNo = entryNo;
        o.AccountingSyncedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }
}
