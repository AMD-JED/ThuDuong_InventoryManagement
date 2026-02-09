using InventoryManagement.Core.Enums;
using InventoryManagement.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data;

/// <summary>
/// Seeds the database with sample data for demonstration and testing.
/// Includes 5 warehouses, 5 suppliers, 25 products, and sample transactions.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds the database if it's empty. Safe to call multiple times.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Products.AnyAsync())
            return; // Already seeded

        // ── Suppliers ──────────────────────────────────────────
        var suppliers = new List<Supplier>
        {
            new() { Name = "TechParts Global", ContactName = "James Chen", Email = "james@techparts.com", Phone = "555-0101", Address = "1200 Innovation Dr, San Jose, CA", LeadTimeDays = 5, Rating = 5 },
            new() { Name = "Office Supply Co.", ContactName = "Maria Garcia", Email = "maria@officesupply.com", Phone = "555-0102", Address = "340 Commerce Blvd, Chicago, IL", LeadTimeDays = 3, Rating = 4 },
            new() { Name = "Industrial Essentials", ContactName = "Robert Kim", Email = "robert@indessentials.com", Phone = "555-0103", Address = "780 Factory Ln, Detroit, MI", LeadTimeDays = 7, Rating = 4 },
            new() { Name = "SafetyFirst Equipment", ContactName = "Lisa Thompson", Email = "lisa@safetyfirst.com", Phone = "555-0104", Address = "55 Protection Ave, Houston, TX", LeadTimeDays = 4, Rating = 5 },
            new() { Name = "PackRight Solutions", ContactName = "David Park", Email = "david@packright.com", Phone = "555-0105", Address = "900 Shipping Way, Memphis, TN", LeadTimeDays = 6, Rating = 3 },
        };
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        // ── Warehouses ─────────────────────────────────────────
        var warehouses = new List<Warehouse>
        {
            new() { Name = "East Coast Hub", Address = "100 Distribution Dr", City = "Newark", State = "NJ", Capacity = 50000 },
            new() { Name = "West Coast Hub", Address = "200 Pacific Way", City = "Los Angeles", State = "CA", Capacity = 60000 },
            new() { Name = "Central Distribution", Address = "300 Heartland Blvd", City = "Dallas", State = "TX", Capacity = 45000 },
            new() { Name = "Southeast Fulfillment", Address = "400 Peachtree Ct", City = "Atlanta", State = "GA", Capacity = 35000 },
            new() { Name = "Midwest Storage", Address = "500 Lakeside Rd", City = "Chicago", State = "IL", Capacity = 40000 },
        };
        context.Warehouses.AddRange(warehouses);
        await context.SaveChangesAsync();

        // ── Products ───────────────────────────────────────────
        var products = new List<Product>
        {
            // Electronics (Supplier: TechParts Global)
            new() { SKU = "ELEC-001", Name = "Wireless Keyboard", Description = "Bluetooth mechanical keyboard with RGB backlight", Category = "Electronics", UnitPrice = 79.99m, ReorderPoint = 15, ReorderQuantity = 100, SupplierId = suppliers[0].Id },
            new() { SKU = "ELEC-002", Name = "USB-C Hub 7-Port", Description = "Multi-port USB-C adapter with HDMI and Ethernet", Category = "Electronics", UnitPrice = 49.99m, ReorderPoint = 20, ReorderQuantity = 150, SupplierId = suppliers[0].Id },
            new() { SKU = "ELEC-003", Name = "Wireless Mouse", Description = "Ergonomic wireless optical mouse", Category = "Electronics", UnitPrice = 34.99m, ReorderPoint = 25, ReorderQuantity = 200, SupplierId = suppliers[0].Id },
            new() { SKU = "ELEC-004", Name = "27\" Monitor", Description = "27-inch 4K IPS display with USB-C", Category = "Electronics", UnitPrice = 399.99m, ReorderPoint = 8, ReorderQuantity = 30, SupplierId = suppliers[0].Id },
            new() { SKU = "ELEC-005", Name = "Webcam HD 1080p", Description = "Full HD webcam with auto-focus and mic", Category = "Electronics", UnitPrice = 69.99m, ReorderPoint = 15, ReorderQuantity = 80, SupplierId = suppliers[0].Id },

            // Office Supplies (Supplier: Office Supply Co.)
            new() { SKU = "OFFC-001", Name = "Premium Copy Paper", Description = "A4 80gsm white copy paper, 500 sheets/ream", Category = "Office Supplies", UnitPrice = 8.99m, ReorderPoint = 100, ReorderQuantity = 500, SupplierId = suppliers[1].Id },
            new() { SKU = "OFFC-002", Name = "Gel Pen Set (12pk)", Description = "Assorted color gel pens, fine point", Category = "Office Supplies", UnitPrice = 12.49m, ReorderPoint = 50, ReorderQuantity = 200, SupplierId = suppliers[1].Id },
            new() { SKU = "OFFC-003", Name = "Desk Organizer", Description = "Mesh desk organizer with 5 compartments", Category = "Office Supplies", UnitPrice = 24.99m, ReorderPoint = 20, ReorderQuantity = 100, SupplierId = suppliers[1].Id },
            new() { SKU = "OFFC-004", Name = "Whiteboard 4x3ft", Description = "Magnetic dry-erase whiteboard with aluminum frame", Category = "Office Supplies", UnitPrice = 89.99m, ReorderPoint = 10, ReorderQuantity = 40, SupplierId = suppliers[1].Id },
            new() { SKU = "OFFC-005", Name = "Sticky Notes (24pk)", Description = "3x3 inch sticky notes, assorted colors", Category = "Office Supplies", UnitPrice = 15.99m, ReorderPoint = 60, ReorderQuantity = 300, SupplierId = suppliers[1].Id },

            // Industrial (Supplier: Industrial Essentials)
            new() { SKU = "INDL-001", Name = "Steel Shelving Unit", Description = "Heavy-duty 5-tier steel shelving, 72\"H", Category = "Industrial", UnitPrice = 149.99m, ReorderPoint = 5, ReorderQuantity = 25, SupplierId = suppliers[2].Id },
            new() { SKU = "INDL-002", Name = "Pallet Jack", Description = "Manual hydraulic pallet jack, 5500lb capacity", Category = "Industrial", UnitPrice = 299.99m, ReorderPoint = 3, ReorderQuantity = 10, SupplierId = suppliers[2].Id },
            new() { SKU = "INDL-003", Name = "LED Shop Light", Description = "48\" LED shop light, 5000 lumens", Category = "Industrial", UnitPrice = 39.99m, ReorderPoint = 15, ReorderQuantity = 60, SupplierId = suppliers[2].Id },
            new() { SKU = "INDL-004", Name = "Tool Chest", Description = "Rolling 5-drawer tool chest with lock", Category = "Industrial", UnitPrice = 219.99m, ReorderPoint = 5, ReorderQuantity = 20, SupplierId = suppliers[2].Id },
            new() { SKU = "INDL-005", Name = "Work Bench", Description = "Adjustable height workbench, 60\" wide", Category = "Industrial", UnitPrice = 349.99m, ReorderPoint = 3, ReorderQuantity = 15, SupplierId = suppliers[2].Id },

            // Safety (Supplier: SafetyFirst Equipment)
            new() { SKU = "SAFE-001", Name = "Safety Goggles", Description = "Anti-fog safety goggles, ANSI Z87.1", Category = "Safety", UnitPrice = 14.99m, ReorderPoint = 30, ReorderQuantity = 200, SupplierId = suppliers[3].Id },
            new() { SKU = "SAFE-002", Name = "Hard Hat", Description = "Type I/II hard hat with ratchet suspension", Category = "Safety", UnitPrice = 29.99m, ReorderPoint = 20, ReorderQuantity = 100, SupplierId = suppliers[3].Id },
            new() { SKU = "SAFE-003", Name = "First Aid Kit", Description = "OSHA-compliant 50-person first aid kit", Category = "Safety", UnitPrice = 49.99m, ReorderPoint = 10, ReorderQuantity = 50, SupplierId = suppliers[3].Id },
            new() { SKU = "SAFE-004", Name = "Hi-Vis Vest", Description = "ANSI Class 2 high-visibility safety vest", Category = "Safety", UnitPrice = 11.99m, ReorderPoint = 40, ReorderQuantity = 250, SupplierId = suppliers[3].Id },
            new() { SKU = "SAFE-005", Name = "Fire Extinguisher", Description = "ABC dry chemical fire extinguisher, 10lb", Category = "Safety", UnitPrice = 59.99m, ReorderPoint = 8, ReorderQuantity = 30, SupplierId = suppliers[3].Id },

            // Packaging (Supplier: PackRight Solutions)
            new() { SKU = "PACK-001", Name = "Shipping Boxes (25pk)", Description = "Medium corrugated shipping boxes 14x10x6\"", Category = "Packaging", UnitPrice = 29.99m, ReorderPoint = 40, ReorderQuantity = 200, SupplierId = suppliers[4].Id },
            new() { SKU = "PACK-002", Name = "Bubble Wrap Roll", Description = "Large bubble wrap, 12\" x 175ft roll", Category = "Packaging", UnitPrice = 24.99m, ReorderPoint = 30, ReorderQuantity = 150, SupplierId = suppliers[4].Id },
            new() { SKU = "PACK-003", Name = "Packing Tape (6pk)", Description = "Heavy-duty clear packing tape, 2\" x 110yds", Category = "Packaging", UnitPrice = 18.99m, ReorderPoint = 50, ReorderQuantity = 300, SupplierId = suppliers[4].Id },
            new() { SKU = "PACK-004", Name = "Stretch Wrap", Description = "Pallet stretch wrap, 18\" x 1500ft", Category = "Packaging", UnitPrice = 22.99m, ReorderPoint = 25, ReorderQuantity = 100, SupplierId = suppliers[4].Id },
            new() { SKU = "PACK-005", Name = "Shipping Labels (500)", Description = "Self-adhesive shipping labels 4x6\"", Category = "Packaging", UnitPrice = 16.99m, ReorderPoint = 35, ReorderQuantity = 200, SupplierId = suppliers[4].Id },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // ── Inventory Items ────────────────────────────────────
        var random = new Random(42); // Deterministic seed
        var inventoryItems = new List<InventoryItem>();

        foreach (var product in products)
        {
            // Distribute across 3-5 warehouses per product
            var warehouseCount = random.Next(3, 6);
            var selectedWarehouses = warehouses.OrderBy(_ => random.Next()).Take(warehouseCount);

            foreach (var warehouse in selectedWarehouses)
            {
                var quantity = random.Next(2, 120); // Some will be below reorder
                inventoryItems.Add(new InventoryItem
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = quantity,
                    LastUpdated = DateTime.UtcNow.AddDays(-random.Next(0, 30))
                });
            }
        }
        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();

        // ── Stock Transactions ─────────────────────────────────
        var transactionTypes = new[] { TransactionType.Received, TransactionType.Shipped, TransactionType.Transferred, TransactionType.Adjusted, TransactionType.Returned };
        var transactions = new List<StockTransaction>();

        for (int i = 0; i < 60; i++)
        {
            var product = products[random.Next(products.Count)];
            var warehouse = warehouses[random.Next(warehouses.Count)];
            var type = transactionTypes[random.Next(transactionTypes.Length)];
            var qty = type switch
            {
                TransactionType.Received => random.Next(20, 200),
                TransactionType.Shipped => -random.Next(5, 50),
                TransactionType.Transferred => random.Next(10, 80),
                TransactionType.Adjusted => random.Next(-20, 20),
                TransactionType.Returned => random.Next(1, 15),
                _ => 0
            };

            transactions.Add(new StockTransaction
            {
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                Type = type,
                Quantity = qty,
                ReferenceNumber = $"REF-{1000 + i:D4}",
                Notes = type switch
                {
                    TransactionType.Received => "Supplier delivery received",
                    TransactionType.Shipped => "Customer order fulfilled",
                    TransactionType.Transferred => "Inter-warehouse transfer",
                    TransactionType.Adjusted => "Inventory count adjustment",
                    TransactionType.Returned => "Customer return processed",
                    _ => ""
                },
                TransactionDate = DateTime.UtcNow.AddDays(-random.Next(0, 60)).AddHours(random.Next(8, 18)),
                DestinationWarehouseId = type == TransactionType.Transferred ? warehouses[random.Next(warehouses.Count)].Id : null
            });
        }
        context.StockTransactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }
}
