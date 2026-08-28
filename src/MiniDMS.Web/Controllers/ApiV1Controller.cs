using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Data;
using MiniDMS.Models.Entities;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

/// <summary>
/// API JSON cho SPA React (behind Identity cookie). DTO phẳng. Dashboard cache Redis 15s theo tenant (X-Cache).
/// DMS đại lý: đơn hàng (Draft→Confirmed→Delivered→Cancelled + thanh toán), sản phẩm, kho (nhập/xuất), khách hàng.
/// Tích hợp: xuất HĐĐT (QinvoiceLite), đồng bộ kế toán (MiniAccounting) qua OrderController giữ nguyên.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public class ApiV1Controller(IOrderService orders, IProductService products, IStockService stock, IReportService report, ICache cache, ITenantContext tenant) : ControllerBase
{
    private string CurrentUser => User.Identity?.Name ?? "api";

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"dms:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<DashDto>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await report.GetDashboardAsync();
        var dto = new DashDto(d.TotalProducts, d.LowStockCount, d.TodayRevenue, d.PendingOrders,
            d.MonthlySales.Select(x => new MonthDto(x.Label, x.Value)).ToList());
        await cache.SetAsync(key, dto, TimeSpan.FromSeconds(15));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(dto);
    }

    // ── Orders ──
    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] OrderStatus? status)
        => Ok((await orders.GetAllAsync(from, to, status)).Select(o => new
        {
            o.Id, o.OrderNo, customer = o.Customer?.Name, o.OrderDate, o.TotalAmount,
            status = (int)o.Status, statusText = o.Status.ToString(), paymentStatus = (int)o.PaymentStatus, paymentText = o.PaymentStatus.ToString(),
            eInvoice = o.EInvoiceCode, lines = o.Lines?.Count ?? 0
        }));

    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Order(int id)
    {
        var o = await orders.GetByIdAsync(id);
        if (o == null) return NotFound(new { error = "Không tìm thấy đơn." });
        return Ok(new
        {
            o.Id, o.OrderNo, customerId = o.CustomerId, customer = o.Customer?.Name, o.OrderDate, o.TotalAmount, o.PaidAmount,
            status = (int)o.Status, statusText = o.Status.ToString(), paymentStatus = (int)o.PaymentStatus, paymentText = o.PaymentStatus.ToString(),
            eInvoiceCode = o.EInvoiceCode, accountingEntry = o.AccountingEntryNo,
            lines = o.Lines?.Select(l => new { l.ProductId, product = l.Product?.Name, l.Quantity, l.UnitPrice, lineTotal = l.Quantity * l.UnitPrice }) ?? Enumerable.Empty<object>()
        });
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderReq r)
    {
        if (r.CustomerId <= 0 || r.Lines == null || r.Lines.Count == 0)
            return BadRequest(new { error = "Cần khách hàng và ít nhất 1 dòng hàng." });
        var order = new Order { CustomerId = r.CustomerId, OrderDate = r.OrderDate == default ? DateTime.Now : r.OrderDate };
        var lines = r.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).Select(l => new OrderLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList();
        var id = await orders.CreateAsync(order, lines);
        return Ok(new { id });
    }

    [HttpPost("orders/{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id) { try { await orders.ConfirmAsync(id, CurrentUser); return Ok(new { ok = true }); } catch (Exception e) { return BadRequest(new { error = e.Message }); } }

    [HttpPost("orders/{id:int}/deliver")]
    public async Task<IActionResult> Deliver(int id) { try { await orders.MarkDeliveredAsync(id, CurrentUser); return Ok(new { ok = true }); } catch (Exception e) { return BadRequest(new { error = e.Message }); } }

    [HttpPost("orders/{id:int}/payment")]
    public async Task<IActionResult> Payment(int id, [FromBody] PaymentReq r) { try { await orders.RecordPaymentAsync(id, r.Amount, CurrentUser); return Ok(new { ok = true }); } catch (Exception e) { return BadRequest(new { error = e.Message }); } }

    [HttpPost("orders/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id) { try { await orders.CancelAsync(id, CurrentUser); return Ok(new { ok = true }); } catch (Exception e) { return BadRequest(new { error = e.Message }); } }

    // ── Products ──
    [HttpGet("products")]
    public async Task<IActionResult> Products()
        => Ok((await products.GetAllAsync(false)).Select(p => new { p.Id, p.SKU, p.Name, category = p.Category?.Name, p.SalePrice, p.CostPrice, p.LowStockThreshold, p.IsActive }));

    // ── Stock ──
    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] string? sku)
        => Ok((await stock.GetBalancesAsync(sku)).Select(b => new { b.ProductId, b.SKU, b.ProductName, quantity = b.Balance, minStock = b.LowStockThreshold, low = b.IsLowStock }));

    [HttpGet("stock/{productId:int}/history")]
    public async Task<IActionResult> StockHistory(int productId)
        => Ok((await stock.GetHistoryAsync(productId)).Select(t => new { type = t.Type.ToString(), t.Quantity, t.Note, t.RefNo, t.CreatedAt, t.CreatedBy }));

    [HttpPost("stock/in")]
    public async Task<IActionResult> StockIn([FromBody] StockReq r) { var id = await stock.StockInAsync(r.ProductId, r.Quantity, r.Note, r.RefNo, CurrentUser); return Ok(new { id }); }

    [HttpPost("stock/out")]
    public async Task<IActionResult> StockOut([FromBody] StockReq r) { try { var id = await stock.StockOutAsync(r.ProductId, r.Quantity, r.Note, r.RefNo, CurrentUser); return Ok(new { id }); } catch (Exception e) { return BadRequest(new { error = e.Message }); } }

    // ── Customers ──
    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] string? q)
        => Ok((await orders.GetCustomersAsync(q)).Select(c => new { c.Id, c.Code, c.Name, c.Phone, c.Address }));

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên khách." });
        var id = await orders.CreateCustomerAsync(new Customer { Name = r.Name.Trim(), Phone = r.Phone, Address = r.Address });
        return Ok(new { id });
    }
}

public record DashDto(int TotalProducts, int LowStockCount, decimal TodayRevenue, int PendingOrders, List<MonthDto> MonthlySales);
public record MonthDto(string Label, decimal Value);

public class OrderLineReq { public int ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public class OrderReq { public int CustomerId { get; set; } public DateTime OrderDate { get; set; } public List<OrderLineReq>? Lines { get; set; } }
public class PaymentReq { public decimal Amount { get; set; } }
public class StockReq { public int ProductId { get; set; } public int Quantity { get; set; } public string? Note { get; set; } public string? RefNo { get; set; } }
public class CustomerReq { public string Name { get; set; } = ""; public string? Phone { get; set; } public string? Address { get; set; } }
