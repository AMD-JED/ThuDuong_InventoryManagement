namespace InventoryManagement.Core.Enums;

/// <summary>
/// Represents the type of stock transaction performed in the inventory system.
/// </summary>
public enum TransactionType
{
    /// <summary>Stock received from supplier</summary>
    Received = 1,

    /// <summary>Stock shipped to customer</summary>
    Shipped = 2,

    /// <summary>Stock transferred between warehouses</summary>
    Transferred = 3,

    /// <summary>Manual stock adjustment (count correction, damage, etc.)</summary>
    Adjusted = 4,

    /// <summary>Stock returned from customer</summary>
    Returned = 5
}
