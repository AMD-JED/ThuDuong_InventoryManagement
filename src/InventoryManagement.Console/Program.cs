using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;
using InventoryManagement.Data;
using InventoryManagement.Data.Repositories;
using InventoryManagement.Services;
using InventoryManagement.Console.UI;

// ─────────────────────────────────────────────────────────────
//  Inventory Management System
//  Multi-Location Warehouse Tracking with KPI Dashboard
//  Author: Thu Duong
// ─────────────────────────────────────────────────────────────

var services = new ServiceCollection();

// Database — SQLite for portability
var dbPath = Path.Combine(AppContext.BaseDirectory, "inventory.db");
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repositories
services.AddScoped<IProductRepository, ProductRepository>();
services.AddScoped<IRepository<Warehouse>, Repository<Warehouse>>();
services.AddScoped<IRepository<Supplier>, Repository<Supplier>>();

// Services
services.AddScoped<IInventoryService, InventoryService>();
services.AddScoped<IReorderService, ReorderService>();
services.AddScoped<IReportingService, ReportingService>();

// UI
services.AddScoped<MainMenu>();
services.AddScoped<ProductMenu>();
services.AddScoped<WarehouseMenu>();
services.AddScoped<InventoryMenu>();
services.AddScoped<ReportsMenu>();

var serviceProvider = services.BuildServiceProvider();

// Initialize database and seed data
using (var scope = serviceProvider.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(context);
}

// Run application
using (var scope = serviceProvider.CreateScope())
{
    var mainMenu = scope.ServiceProvider.GetRequiredService<MainMenu>();
    await mainMenu.RunAsync();
}
