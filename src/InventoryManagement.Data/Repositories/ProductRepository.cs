using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;

namespace InventoryManagement.Data.Repositories;

/// <summary>
/// Repository for Product-specific data access with optimized queries.
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Product?> GetBySKUAsync(string sku)
    {
        return await _dbSet
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.SKU == sku);
    }

    /// <inheritdoc/>
    public async Task<List<Product>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Include(p => p.Supplier)
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Include(p => p.Supplier)
            .Where(p => p.Name.ToLower().Contains(term) ||
                        p.SKU.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term) ||
                        p.Category.ToLower().Contains(term))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Product?> GetWithSupplierAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc/>
    public override async Task<List<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Supplier)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
