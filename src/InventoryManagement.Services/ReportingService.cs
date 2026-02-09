using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Data;

namespace InventoryManagement.Services;

/// <summary>
/// Service for KPI dashboard calculations and inventory reporting.
/// Tracks inventory turnover, stock levels, and supplier performance metrics.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly AppDbContext _context;

    public ReportingService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetTotalInventoryValueAsync()
    {
        return await _context.InventoryItems
            .Include(i => i.Product)
            .SumAsync(i => i.Quantity * i.Product.UnitPrice);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetInventoryTurnoverRateAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var totalShipped = await _context.StockTransactions
            .Where(t => t.Type == TransactionType.Shipped && t.TransactionDate >= startDate)
            .SumAsync(t => Math.Abs(t.Quantity));

        var averageInventory = await _context.InventoryItems.SumAsync(i => i.Quantity);

        if (averageInventory == 0) return 0;

        // Annualize the turnover rate
        var periodFactor = 365.0m / days;
        return Math.Round((decimal)totalShipped / averageInventory * periodFactor, 2);
    }

    /// <inheritdoc/>
    public async Task<List<WarehouseStockSummary>> GetStockByWarehouseAsync()
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.InventoryItems)
                .ThenInclude(i => i.Product)
            .Where(w => w.IsActive)
            .ToListAsync();

        return warehouses.Select(w => new WarehouseStockSummary
        {
            WarehouseId = w.Id,
            WarehouseName = w.Name,
            TotalProducts = w.InventoryItems.Count,
            TotalUnits = w.InventoryItems.Sum(i => i.Quantity),
            TotalValue = w.InventoryItems.Sum(i => i.Quantity * i.Product.UnitPrice),
            Capacity = w.Capacity,
            UtilizationPercent = w.Capacity > 0
                ? Math.Round((double)w.InventoryItems.Sum(i => i.Quantity) / w.Capacity * 100, 1)
                : 0
        })
        .OrderByDescending(s => s.TotalValue)
        .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<ProductValueSummary>> GetTopProductsByValueAsync(int count = 10)
    {
        var products = await _context.Products
            .Include(p => p.InventoryItems)
            .Where(p => p.IsActive)
            .ToListAsync();

        return products
            .Select(p => new ProductValueSummary
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                TotalQuantity = p.InventoryItems.Sum(i => i.Quantity),
                UnitPrice = p.UnitPrice,
                TotalValue = p.InventoryItems.Sum(i => i.Quantity) * p.UnitPrice
            })
            .OrderByDescending(p => p.TotalValue)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<ReorderSummary>> GetItemsBelowReorderPointAsync()
    {
        var items = await _context.InventoryItems
            .Include(i => i.Product)
                .ThenInclude(p => p.Supplier)
            .Include(i => i.Warehouse)
            .Where(i => i.Product.IsActive)
            .ToListAsync();

        return items
            .Where(i => i.Quantity <= i.Product.ReorderPoint)
            .Select(i => new ReorderSummary
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                SKU = i.Product.SKU,
                WarehouseName = i.Warehouse.Name,
                CurrentStock = i.Quantity,
                ReorderPoint = i.Product.ReorderPoint,
                ReorderQuantity = i.Product.ReorderQuantity,
                SupplierName = i.Product.Supplier?.Name ?? "N/A"
            })
            .OrderBy(r => r.CurrentStock)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<SupplierPerformance>> GetSupplierPerformanceAsync()
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Products)
                .ThenInclude(p => p.InventoryItems)
            .Where(s => s.IsActive)
            .ToListAsync();

        return suppliers.Select(s => new SupplierPerformance
        {
            SupplierId = s.Id,
            SupplierName = s.Name,
            ProductCount = s.Products.Count,
            TotalUnitsSupplied = s.Products.Sum(p => p.InventoryItems.Sum(i => i.Quantity)),
            TotalValue = s.Products.Sum(p => p.InventoryItems.Sum(i => i.Quantity) * p.UnitPrice),
            Rating = s.Rating,
            LeadTimeDays = s.LeadTimeDays
        })
        .OrderByDescending(s => s.TotalValue)
        .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<CategorySummary>> GetCategorySummaryAsync()
    {
        var products = await _context.Products
            .Include(p => p.InventoryItems)
            .Where(p => p.IsActive)
            .ToListAsync();

        return products
            .GroupBy(p => p.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                ProductCount = g.Count(),
                TotalUnits = g.Sum(p => p.InventoryItems.Sum(i => i.Quantity)),
                TotalValue = g.Sum(p => p.InventoryItems.Sum(i => i.Quantity) * p.UnitPrice)
            })
            .OrderByDescending(c => c.TotalValue)
            .ToList();
    }
}
