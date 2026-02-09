using InventoryManagement.Core.Models;

namespace InventoryManagement.Core.Interfaces;

/// <summary>
/// Repository interface for Product-specific data access operations.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>Gets a product by its SKU code.</summary>
    Task<Product?> GetBySKUAsync(string sku);

    /// <summary>Gets all products in a specific category.</summary>
    Task<List<Product>> GetByCategoryAsync(string category);

    /// <summary>Searches products by name or SKU (case-insensitive).</summary>
    Task<List<Product>> SearchAsync(string searchTerm);

    /// <summary>Gets all distinct product categories.</summary>
    Task<List<string>> GetCategoriesAsync();

    /// <summary>Gets a product with its supplier information.</summary>
    Task<Product?> GetWithSupplierAsync(int id);
}
