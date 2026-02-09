using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Console UI for warehouse management operations.
/// </summary>
public class WarehouseMenu
{
    private readonly IRepository<Warehouse> _warehouseRepo;
    private readonly IInventoryService _inventoryService;

    public WarehouseMenu(IRepository<Warehouse> warehouseRepo, IInventoryService inventoryService)
    {
        _warehouseRepo = warehouseRepo;
        _inventoryService = inventoryService;
    }

    /// <summary>Shows the warehouse management menu.</summary>
    public async Task ShowAsync()
    {
        while (true)
        {
            var choice = ConsoleHelper.ShowMenu("WAREHOUSE MANAGEMENT", new[]
            {
                "View All Warehouses",
                "View Warehouse Details & Inventory",
                "Add New Warehouse",
                "Edit Warehouse",
                "Deactivate Warehouse"
            });

            switch (choice)
            {
                case 1: await ListWarehousesAsync(); break;
                case 2: await ViewWarehouseDetailsAsync(); break;
                case 3: await AddWarehouseAsync(); break;
                case 4: await EditWarehouseAsync(); break;
                case 5: await DeactivateWarehouseAsync(); break;
                case 0: return;
                default:
                    ConsoleHelper.Error("Invalid option.");
                    ConsoleHelper.WaitForKey();
                    break;
            }
        }
    }

    private async Task ListWarehousesAsync()
    {
        ConsoleHelper.PrintHeader("ALL WAREHOUSES");
        var warehouses = await _warehouseRepo.GetAllAsync();

        var headers = new[] { "ID", "Name", "City", "State", "Capacity", "Status" };
        var widths = new[] { 4, 24, 14, 8, 10, 8 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var w in warehouses)
        {
            var status = w.IsActive ? "Active" : "Inactive";
            var color = w.IsActive ? ConsoleColor.White : ConsoleColor.DarkGray;
            ConsoleHelper.PrintTableRow(new[]
            {
                w.Id.ToString(), w.Name, w.City, w.State,
                w.Capacity.ToString("N0"), status
            }, widths, color);
        }

        ConsoleHelper.Info($"Total: {warehouses.Count} warehouses");
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewWarehouseDetailsAsync()
    {
        ConsoleHelper.PrintSubHeader("Warehouse Details");
        var id = ConsoleHelper.PromptInt("Enter Warehouse ID");
        if (id == null) return;

        var warehouse = await _warehouseRepo.GetByIdAsync(id.Value);
        if (warehouse == null) { ConsoleHelper.Error("Warehouse not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.PrintHeader($"WAREHOUSE: {warehouse.Name}");
        System.Console.WriteLine($"  {"ID:",-16} {warehouse.Id}");
        System.Console.WriteLine($"  {"Name:",-16} {warehouse.Name}");
        System.Console.WriteLine($"  {"Address:",-16} {warehouse.Address}");
        System.Console.WriteLine($"  {"City:",-16} {warehouse.City}");
        System.Console.WriteLine($"  {"State:",-16} {warehouse.State}");
        System.Console.WriteLine($"  {"Capacity:",-16} {warehouse.Capacity:N0} units");
        System.Console.WriteLine($"  {"Status:",-16} {(warehouse.IsActive ? "Active" : "Inactive")}");

        // Show inventory at this warehouse
        var inventory = await _inventoryService.GetWarehouseInventoryAsync(id.Value);
        var totalUnits = inventory.Sum(i => i.Quantity);
        var utilization = warehouse.Capacity > 0 ? (double)totalUnits / warehouse.Capacity * 100 : 0;

        System.Console.WriteLine();
        System.Console.Write($"  {"Utilization:",-16} ");
        ConsoleHelper.PrintProgressBar(utilization);
        System.Console.WriteLine($"  ({totalUnits:N0} / {warehouse.Capacity:N0} units)");

        if (inventory.Count > 0)
        {
            ConsoleHelper.PrintSubHeader("Inventory at this Warehouse");
            var headers = new[] { "Product", "SKU", "Qty", "Status", "Value" };
            var widths = new[] { 22, 10, 8, 10, 12 };
            ConsoleHelper.PrintTableHeader(headers, widths);

            foreach (var item in inventory)
            {
                var level = _inventoryService.GetStockLevel(item.Quantity, item.Product.ReorderPoint);
                var indicator = ConsoleHelper.GetStockLevelIndicator(level);
                var color = ConsoleHelper.GetStockLevelColor(level);
                var value = item.Quantity * item.Product.UnitPrice;

                ConsoleHelper.PrintTableRow(new[]
                {
                    item.Product.Name, item.Product.SKU,
                    item.Quantity.ToString(), $"{indicator} {level}",
                    ConsoleHelper.FormatCurrency(value)
                }, widths, color);
            }

            var totalValue = inventory.Sum(i => i.Quantity * i.Product.UnitPrice);
            ConsoleHelper.PrintSeparator();
            System.Console.WriteLine($"  {"Total:",-34} {totalUnits,-8} {"",10} {ConsoleHelper.FormatCurrency(totalValue)}");
        }
        else
        {
            ConsoleHelper.Info("No inventory at this warehouse.");
        }

        ConsoleHelper.WaitForKey();
    }

    private async Task AddWarehouseAsync()
    {
        ConsoleHelper.PrintHeader("ADD NEW WAREHOUSE");

        var name = ConsoleHelper.Prompt("Warehouse Name");
        if (string.IsNullOrEmpty(name)) { ConsoleHelper.Error("Name is required."); ConsoleHelper.WaitForKey(); return; }

        var address = ConsoleHelper.Prompt("Address");
        var city = ConsoleHelper.Prompt("City");
        var state = ConsoleHelper.Prompt("State");
        var capacity = ConsoleHelper.PromptInt("Capacity (units)") ?? 10000;

        var warehouse = new Warehouse
        {
            Name = name,
            Address = address ?? string.Empty,
            City = city ?? string.Empty,
            State = state ?? string.Empty,
            Capacity = capacity
        };

        await _warehouseRepo.AddAsync(warehouse);
        ConsoleHelper.Success($"Warehouse '{name}' added with ID {warehouse.Id}.");
        ConsoleHelper.WaitForKey();
    }

    private async Task EditWarehouseAsync()
    {
        ConsoleHelper.PrintSubHeader("Edit Warehouse");
        var id = ConsoleHelper.PromptInt("Enter Warehouse ID");
        if (id == null) return;

        var warehouse = await _warehouseRepo.GetByIdAsync(id.Value);
        if (warehouse == null) { ConsoleHelper.Error("Warehouse not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.Info($"Editing: {warehouse.Name}. Press Enter to keep current value.");
        System.Console.WriteLine();

        var name = ConsoleHelper.Prompt($"Name [{warehouse.Name}]");
        if (!string.IsNullOrEmpty(name)) warehouse.Name = name;

        var address = ConsoleHelper.Prompt($"Address [{warehouse.Address}]");
        if (!string.IsNullOrEmpty(address)) warehouse.Address = address;

        var city = ConsoleHelper.Prompt($"City [{warehouse.City}]");
        if (!string.IsNullOrEmpty(city)) warehouse.City = city;

        var state = ConsoleHelper.Prompt($"State [{warehouse.State}]");
        if (!string.IsNullOrEmpty(state)) warehouse.State = state;

        var capacity = ConsoleHelper.PromptInt($"Capacity [{warehouse.Capacity}]");
        if (capacity.HasValue) warehouse.Capacity = capacity.Value;

        await _warehouseRepo.UpdateAsync(warehouse);
        ConsoleHelper.Success($"Warehouse '{warehouse.Name}' updated.");
        ConsoleHelper.WaitForKey();
    }

    private async Task DeactivateWarehouseAsync()
    {
        var id = ConsoleHelper.PromptInt("Enter Warehouse ID to deactivate");
        if (id == null) return;

        var warehouse = await _warehouseRepo.GetByIdAsync(id.Value);
        if (warehouse == null) { ConsoleHelper.Error("Warehouse not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.Warning($"Deactivate '{warehouse.Name}'? (y/n)");
        var confirm = ConsoleHelper.Prompt("Confirm");
        if (confirm?.ToLower() == "y")
        {
            warehouse.IsActive = false;
            await _warehouseRepo.UpdateAsync(warehouse);
            ConsoleHelper.Success($"Warehouse '{warehouse.Name}' deactivated.");
        }
        ConsoleHelper.WaitForKey();
    }
}
