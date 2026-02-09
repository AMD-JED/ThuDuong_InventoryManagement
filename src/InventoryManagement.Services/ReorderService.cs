using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;
using InventoryManagement.Data;

namespace InventoryManagement.Services;

/// <summary>
/// Service for automated reorder calculations and low-stock alert management.
/// Monitors inventory levels and generates alerts when stock falls below reorder points.
/// </summary>
public class ReorderService : IReorderService
{
    private readonly AppDbContext _context;

    public ReorderService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<ReorderAlert>> CheckAndGenerateAlertsAsync()
    {
        var newAlerts = new List<ReorderAlert>();

        var inventoryItems = await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.Product.IsActive)
            .ToListAsync();

        foreach (var item in inventoryItems)
        {
            if (item.Quantity <= item.Product.ReorderPoint)
            {
                // Check if an unresolved alert already exists for this product/warehouse
                var existingAlert = await _context.ReorderAlerts
                    .AnyAsync(a => a.ProductId == item.ProductId &&
                                   a.WarehouseId == item.WarehouseId &&
                                   !a.IsResolved);

                if (!existingAlert)
                {
                    var alert = new ReorderAlert
                    {
                        ProductId = item.ProductId,
                        WarehouseId = item.WarehouseId,
                        CurrentStock = item.Quantity,
                        ReorderPoint = item.Product.ReorderPoint,
                        RecommendedOrderQuantity = item.Product.ReorderQuantity,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ReorderAlerts.Add(alert);
                    newAlerts.Add(alert);
                }
            }
        }

        if (newAlerts.Count > 0)
            await _context.SaveChangesAsync();

        // Reload with navigation properties
        if (newAlerts.Count > 0)
        {
            var alertIds = newAlerts.Select(a => a.Id).ToList();
            newAlerts = await _context.ReorderAlerts
                .Include(a => a.Product)
                .Include(a => a.Warehouse)
                .Where(a => alertIds.Contains(a.Id))
                .ToListAsync();
        }

        return newAlerts;
    }

    /// <inheritdoc/>
    public async Task<List<ReorderAlert>> GetPendingAlertsAsync()
    {
        return await _context.ReorderAlerts
            .Include(a => a.Product)
                .ThenInclude(p => p.Supplier)
            .Include(a => a.Warehouse)
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task ResolveAlertAsync(int alertId)
    {
        var alert = await _context.ReorderAlerts.FindAsync(alertId);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingAlertCountAsync()
    {
        return await _context.ReorderAlerts.CountAsync(a => !a.IsResolved);
    }
}
