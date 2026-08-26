namespace MiniDMS.Models.Entities;

public class Product : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SKU { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Unit { get; set; } = "cái";
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ProductCategory Category { get; set; } = null!;
    public ICollection<StockTransaction> Transactions { get; set; } = [];
}

public class ProductCategory : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
