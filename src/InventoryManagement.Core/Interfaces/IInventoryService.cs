using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Models;

namespace InventoryManagement.Core.Interfaces;

/// <summary>
/// Service interface for inventory management operations including stock tracking
/// and real-time synchronization across multiple warehouse locations.
/// </summary>
public interface IInventoryService
{
    /// <summary>Gets the current stock level for a product at a specific warehouse.</summary>
    Task<InventoryItem?> GetStockAsync(int productId, int warehouseId);

    /// <summary>Gets all inventory items for a specific warehouse.</summary>
    Task<List<InventoryItem>> GetWarehouseInventoryAsync(int warehouseId);

    /// <summary>Gets all inventory items for a specific product across all warehouses.</summary>
    Task<List<InventoryItem>> GetProductInventoryAsync(int productId);

    /// <summary>Gets all inventory items system-wide with product and warehouse details.</summary>
    Task<List<InventoryItem>> GetAllInventoryAsync();

    /// <summary>Receives stock from a supplier into a warehouse.</summary>
    Task<StockTransaction> ReceiveStockAsync(int productId, int warehouseId, int quantity, string reference = "", string notes = "");

    /// <summary>Ships stock out of a warehouse.</summary>
    Task<StockTransaction> ShipStockAsync(int productId, int warehouseId, int quantity, string reference = "", string notes = "");

    /// <summary>Transfers stock between two warehouses.</summary>
    Task<StockTransaction> TransferStockAsync(int productId, int sourceWarehouseId, int destWarehouseId, int quantity, string notes = "");

    /// <summary>Adjusts stock count (for corrections, damage write-offs, etc.).</summary>
    Task<StockTransaction> AdjustStockAsync(int productId, int warehouseId, int newQuantity, string notes = "");

    /// <summary>Gets the stock health level for a product at a warehouse.</summary>
    StockLevel GetStockLevel(int currentQuantity, int reorderPoint);

    /// <summary>Gets total quantity of a product across all warehouses.</summary>
    Task<int> GetTotalStockAsync(int productId);

    /// <summary>Gets recent transactions, optionally filtered.</summary>
    Task<List<StockTransaction>> GetRecentTransactionsAsync(int count = 20, int? productId = null, int? warehouseId = null);
}
