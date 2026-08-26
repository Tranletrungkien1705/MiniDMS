namespace MiniDMS.Models.Entities;

public enum OrderStatus { Draft, Confirmed, Delivered, Cancelled }
public enum PaymentStatus { Unpaid, PartialPaid, Paid }

public class Customer
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DebtBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<Order> Orders { get; set; } = [];
}

public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = "";
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount => TotalAmount - PaidAmount;
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = "";

    // ── Hóa đơn điện tử (tích hợp QinvoiceLite) ──────────────────────
    public Guid? EInvoiceId { get; set; }          // Id hóa đơn bên QinvoiceLite
    public string? EInvoiceSeries { get; set; }    // ký hiệu
    public long? EInvoiceNumber { get; set; }      // số hóa đơn
    public string? EInvoiceCode { get; set; }      // mã CQT cấp
    public string? EInvoiceStatus { get; set; }    // Draft/Authorized/...
    public DateTime? EInvoiceIssuedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
