namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents an automated reorder alert generated when stock falls below the reorder point.
/// </summary>
public class ReorderAlert
{
    /// <summary>Unique identifier for the alert.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the product that needs reordering.</summary>
    public int ProductId { get; set; }

    /// <summary>Foreign key to the warehouse with low stock.</summary>
    public int WarehouseId { get; set; }

    /// <summary>Current stock level when the alert was triggered.</summary>
    public int CurrentStock { get; set; }

    /// <summary>The reorder point threshold that was breached.</summary>
    public int ReorderPoint { get; set; }

    /// <summary>Recommended quantity to order.</summary>
    public int RecommendedOrderQuantity { get; set; }

    /// <summary>Whether this alert has been acknowledged/resolved.</summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>Timestamp when the alert was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when the alert was resolved (if applicable).</summary>
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    /// <summary>The product that needs reordering.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>The warehouse with low stock.</summary>
    public Warehouse Warehouse { get; set; } = null!;
}
