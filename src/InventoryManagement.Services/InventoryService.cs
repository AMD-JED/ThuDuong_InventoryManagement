using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;
using InventoryManagement.Data;

namespace InventoryManagement.Services;

/// <summary>
/// Core inventory service handling stock operations with real-time data
/// synchronization across multiple warehouse locations.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<InventoryItem?> GetStockAsync(int productId, int warehouseId)
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
    }

    /// <inheritdoc/>
    public async Task<List<InventoryItem>> GetWarehouseInventoryAsync(int warehouseId)
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.WarehouseId == warehouseId)
            .OrderBy(i => i.Product.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<InventoryItem>> GetProductInventoryAsync(int productId)
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Warehouse.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<InventoryItem>> GetAllInventoryAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .OrderBy(i => i.Product.Name)
            .ThenBy(i => i.Warehouse.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<StockTransaction> ReceiveStockAsync(int productId, int warehouseId, int quantity, string reference = "", string notes = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Receive quantity must be positive.", nameof(quantity));

        var item = await GetOrCreateInventoryItemAsync(productId, warehouseId);
        item.Quantity += quantity;
        item.LastUpdated = DateTime.UtcNow;

        var transaction = new StockTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = TransactionType.Received,
            Quantity = quantity,
            ReferenceNumber = reference,
            Notes = string.IsNullOrEmpty(notes) ? "Stock received from supplier" : notes,
            TransactionDate = DateTime.UtcNow
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<StockTransaction> ShipStockAsync(int productId, int warehouseId, int quantity, string reference = "", string notes = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Ship quantity must be positive.", nameof(quantity));

        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

        if (item == null || item.Quantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {item?.Quantity ?? 0}, Requested: {quantity}");

        item.Quantity -= quantity;
        item.LastUpdated = DateTime.UtcNow;

        var transaction = new StockTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = TransactionType.Shipped,
            Quantity = -quantity,
            ReferenceNumber = reference,
            Notes = string.IsNullOrEmpty(notes) ? "Stock shipped to customer" : notes,
            TransactionDate = DateTime.UtcNow
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<StockTransaction> TransferStockAsync(int productId, int sourceWarehouseId, int destWarehouseId, int quantity, string notes = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Transfer quantity must be positive.", nameof(quantity));

        if (sourceWarehouseId == destWarehouseId)
            throw new ArgumentException("Source and destination warehouses must be different.");

        var sourceItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == sourceWarehouseId);

        if (sourceItem == null || sourceItem.Quantity < quantity)
            throw new InvalidOperationException($"Insufficient stock at source. Available: {sourceItem?.Quantity ?? 0}, Requested: {quantity}");

        var destItem = await GetOrCreateInventoryItemAsync(productId, destWarehouseId);

        sourceItem.Quantity -= quantity;
        sourceItem.LastUpdated = DateTime.UtcNow;
        destItem.Quantity += quantity;
        destItem.LastUpdated = DateTime.UtcNow;

        var transaction = new StockTransaction
        {
            ProductId = productId,
            WarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destWarehouseId,
            Type = TransactionType.Transferred,
            Quantity = quantity,
            Notes = string.IsNullOrEmpty(notes) ? "Inter-warehouse transfer" : notes,
            TransactionDate = DateTime.UtcNow
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<StockTransaction> AdjustStockAsync(int productId, int warehouseId, int newQuantity, string notes = "")
    {
        if (newQuantity < 0)
            throw new ArgumentException("Adjusted quantity cannot be negative.", nameof(newQuantity));

        var item = await GetOrCreateInventoryItemAsync(productId, warehouseId);
        var difference = newQuantity - item.Quantity;

        item.Quantity = newQuantity;
        item.LastUpdated = DateTime.UtcNow;

        var transaction = new StockTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Type = TransactionType.Adjusted,
            Quantity = difference,
            Notes = string.IsNullOrEmpty(notes) ? $"Manual adjustment: {item.Quantity - difference} → {newQuantity}" : notes,
            TransactionDate = DateTime.UtcNow
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <inheritdoc/>
    public StockLevel GetStockLevel(int currentQuantity, int reorderPoint)
    {
        if (currentQuantity <= reorderPoint)
            return StockLevel.Critical;
        if (currentQuantity <= reorderPoint * 2)
            return StockLevel.Low;
        return StockLevel.Healthy;
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalStockAsync(int productId)
    {
        return await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .SumAsync(i => i.Quantity);
    }

    /// <inheritdoc/>
    public async Task<List<StockTransaction>> GetRecentTransactionsAsync(int count = 20, int? productId = null, int? warehouseId = null)
    {
        var query = _context.StockTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseId == warehouseId.Value);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(count)
            .ToListAsync();
    }

    private async Task<InventoryItem> GetOrCreateInventoryItemAsync(int productId, int warehouseId)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.InventoryItems.Add(item);
        }

        return item;
    }
}
