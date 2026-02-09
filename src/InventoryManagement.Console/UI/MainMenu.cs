using InventoryManagement.Core.Interfaces;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Main application menu and navigation hub.
/// </summary>
public class MainMenu
{
    private readonly ProductMenu _productMenu;
    private readonly WarehouseMenu _warehouseMenu;
    private readonly InventoryMenu _inventoryMenu;
    private readonly ReportsMenu _reportsMenu;
    private readonly IReorderService _reorderService;

    public MainMenu(
        ProductMenu productMenu,
        WarehouseMenu warehouseMenu,
        InventoryMenu inventoryMenu,
        ReportsMenu reportsMenu,
        IReorderService reorderService)
    {
        _productMenu = productMenu;
        _warehouseMenu = warehouseMenu;
        _inventoryMenu = inventoryMenu;
        _reportsMenu = reportsMenu;
        _reorderService = reorderService;
    }

    /// <summary>Runs the main application loop.</summary>
    public async Task RunAsync()
    {
        while (true)
        {
            System.Console.Clear();
            PrintBanner();
            await PrintAlertSummaryAsync();

            var choice = ConsoleHelper.ShowMenu("MAIN MENU", new[]
            {
                "📦  Product Management",
                "🏭  Warehouse Management",
                "📊  Inventory Operations",
                "📈  KPI Dashboard & Reports",
                "🔔  Reorder Alerts",
                "🔍  Search & Filter"
            });

            switch (choice)
            {
                case 1: await _productMenu.ShowAsync(); break;
                case 2: await _warehouseMenu.ShowAsync(); break;
                case 3: await _inventoryMenu.ShowAsync(); break;
                case 4: await _reportsMenu.ShowAsync(); break;
                case 5: await _inventoryMenu.ShowReorderAlertsAsync(); break;
                case 6: await _productMenu.SearchProductsAsync(); break;
                case 0:
                    ConsoleHelper.WriteLineColor("\n  👋 Thank you for using Inventory Management System!\n", ConsoleColor.Cyan);
                    return;
                default:
                    ConsoleHelper.Error("Invalid option. Please try again.");
                    ConsoleHelper.WaitForKey();
                    break;
            }
        }
    }

    private void PrintBanner()
    {
        ConsoleHelper.WriteLineColor(@"
   ╔══════════════════════════════════════════════════════════════╗
   ║                                                              ║
   ║    ██╗███╗   ██╗██╗   ██╗███████╗███╗   ██╗████████╗        ║
   ║    ██║████╗  ██║██║   ██║██╔════╝████╗  ██║╚══██╔══╝        ║
   ║    ██║██╔██╗ ██║██║   ██║█████╗  ██╔██╗ ██║   ██║           ║
   ║    ██║██║╚██╗██║╚██╗ ██╔╝██╔══╝  ██║╚██╗██║   ██║           ║
   ║    ██║██║ ╚████║ ╚████╔╝ ███████╗██║ ╚████║   ██║           ║
   ║    ╚═╝╚═╝  ╚═══╝  ╚═══╝  ╚══════╝╚═╝  ╚═══╝   ╚═╝           ║
   ║                                                              ║
   ║     📦  I N V E N T O R Y   M A N A G E M E N T             ║
   ║     🏭  Multi-Location Warehouse Tracking System             ║
   ║                                                              ║
   ╚══════════════════════════════════════════════════════════════╝", ConsoleColor.DarkCyan);
    }

    private async Task PrintAlertSummaryAsync()
    {
        var alertCount = await _reorderService.GetPendingAlertCountAsync();
        if (alertCount > 0)
        {
            System.Console.WriteLine();
            ConsoleHelper.WriteColor("   🔔 ", ConsoleColor.Yellow);
            ConsoleHelper.WriteLineColor($"{alertCount} pending reorder alert(s) — select option 5 to view", ConsoleColor.Yellow);
        }
    }
}
