namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents the stock level of a specific product at a specific warehouse.
/// This is the core junction entity enabling multi-location inventory tracking.
/// </summary>
public class InventoryItem
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the product.</summary>
    public int ProductId { get; set; }

    /// <summary>Foreign key to the warehouse.</summary>
    public int WarehouseId { get; set; }

    /// <summary>Current quantity on hand at this warehouse.</summary>
    public int Quantity { get; set; }

    /// <summary>Last time this inventory record was updated.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>The product this inventory record tracks.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>The warehouse where this stock is located.</summary>
    public Warehouse Warehouse { get; set; } = null!;
}
