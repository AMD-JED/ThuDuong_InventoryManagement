using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;

namespace InventoryManagement.Data;

/// <summary>
/// Entity Framework Core database context for the Inventory Management System.
/// Configured for SQLite with normalized schema supporting multi-location tracking.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<ReorderAlert> ReorderAlerts => Set<ReorderAlert>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.HasIndex(p => p.Category);
            entity.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Supplier)
                  .WithMany(s => s.Products)
                  .HasForeignKey(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Warehouse configuration
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.Name).IsUnique();
        });

        // InventoryItem — composite unique index on Product + Warehouse
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.ProductId, i.WarehouseId }).IsUnique();
            entity.HasOne(i => i.Product)
                  .WithMany(p => p.InventoryItems)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Warehouse)
                  .WithMany(w => w.InventoryItems)
                  .HasForeignKey(i => i.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Name);
        });

        // StockTransaction configuration
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TransactionDate);
            entity.HasIndex(t => t.Type);
            entity.HasOne(t => t.Product)
                  .WithMany(p => p.Transactions)
                  .HasForeignKey(t => t.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Warehouse)
                  .WithMany()
                  .HasForeignKey(t => t.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ReorderAlert configuration
        modelBuilder.Entity<ReorderAlert>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.IsResolved);
            entity.HasOne(a => a.Product)
                  .WithMany()
                  .HasForeignKey(a => a.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Warehouse)
                  .WithMany()
                  .HasForeignKey(a => a.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
