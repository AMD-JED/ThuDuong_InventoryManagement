using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents a single stock movement event in the inventory system.
/// Every stock change is logged as an immutable transaction for full audit trail.
/// </summary>
public class StockTransaction
{
    /// <summary>Unique identifier for the transaction.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the product involved in this transaction.</summary>
    public int ProductId { get; set; }

    /// <summary>Foreign key to the warehouse where the transaction occurred.</summary>
    public int WarehouseId { get; set; }

    /// <summary>Type of stock movement (Received, Shipped, Transferred, Adjusted, Returned).</summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Quantity changed. Positive for stock additions, negative for removals.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>Optional reference number (PO number, shipment ID, etc.).</summary>
    [MaxLength(100)]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>Optional notes about the transaction.</summary>
    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    /// <summary>Timestamp when the transaction was recorded.</summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    /// <summary>For transfers: destination warehouse ID.</summary>
    public int? DestinationWarehouseId { get; set; }

    // Navigation properties
    /// <summary>The product involved in this transaction.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>The warehouse where the transaction originated.</summary>
    public Warehouse Warehouse { get; set; } = null!;
}
