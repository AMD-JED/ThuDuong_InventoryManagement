using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Console UI for inventory operations — stock tracking, transactions, and reorder alerts.
/// </summary>
public class InventoryMenu
{
    private readonly IInventoryService _inventoryService;
    private readonly IReorderService _reorderService;
    private readonly IProductRepository _productRepo;
    private readonly IRepository<Warehouse> _warehouseRepo;

    public InventoryMenu(
        IInventoryService inventoryService,
        IReorderService reorderService,
        IProductRepository productRepo,
        IRepository<Warehouse> warehouseRepo)
    {
        _inventoryService = inventoryService;
        _reorderService = reorderService;
        _productRepo = productRepo;
        _warehouseRepo = warehouseRepo;
    }

    /// <summary>Shows the inventory operations menu.</summary>
    public async Task ShowAsync()
    {
        while (true)
        {
            var choice = ConsoleHelper.ShowMenu("INVENTORY OPERATIONS", new[]
            {
                "📋  View All Inventory",
                "📦  View Product Stock (All Warehouses)",
                "🏭  View Warehouse Inventory",
                "📥  Receive Stock",
                "📤  Ship Stock",
                "🔄  Transfer Between Warehouses",
                "✏️   Adjust Stock",
                "📜  View Transaction History",
                "🔔  Reorder Alerts"
            });

            switch (choice)
            {
                case 1: await ViewAllInventoryAsync(); break;
                case 2: await ViewProductStockAsync(); break;
                case 3: await ViewWarehouseStockAsync(); break;
                case 4: await ReceiveStockAsync(); break;
                case 5: await ShipStockAsync(); break;
                case 6: await TransferStockAsync(); break;
                case 7: await AdjustStockAsync(); break;
                case 8: await ViewTransactionsAsync(); break;
                case 9: await ShowReorderAlertsAsync(); break;
                case 0: return;
                default:
                    ConsoleHelper.Error("Invalid option.");
                    ConsoleHelper.WaitForKey();
                    break;
            }
        }
    }

    private async Task ViewAllInventoryAsync()
    {
        ConsoleHelper.PrintHeader("INVENTORY OVERVIEW — ALL LOCATIONS");
        var items = await _inventoryService.GetAllInventoryAsync();

        if (items.Count == 0)
        {
            ConsoleHelper.Info("No inventory records found.");
            ConsoleHelper.WaitForKey();
            return;
        }

        var headers = new[] { "Product", "SKU", "Warehouse", "Qty", "Status", "Value" };
        var widths = new[] { 20, 10, 22, 8, 10, 12 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var item in items)
        {
            var level = _inventoryService.GetStockLevel(item.Quantity, item.Product.ReorderPoint);
            var indicator = ConsoleHelper.GetStockLevelIndicator(level);
            var color = ConsoleHelper.GetStockLevelColor(level);

            ConsoleHelper.PrintTableRow(new[]
            {
                item.Product.Name, item.Product.SKU, item.Warehouse.Name,
                item.Quantity.ToString(), $"{indicator} {level}",
                ConsoleHelper.FormatCurrency(item.Quantity * item.Product.UnitPrice)
            }, widths, color);
        }

        var totalValue = items.Sum(i => i.Quantity * i.Product.UnitPrice);
        var totalUnits = items.Sum(i => i.Quantity);
        ConsoleHelper.PrintSeparator(82);
        System.Console.WriteLine();
        ConsoleHelper.Info($"Total: {items.Count} records | {totalUnits:N0} units | {ConsoleHelper.FormatCurrency(totalValue)}");

        // Legend
        System.Console.WriteLine();
        ConsoleHelper.WriteColor("  Legend: ", ConsoleColor.DarkGray);
        ConsoleHelper.WriteColor("🟢 Healthy  ", ConsoleColor.Green);
        ConsoleHelper.WriteColor("🟡 Low  ", ConsoleColor.Yellow);
        ConsoleHelper.WriteLineColor("🔴 Critical", ConsoleColor.Red);

        ConsoleHelper.WaitForKey();
    }

    private async Task ViewProductStockAsync()
    {
        ConsoleHelper.PrintSubHeader("View Product Stock Across Warehouses");
        var id = ConsoleHelper.PromptInt("Enter Product ID");
        if (id == null) return;

        var product = await _productRepo.GetByIdAsync(id.Value);
        if (product == null) { ConsoleHelper.Error("Product not found."); ConsoleHelper.WaitForKey(); return; }

        var items = await _inventoryService.GetProductInventoryAsync(id.Value);
        var totalStock = await _inventoryService.GetTotalStockAsync(id.Value);

        ConsoleHelper.PrintHeader($"STOCK: {product.Name} ({product.SKU})");
        System.Console.WriteLine($"  Reorder Point: {product.ReorderPoint} | Reorder Qty: {product.ReorderQuantity} | Total Stock: {totalStock}");
        System.Console.WriteLine();

        var headers = new[] { "Warehouse", "City", "Qty", "Status", "Value" };
        var widths = new[] { 24, 14, 8, 10, 12 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var item in items)
        {
            var level = _inventoryService.GetStockLevel(item.Quantity, product.ReorderPoint);
            var indicator = ConsoleHelper.GetStockLevelIndicator(level);
            var color = ConsoleHelper.GetStockLevelColor(level);

            ConsoleHelper.PrintTableRow(new[]
            {
                item.Warehouse.Name, item.Warehouse.City,
                item.Quantity.ToString(), $"{indicator} {level}",
                ConsoleHelper.FormatCurrency(item.Quantity * product.UnitPrice)
            }, widths, color);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewWarehouseStockAsync()
    {
        ConsoleHelper.PrintSubHeader("View Warehouse Inventory");
        var id = ConsoleHelper.PromptInt("Enter Warehouse ID");
        if (id == null) return;

        var warehouse = await _warehouseRepo.GetByIdAsync(id.Value);
        if (warehouse == null) { ConsoleHelper.Error("Warehouse not found."); ConsoleHelper.WaitForKey(); return; }

        var items = await _inventoryService.GetWarehouseInventoryAsync(id.Value);

        ConsoleHelper.PrintHeader($"INVENTORY AT: {warehouse.Name}");

        var headers = new[] { "Product", "SKU", "Qty", "Status", "Value" };
        var widths = new[] { 22, 10, 8, 10, 12 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var item in items)
        {
            var level = _inventoryService.GetStockLevel(item.Quantity, item.Product.ReorderPoint);
            var indicator = ConsoleHelper.GetStockLevelIndicator(level);
            var color = ConsoleHelper.GetStockLevelColor(level);

            ConsoleHelper.PrintTableRow(new[]
            {
                item.Product.Name, item.Product.SKU,
                item.Quantity.ToString(), $"{indicator} {level}",
                ConsoleHelper.FormatCurrency(item.Quantity * item.Product.UnitPrice)
            }, widths, color);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ReceiveStockAsync()
    {
        ConsoleHelper.PrintHeader("RECEIVE STOCK");
        await ShowQuickReferencesAsync();

        var productId = ConsoleHelper.PromptInt("Product ID");
        var warehouseId = ConsoleHelper.PromptInt("Warehouse ID");
        var quantity = ConsoleHelper.PromptInt("Quantity to receive");
        var reference = ConsoleHelper.Prompt("Reference # (optional)");
        var notes = ConsoleHelper.Prompt("Notes (optional)");

        if (productId == null || warehouseId == null || quantity == null || quantity <= 0)
        {
            ConsoleHelper.Error("Invalid input. All fields are required and quantity must be positive.");
            ConsoleHelper.WaitForKey();
            return;
        }

        try
        {
            var tx = await _inventoryService.ReceiveStockAsync(productId.Value, warehouseId.Value, quantity.Value, reference ?? "", notes ?? "");
            ConsoleHelper.Success($"Received {quantity} units. Transaction ID: {tx.Id}");

            // Check and generate reorder alerts
            await _reorderService.CheckAndGenerateAlertsAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error(ex.Message);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ShipStockAsync()
    {
        ConsoleHelper.PrintHeader("SHIP STOCK");
        await ShowQuickReferencesAsync();

        var productId = ConsoleHelper.PromptInt("Product ID");
        var warehouseId = ConsoleHelper.PromptInt("Warehouse ID");
        var quantity = ConsoleHelper.PromptInt("Quantity to ship");
        var reference = ConsoleHelper.Prompt("Reference # (optional)");
        var notes = ConsoleHelper.Prompt("Notes (optional)");

        if (productId == null || warehouseId == null || quantity == null || quantity <= 0)
        {
            ConsoleHelper.Error("Invalid input.");
            ConsoleHelper.WaitForKey();
            return;
        }

        try
        {
            var tx = await _inventoryService.ShipStockAsync(productId.Value, warehouseId.Value, quantity.Value, reference ?? "", notes ?? "");
            ConsoleHelper.Success($"Shipped {quantity} units. Transaction ID: {tx.Id}");

            // Check reorder alerts after shipping
            var newAlerts = await _reorderService.CheckAndGenerateAlertsAsync();
            if (newAlerts.Count > 0)
            {
                ConsoleHelper.Warning($"{newAlerts.Count} new reorder alert(s) generated!");
                foreach (var alert in newAlerts)
                    ConsoleHelper.Warning($"  → {alert.Product.Name} at {alert.Warehouse.Name}: {alert.CurrentStock} units (reorder point: {alert.ReorderPoint})");
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error(ex.Message);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task TransferStockAsync()
    {
        ConsoleHelper.PrintHeader("TRANSFER STOCK BETWEEN WAREHOUSES");
        await ShowQuickReferencesAsync();

        var productId = ConsoleHelper.PromptInt("Product ID");
        var sourceId = ConsoleHelper.PromptInt("Source Warehouse ID");
        var destId = ConsoleHelper.PromptInt("Destination Warehouse ID");
        var quantity = ConsoleHelper.PromptInt("Quantity to transfer");
        var notes = ConsoleHelper.Prompt("Notes (optional)");

        if (productId == null || sourceId == null || destId == null || quantity == null || quantity <= 0)
        {
            ConsoleHelper.Error("Invalid input.");
            ConsoleHelper.WaitForKey();
            return;
        }

        try
        {
            var tx = await _inventoryService.TransferStockAsync(productId.Value, sourceId.Value, destId.Value, quantity.Value, notes ?? "");
            ConsoleHelper.Success($"Transferred {quantity} units. Transaction ID: {tx.Id}");
            await _reorderService.CheckAndGenerateAlertsAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error(ex.Message);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task AdjustStockAsync()
    {
        ConsoleHelper.PrintHeader("ADJUST STOCK COUNT");
        await ShowQuickReferencesAsync();

        var productId = ConsoleHelper.PromptInt("Product ID");
        var warehouseId = ConsoleHelper.PromptInt("Warehouse ID");
        var newQty = ConsoleHelper.PromptInt("New Quantity (actual count)");
        var notes = ConsoleHelper.Prompt("Reason for adjustment");

        if (productId == null || warehouseId == null || newQty == null)
        {
            ConsoleHelper.Error("Invalid input.");
            ConsoleHelper.WaitForKey();
            return;
        }

        try
        {
            var tx = await _inventoryService.AdjustStockAsync(productId.Value, warehouseId.Value, newQty.Value, notes ?? "");
            ConsoleHelper.Success($"Stock adjusted. Difference: {(tx.Quantity >= 0 ? "+" : "")}{tx.Quantity}. Transaction ID: {tx.Id}");
            await _reorderService.CheckAndGenerateAlertsAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error(ex.Message);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewTransactionsAsync()
    {
        ConsoleHelper.PrintHeader("TRANSACTION HISTORY");

        ConsoleHelper.Info("Filter options (press Enter to skip):");
        var productId = ConsoleHelper.PromptInt("Product ID (optional)");
        var warehouseId = ConsoleHelper.PromptInt("Warehouse ID (optional)");
        var count = ConsoleHelper.PromptInt("Number of records (default: 20)") ?? 20;

        var transactions = await _inventoryService.GetRecentTransactionsAsync(count, productId, warehouseId);

        if (transactions.Count == 0)
        {
            ConsoleHelper.Info("No transactions found.");
            ConsoleHelper.WaitForKey();
            return;
        }

        var headers = new[] { "ID", "Date", "Type", "Product", "Warehouse", "Qty", "Reference" };
        var widths = new[] { 4, 18, 12, 18, 20, 8, 12 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var tx in transactions)
        {
            var typeColor = tx.Type switch
            {
                TransactionType.Received => ConsoleColor.Green,
                TransactionType.Shipped => ConsoleColor.Red,
                TransactionType.Transferred => ConsoleColor.Cyan,
                TransactionType.Adjusted => ConsoleColor.Yellow,
                TransactionType.Returned => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };

            var qtyStr = tx.Quantity >= 0 ? $"+{tx.Quantity}" : tx.Quantity.ToString();

            ConsoleHelper.PrintTableRow(new[]
            {
                tx.Id.ToString(),
                tx.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                tx.Type.ToString(),
                tx.Product.Name,
                tx.Warehouse.Name,
                qtyStr,
                tx.ReferenceNumber
            }, widths, typeColor);
        }

        System.Console.WriteLine();
        ConsoleHelper.WriteColor("  Legend: ", ConsoleColor.DarkGray);
        ConsoleHelper.WriteColor("Received  ", ConsoleColor.Green);
        ConsoleHelper.WriteColor("Shipped  ", ConsoleColor.Red);
        ConsoleHelper.WriteColor("Transferred  ", ConsoleColor.Cyan);
        ConsoleHelper.WriteColor("Adjusted  ", ConsoleColor.Yellow);
        ConsoleHelper.WriteLineColor("Returned", ConsoleColor.Magenta);

        ConsoleHelper.WaitForKey();
    }

    /// <summary>Shows pending reorder alerts with options to resolve.</summary>
    public async Task ShowReorderAlertsAsync()
    {
        // First scan for new alerts
        var newAlerts = await _reorderService.CheckAndGenerateAlertsAsync();
        if (newAlerts.Count > 0)
            ConsoleHelper.Warning($"{newAlerts.Count} new alert(s) generated.");

        var alerts = await _reorderService.GetPendingAlertsAsync();

        ConsoleHelper.PrintHeader("🔔 REORDER ALERTS");

        if (alerts.Count == 0)
        {
            ConsoleHelper.Success("No pending reorder alerts. All stock levels are healthy!");
            ConsoleHelper.WaitForKey();
            return;
        }

        ConsoleHelper.WriteLineColor($"  {alerts.Count} pending alert(s):\n", ConsoleColor.Yellow);

        var headers = new[] { "Alert ID", "Product", "Warehouse", "Current", "Reorder Pt", "Order Qty", "Supplier" };
        var widths = new[] { 8, 18, 20, 8, 10, 10, 18 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var alert in alerts)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                alert.Id.ToString(),
                alert.Product.Name,
                alert.Warehouse.Name,
                alert.CurrentStock.ToString(),
                alert.ReorderPoint.ToString(),
                alert.RecommendedOrderQuantity.ToString(),
                alert.Product.Supplier?.Name ?? "N/A"
            }, widths, ConsoleColor.Red);
        }

        System.Console.WriteLine();
        var resolveId = ConsoleHelper.PromptInt("Enter Alert ID to resolve (or press Enter to skip)");
        if (resolveId.HasValue)
        {
            await _reorderService.ResolveAlertAsync(resolveId.Value);
            ConsoleHelper.Success($"Alert #{resolveId.Value} resolved.");
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ShowQuickReferencesAsync()
    {
        var products = await _productRepo.GetAllAsync();
        var warehouses = await _warehouseRepo.GetAllAsync();

        ConsoleHelper.WriteLineColor("  Quick Reference:", ConsoleColor.DarkGray);
        ConsoleHelper.WriteColor("  Products: ", ConsoleColor.DarkGray);
        var productRefs = products.Take(8).Select(p => $"[{p.Id}]{p.SKU}");
        ConsoleHelper.WriteLineColor(string.Join("  ", productRefs), ConsoleColor.DarkGray);
        ConsoleHelper.WriteColor("  Warehouses: ", ConsoleColor.DarkGray);
        var whRefs = warehouses.Take(5).Select(w => $"[{w.Id}]{w.Name}");
        ConsoleHelper.WriteLineColor(string.Join("  ", whRefs), ConsoleColor.DarkGray);
        System.Console.WriteLine();
    }
}
