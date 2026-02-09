using InventoryManagement.Core.Interfaces;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Console UI for KPI dashboard and reporting — inventory turnover,
/// stock levels, and supplier performance metrics.
/// </summary>
public class ReportsMenu
{
    private readonly IReportingService _reportingService;
    private readonly IInventoryService _inventoryService;
    private readonly IReorderService _reorderService;

    public ReportsMenu(
        IReportingService reportingService,
        IInventoryService inventoryService,
        IReorderService reorderService)
    {
        _reportingService = reportingService;
        _inventoryService = inventoryService;
        _reorderService = reorderService;
    }

    /// <summary>Shows the reports and KPI dashboard menu.</summary>
    public async Task ShowAsync()
    {
        while (true)
        {
            var choice = ConsoleHelper.ShowMenu("KPI DASHBOARD & REPORTS", new[]
            {
                "📊  Full KPI Dashboard",
                "🏭  Stock Levels by Warehouse",
                "📦  Top Products by Value",
                "⚠️   Items Below Reorder Point",
                "🤝  Supplier Performance",
                "📁  Category Summary",
                "📜  Recent Transactions"
            });

            switch (choice)
            {
                case 1: await ShowFullDashboardAsync(); break;
                case 2: await ShowWarehouseStockAsync(); break;
                case 3: await ShowTopProductsAsync(); break;
                case 4: await ShowReorderItemsAsync(); break;
                case 5: await ShowSupplierPerformanceAsync(); break;
                case 6: await ShowCategorySummaryAsync(); break;
                case 7: await ShowRecentTransactionsAsync(); break;
                case 0: return;
                default:
                    ConsoleHelper.Error("Invalid option.");
                    ConsoleHelper.WaitForKey();
                    break;
            }
        }
    }

    private async Task ShowFullDashboardAsync()
    {
        ConsoleHelper.PrintHeader("📊 KPI DASHBOARD — INVENTORY MANAGEMENT");

        // Key Metrics Row
        var totalValue = await _reportingService.GetTotalInventoryValueAsync();
        var turnover = await _reportingService.GetInventoryTurnoverRateAsync();
        var alertCount = await _reorderService.GetPendingAlertCountAsync();
        var reorderItems = await _reportingService.GetItemsBelowReorderPointAsync();

        ConsoleHelper.PrintSubHeader("KEY PERFORMANCE INDICATORS");

        // KPI Cards
        System.Console.WriteLine();
        PrintKpiCard("Total Inventory Value", ConsoleHelper.FormatCurrency(totalValue), ConsoleColor.Green);
        PrintKpiCard("Turnover Rate (Annual)", $"{turnover:F2}x", ConsoleColor.Cyan);
        PrintKpiCard("Pending Reorder Alerts", alertCount.ToString(), alertCount > 0 ? ConsoleColor.Red : ConsoleColor.Green);
        PrintKpiCard("Items Below Reorder", reorderItems.Count.ToString(), reorderItems.Count > 0 ? ConsoleColor.Yellow : ConsoleColor.Green);
        System.Console.WriteLine();

        // Warehouse Summary
        ConsoleHelper.PrintSubHeader("STOCK BY WAREHOUSE");
        var warehouseData = await _reportingService.GetStockByWarehouseAsync();

        var wHeaders = new[] { "Warehouse", "Products", "Units", "Value", "Utilization" };
        var wWidths = new[] { 24, 10, 10, 14, 14 };
        ConsoleHelper.PrintTableHeader(wHeaders, wWidths);

        foreach (var w in warehouseData)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                w.WarehouseName, w.TotalProducts.ToString(),
                w.TotalUnits.ToString("N0"), ConsoleHelper.FormatCurrency(w.TotalValue),
                $"{w.UtilizationPercent:F1}%"
            }, wWidths);
        }

        // Top Products
        ConsoleHelper.PrintSubHeader("TOP 5 PRODUCTS BY VALUE");
        var topProducts = await _reportingService.GetTopProductsByValueAsync(5);

        var pHeaders = new[] { "Product", "SKU", "Qty", "Unit Price", "Total Value" };
        var pWidths = new[] { 22, 10, 8, 12, 14 };
        ConsoleHelper.PrintTableHeader(pHeaders, pWidths);

        foreach (var p in topProducts)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                p.ProductName, p.SKU, p.TotalQuantity.ToString(),
                ConsoleHelper.FormatCurrency(p.UnitPrice), ConsoleHelper.FormatCurrency(p.TotalValue)
            }, pWidths);
        }

        // Reorder Alerts Preview
        if (reorderItems.Count > 0)
        {
            ConsoleHelper.PrintSubHeader("⚠️  ITEMS REQUIRING REORDER");
            var rHeaders = new[] { "Product", "Warehouse", "Stock", "Reorder Pt", "Supplier" };
            var rWidths = new[] { 20, 22, 8, 10, 18 };
            ConsoleHelper.PrintTableHeader(rHeaders, rWidths);

            foreach (var r in reorderItems.Take(5))
            {
                ConsoleHelper.PrintTableRow(new[]
                {
                    r.ProductName, r.WarehouseName, r.CurrentStock.ToString(),
                    r.ReorderPoint.ToString(), r.SupplierName
                }, rWidths, ConsoleColor.Red);
            }

            if (reorderItems.Count > 5)
                ConsoleHelper.Info($"...and {reorderItems.Count - 5} more. View full list in Reorder Alerts.");
        }

        // Recent Transactions
        ConsoleHelper.PrintSubHeader("RECENT TRANSACTIONS");
        var recentTx = await _inventoryService.GetRecentTransactionsAsync(5);

        var tHeaders = new[] { "Date", "Type", "Product", "Warehouse", "Qty" };
        var tWidths = new[] { 18, 12, 20, 22, 8 };
        ConsoleHelper.PrintTableHeader(tHeaders, tWidths);

        foreach (var tx in recentTx)
        {
            var qtyStr = tx.Quantity >= 0 ? $"+{tx.Quantity}" : tx.Quantity.ToString();
            ConsoleHelper.PrintTableRow(new[]
            {
                tx.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                tx.Type.ToString(), tx.Product.Name,
                tx.Warehouse.Name, qtyStr
            }, tWidths);
        }

        ConsoleHelper.WaitForKey();
    }

    private async Task ShowWarehouseStockAsync()
    {
        ConsoleHelper.PrintHeader("STOCK LEVELS BY WAREHOUSE");
        var data = await _reportingService.GetStockByWarehouseAsync();

        foreach (var w in data)
        {
            ConsoleHelper.WriteLineColor($"\n  🏭 {w.WarehouseName}", ConsoleColor.Cyan);
            System.Console.Write($"     Capacity: ");
            ConsoleHelper.PrintProgressBar(w.UtilizationPercent, 35);
            System.Console.WriteLine();
            System.Console.WriteLine($"     Products: {w.TotalProducts} | Units: {w.TotalUnits:N0} | Value: {ConsoleHelper.FormatCurrency(w.TotalValue)}");
        }

        ConsoleHelper.WaitForKey();
    }

    private async Task ShowTopProductsAsync()
    {
        ConsoleHelper.PrintHeader("TOP PRODUCTS BY INVENTORY VALUE");
        var products = await _reportingService.GetTopProductsByValueAsync(10);

        var headers = new[] { "#", "Product", "SKU", "Total Qty", "Unit Price", "Total Value" };
        var widths = new[] { 3, 22, 10, 10, 12, 14 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        int rank = 1;
        foreach (var p in products)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                rank.ToString(), p.ProductName, p.SKU,
                p.TotalQuantity.ToString("N0"),
                ConsoleHelper.FormatCurrency(p.UnitPrice),
                ConsoleHelper.FormatCurrency(p.TotalValue)
            }, widths);
            rank++;
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ShowReorderItemsAsync()
    {
        ConsoleHelper.PrintHeader("⚠️  ITEMS BELOW REORDER POINT");
        var items = await _reportingService.GetItemsBelowReorderPointAsync();

        if (items.Count == 0)
        {
            ConsoleHelper.Success("All items are above their reorder points!");
            ConsoleHelper.WaitForKey();
            return;
        }

        var headers = new[] { "Product", "SKU", "Warehouse", "Stock", "Reorder Pt", "Order Qty", "Supplier" };
        var widths = new[] { 18, 10, 20, 8, 10, 10, 18 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var item in items)
        {
            var color = item.CurrentStock == 0 ? ConsoleColor.Red : ConsoleColor.Yellow;
            ConsoleHelper.PrintTableRow(new[]
            {
                item.ProductName, item.SKU, item.WarehouseName,
                item.CurrentStock.ToString(), item.ReorderPoint.ToString(),
                item.ReorderQuantity.ToString(), item.SupplierName
            }, widths, color);
        }

        ConsoleHelper.Info($"Total: {items.Count} item(s) need reordering");
        ConsoleHelper.WaitForKey();
    }

    private async Task ShowSupplierPerformanceAsync()
    {
        ConsoleHelper.PrintHeader("🤝 SUPPLIER PERFORMANCE METRICS");
        var suppliers = await _reportingService.GetSupplierPerformanceAsync();

        var headers = new[] { "Supplier", "Products", "Total Units", "Total Value", "Rating", "Lead Time" };
        var widths = new[] { 22, 10, 12, 14, 8, 10 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var s in suppliers)
        {
            var stars = new string('★', s.Rating) + new string('☆', 5 - s.Rating);
            ConsoleHelper.PrintTableRow(new[]
            {
                s.SupplierName, s.ProductCount.ToString(),
                s.TotalUnitsSupplied.ToString("N0"),
                ConsoleHelper.FormatCurrency(s.TotalValue),
                stars, $"{s.LeadTimeDays} days"
            }, widths);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ShowCategorySummaryAsync()
    {
        ConsoleHelper.PrintHeader("📁 INVENTORY BY CATEGORY");
        var categories = await _reportingService.GetCategorySummaryAsync();

        var headers = new[] { "Category", "Products", "Total Units", "Total Value", "% of Total" };
        var widths = new[] { 18, 10, 12, 14, 10 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        var grandTotal = categories.Sum(c => c.TotalValue);

        foreach (var c in categories)
        {
            var pct = grandTotal > 0 ? (c.TotalValue / grandTotal * 100) : 0;
            ConsoleHelper.PrintTableRow(new[]
            {
                c.Category, c.ProductCount.ToString(),
                c.TotalUnits.ToString("N0"),
                ConsoleHelper.FormatCurrency(c.TotalValue),
                $"{pct:F1}%"
            }, widths);
        }

        ConsoleHelper.PrintSeparator();
        System.Console.WriteLine($"  {"TOTAL",-20} {categories.Sum(c => c.ProductCount),-12} {categories.Sum(c => c.TotalUnits),-12:N0} {ConsoleHelper.FormatCurrency(grandTotal)}");
        ConsoleHelper.WaitForKey();
    }

    private async Task ShowRecentTransactionsAsync()
    {
        ConsoleHelper.PrintHeader("📜 RECENT TRANSACTIONS");
        var transactions = await _inventoryService.GetRecentTransactionsAsync(25);

        if (transactions.Count == 0)
        {
            ConsoleHelper.Info("No transactions recorded.");
            ConsoleHelper.WaitForKey();
            return;
        }

        var headers = new[] { "ID", "Date", "Type", "Product", "Warehouse", "Qty", "Reference", "Notes" };
        var widths = new[] { 4, 18, 12, 18, 18, 8, 12, 24 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var tx in transactions)
        {
            var qtyStr = tx.Quantity >= 0 ? $"+{tx.Quantity}" : tx.Quantity.ToString();
            var typeColor = tx.Type switch
            {
                Core.Enums.TransactionType.Received => ConsoleColor.Green,
                Core.Enums.TransactionType.Shipped => ConsoleColor.Red,
                Core.Enums.TransactionType.Transferred => ConsoleColor.Cyan,
                Core.Enums.TransactionType.Adjusted => ConsoleColor.Yellow,
                Core.Enums.TransactionType.Returned => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };

            ConsoleHelper.PrintTableRow(new[]
            {
                tx.Id.ToString(),
                tx.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                tx.Type.ToString(), tx.Product.Name,
                tx.Warehouse.Name, qtyStr,
                tx.ReferenceNumber, tx.Notes
            }, widths, typeColor);
        }
        ConsoleHelper.WaitForKey();
    }

    private void PrintKpiCard(string label, string value, ConsoleColor valueColor)
    {
        System.Console.Write("  ┌─────────────────────────────┐  ");
        System.Console.WriteLine();
        System.Console.Write($"  │ ");
        ConsoleHelper.WriteColor($"{label,-27}", ConsoleColor.DarkGray);
        System.Console.Write(" │  ");
        System.Console.WriteLine();
        System.Console.Write($"  │ ");
        ConsoleHelper.WriteColor($"{value,-27}", valueColor);
        System.Console.Write(" │  ");
        System.Console.WriteLine();
        System.Console.Write("  └─────────────────────────────┘  ");
        System.Console.WriteLine();
    }
}
