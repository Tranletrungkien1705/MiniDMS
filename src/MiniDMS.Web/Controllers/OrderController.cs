using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniDMS.Models.Entities;
using MiniDMS.Services;

namespace MiniDMS.Controllers;

[Authorize]
public class OrderController(IOrderService orders, IProductService products) : Controller
{
    private string User_ => User.Identity?.Name ?? "";

    // GET /Order
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, OrderStatus? status)
    {
        ViewBag.From = from; ViewBag.To = to; ViewBag.Status = status;
        return View(await orders.GetAllAsync(from, to, status));
    }

    // GET /Order/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var o = await orders.GetByIdAsync(id);
        if (o == null) return NotFound();
        return View(o);
    }

    // GET /Order/Create
    [Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await orders.GetCustomersAsync();
        ViewBag.Products = await products.GetAllAsync(activeOnly: true);
        return View();
    }

    // POST /Order/Create
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> Create(int customerId, string? note, int[] productId, int[] quantity)
    {
        var prods = await products.GetAllAsync(activeOnly: true);

        var lines = new List<OrderLine>();
        for (int i = 0; i < (productId?.Length ?? 0); i++)
        {
            if (quantity == null || i >= quantity.Length || quantity[i] <= 0) continue;
            var p = prods.FirstOrDefault(x => x.Id == productId![i]);
            if (p == null) continue;
            lines.Add(new OrderLine { ProductId = p.Id, Quantity = quantity[i], UnitPrice = p.SalePrice });
        }

        if (customerId == 0) ModelState.AddModelError("", "Vui lòng chọn khách hàng.");
        if (lines.Count == 0) ModelState.AddModelError("", "Đơn phải có ít nhất 1 dòng sản phẩm với số lượng > 0.");

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await orders.GetCustomersAsync();
            ViewBag.Products = prods;
            return View();
        }

        var order = new Order { CustomerId = customerId, Note = note, CreatedBy = User_ };
        var id = await orders.CreateAsync(order, lines);
        TempData["Success"] = "Đã tạo đơn hàng (trạng thái Nháp). Bấm \"Xác nhận\" để trừ kho.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Order/Confirm/5 — trừ kho
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> Confirm(int id)
    {
        try { await orders.ConfirmAsync(id, User_); TempData["Success"] = "Đã xác nhận đơn và trừ kho."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Order/Deliver/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> Deliver(int id)
    {
        try { await orders.MarkDeliveredAsync(id, User_); TempData["Success"] = "Đã đánh dấu đã giao hàng."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Order/Pay/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales,Accounting")]
    public async Task<IActionResult> Pay(int id, decimal amount)
    {
        try
        {
            if (amount <= 0) throw new InvalidOperationException("Số tiền phải lớn hơn 0.");
            await orders.RecordPaymentAsync(id, amount, User_);
            TempData["Success"] = "Đã ghi nhận thanh toán.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Order/Cancel/5
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> Cancel(int id)
    {
        try { await orders.CancelAsync(id, User_); TempData["Success"] = "Đã hủy đơn hàng."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Order/Customers
    public async Task<IActionResult> Customers(string? search)
    {
        ViewBag.Search = search;
        return View(await orders.GetCustomersAsync(search));
    }

    // POST /Order/CreateCustomer
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> CreateCustomer(string name, string? phone, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên khách hàng là bắt buộc.";
            return RedirectToAction(nameof(Customers));
        }
        await orders.CreateCustomerAsync(new Customer { Name = name.Trim(), Phone = phone, Email = email, Address = address });
        TempData["Success"] = "Đã thêm khách hàng.";
        return RedirectToAction(nameof(Customers));
    }
}
