namespace InventoryManagement.Core.Interfaces;

/// <summary>
/// Service interface for KPI dashboard and reporting operations.
/// Tracks inventory turnover, stock levels, and supplier performance metrics.
/// </summary>
public interface IReportingService
{
    /// <summary>Calculates the total value of all inventory across all warehouses.</summary>
    Task<decimal> GetTotalInventoryValueAsync();

    /// <summary>Calculates inventory turnover rate over a specified period.</summary>
    Task<decimal> GetInventoryTurnoverRateAsync(int days = 30);

    /// <summary>Gets stock level summary grouped by warehouse.</summary>
    Task<List<WarehouseStockSummary>> GetStockByWarehouseAsync();

    /// <summary>Gets top products ranked by total inventory value.</summary>
    Task<List<ProductValueSummary>> GetTopProductsByValueAsync(int count = 10);

    /// <summary>Gets all products below their reorder point.</summary>
    Task<List<ReorderSummary>> GetItemsBelowReorderPointAsync();

    /// <summary>Gets supplier performance metrics.</summary>
    Task<List<SupplierPerformance>> GetSupplierPerformanceAsync();

    /// <summary>Gets inventory summary by category.</summary>
    Task<List<CategorySummary>> GetCategorySummaryAsync();
}

/// <summary>Summary of stock levels at a warehouse.</summary>
public class WarehouseStockSummary
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
    public int Capacity { get; set; }
    public double UtilizationPercent { get; set; }
}

/// <summary>Product ranked by total inventory value.</summary>
public class ProductValueSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>Products that need reordering.</summary>
public class ReorderSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}

/// <summary>Supplier performance metrics.</summary>
public class SupplierPerformance
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalUnitsSupplied { get; set; }
    public decimal TotalValue { get; set; }
    public int Rating { get; set; }
    public int LeadTimeDays { get; set; }
}

/// <summary>Inventory summary by product category.</summary>
public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
}
