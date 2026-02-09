using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents a physical warehouse location in the multi-site inventory system.
/// </summary>
public class Warehouse
{
    /// <summary>Unique identifier for the warehouse.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the warehouse.</summary>
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Physical address of the warehouse.</summary>
    [Required, MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    /// <summary>City where the warehouse is located.</summary>
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>State or region.</summary>
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    /// <summary>Maximum number of inventory units the warehouse can hold.</summary>
    public int Capacity { get; set; }

    /// <summary>Whether this warehouse is currently operational.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Date the warehouse was added to the system.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>Inventory items stored at this warehouse.</summary>
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
