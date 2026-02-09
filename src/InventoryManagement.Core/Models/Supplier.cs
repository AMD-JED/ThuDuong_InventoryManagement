using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Models;

/// <summary>
/// Represents a supplier that provides products to the inventory system.
/// </summary>
public class Supplier
{
    /// <summary>Unique identifier for the supplier.</summary>
    public int Id { get; set; }

    /// <summary>Company name of the supplier.</summary>
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Primary contact person at the supplier.</summary>
    [MaxLength(200)]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>Contact email address.</summary>
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contact phone number.</summary>
    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Supplier's business address.</summary>
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    /// <summary>Average lead time in days for order fulfillment.</summary>
    public int LeadTimeDays { get; set; } = 7;

    /// <summary>Performance rating (1-5 scale).</summary>
    [Range(1, 5)]
    public int Rating { get; set; } = 3;

    /// <summary>Whether this supplier is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Date the supplier was added to the system.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>Products supplied by this supplier.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
