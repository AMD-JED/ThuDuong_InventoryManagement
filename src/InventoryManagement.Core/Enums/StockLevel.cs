namespace InventoryManagement.Core.Enums;

/// <summary>
/// Represents the stock health level for color-coded display.
/// </summary>
public enum StockLevel
{
    /// <summary>Stock is at or above healthy threshold</summary>
    Healthy,

    /// <summary>Stock is below optimal but above reorder point</summary>
    Low,

    /// <summary>Stock is at or below reorder point — immediate action needed</summary>
    Critical
}
