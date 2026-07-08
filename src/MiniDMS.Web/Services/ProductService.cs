using Microsoft.EntityFrameworkCore;
using MiniDMS.Data;
using MiniDMS.Models.Entities;

namespace MiniDMS.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync(bool activeOnly = true);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySkuAsync(string sku);
    Task<List<ProductCategory>> GetCategoriesAsync();
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task<bool> DeactivateAsync(int id);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
}

public class ProductService(AppDbContext db) : IProductService
{
    public async Task<List<Product>> GetAllAsync(bool activeOnly = true)
    {
        var q = db.Products.Include(p => p.Category).AsQueryable();
        if (activeOnly) q = q.Where(p => p.IsActive);
        return await q.OrderBy(p => p.Category.Name).ThenBy(p => p.SKU).ToListAsync();
    }

    public Task<Product?> GetByIdAsync(int id) =>
        db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetBySkuAsync(string sku) =>
        db.Products.FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);

    public Task<List<ProductCategory>> GetCategoriesAsync() =>
        db.ProductCategories.OrderBy(c => c.Name).ToListAsync();

    public async Task<int> CreateAsync(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateAsync(Product product)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p == null) return false;
        p.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> SkuExistsAsync(string sku, int? excludeId = null) =>
        db.Products.AnyAsync(p => p.SKU == sku && (!excludeId.HasValue || p.Id != excludeId.Value));
}
