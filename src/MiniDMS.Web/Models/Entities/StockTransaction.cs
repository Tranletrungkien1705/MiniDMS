namespace MiniDMS.Models.Entities;

public enum TransactionType { In, Out, Adjust }

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? RefNo { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Product Product { get; set; } = null!;
}

// View-only: current stock per product (computed from transactions)
public class StockBalance
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public int TotalIn { get; set; }
    public int TotalOut { get; set; }
    public int Balance => TotalIn - TotalOut;
    public int LowStockThreshold { get; set; }
    public bool IsLowStock => Balance <= LowStockThreshold;
}
