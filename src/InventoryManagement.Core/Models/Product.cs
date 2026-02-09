using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents a product tracked in the inventory system.
/// </summary>
public class Product
{
    /// <summary>Unique identifier for the product.</summary>
    public int Id { get; set; }

    /// <summary>Stock Keeping Unit — unique alphanumeric code for the product.</summary>
    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    /// <summary>Display name of the product.</summary>
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed description of the product.</summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Product category for grouping and filtering.</summary>
    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price in USD.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Minimum stock threshold that triggers a reorder alert.</summary>
    public int ReorderPoint { get; set; } = 10;

    /// <summary>Optimal quantity to reorder when stock falls below reorder point.</summary>
    public int ReorderQuantity { get; set; } = 50;

    /// <summary>Date the product was added to the system.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether this product is actively tracked.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>Supplier providing this product.</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Inventory records across all warehouses.</summary>
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

    /// <summary>Transaction history for this product.</summary>
    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}
