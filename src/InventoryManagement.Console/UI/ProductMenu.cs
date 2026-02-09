using InventoryManagement.Core.Interfaces;
using InventoryManagement.Core.Models;
using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Console.UI;

/// <summary>
/// Console UI for product management operations (CRUD).
/// </summary>
public class ProductMenu
{
    private readonly IProductRepository _productRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly AppDbContext _context;

    public ProductMenu(IProductRepository productRepo, IRepository<Supplier> supplierRepo, AppDbContext context)
    {
        _productRepo = productRepo;
        _supplierRepo = supplierRepo;
        _context = context;
    }

    /// <summary>Shows the product management menu.</summary>
    public async Task ShowAsync()
    {
        while (true)
        {
            var choice = ConsoleHelper.ShowMenu("PRODUCT MANAGEMENT", new[]
            {
                "View All Products",
                "View Product Details",
                "Add New Product",
                "Edit Product",
                "Delete Product",
                "View by Category",
                "Search Products",
                "Manage Suppliers"
            });

            switch (choice)
            {
                case 1: await ListProductsAsync(); break;
                case 2: await ViewProductDetailsAsync(); break;
                case 3: await AddProductAsync(); break;
                case 4: await EditProductAsync(); break;
                case 5: await DeleteProductAsync(); break;
                case 6: await ViewByCategoryAsync(); break;
                case 7: await SearchProductsAsync(); break;
                case 8: await ManageSuppliersAsync(); break;
                case 0: return;
                default:
                    ConsoleHelper.Error("Invalid option.");
                    ConsoleHelper.WaitForKey();
                    break;
            }
        }
    }

    private async Task ListProductsAsync()
    {
        ConsoleHelper.PrintHeader("ALL PRODUCTS");
        var products = await _productRepo.GetAllAsync();

        if (products.Count == 0)
        {
            ConsoleHelper.Info("No products found.");
            ConsoleHelper.WaitForKey();
            return;
        }

        var headers = new[] { "ID", "SKU", "Name", "Category", "Price", "Reorder Pt", "Supplier" };
        var widths = new[] { 4, 10, 22, 16, 10, 10, 18 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var p in products)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                p.Id.ToString(),
                p.SKU,
                p.Name,
                p.Category,
                ConsoleHelper.FormatCurrency(p.UnitPrice),
                p.ReorderPoint.ToString(),
                p.Supplier?.Name ?? "N/A"
            }, widths);
        }

        System.Console.WriteLine();
        ConsoleHelper.Info($"Total: {products.Count} products");
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewProductDetailsAsync()
    {
        ConsoleHelper.PrintSubHeader("View Product Details");
        var id = ConsoleHelper.PromptInt("Enter Product ID");
        if (id == null) return;

        var product = await _productRepo.GetWithSupplierAsync(id.Value);
        if (product == null)
        {
            ConsoleHelper.Error("Product not found.");
            ConsoleHelper.WaitForKey();
            return;
        }

        ConsoleHelper.PrintHeader($"PRODUCT: {product.Name}");
        System.Console.WriteLine($"  {"ID:",-18} {product.Id}");
        System.Console.WriteLine($"  {"SKU:",-18} {product.SKU}");
        System.Console.WriteLine($"  {"Name:",-18} {product.Name}");
        System.Console.WriteLine($"  {"Description:",-18} {product.Description}");
        System.Console.WriteLine($"  {"Category:",-18} {product.Category}");
        System.Console.WriteLine($"  {"Unit Price:",-18} {ConsoleHelper.FormatCurrency(product.UnitPrice)}");
        System.Console.WriteLine($"  {"Reorder Point:",-18} {product.ReorderPoint}");
        System.Console.WriteLine($"  {"Reorder Qty:",-18} {product.ReorderQuantity}");
        System.Console.WriteLine($"  {"Supplier:",-18} {product.Supplier?.Name ?? "N/A"}");
        System.Console.WriteLine($"  {"Created:",-18} {product.CreatedAt:yyyy-MM-dd HH:mm}");
        System.Console.WriteLine($"  {"Active:",-18} {(product.IsActive ? "Yes" : "No")}");

        ConsoleHelper.WaitForKey();
    }

    private async Task AddProductAsync()
    {
        ConsoleHelper.PrintHeader("ADD NEW PRODUCT");

        var sku = ConsoleHelper.Prompt("SKU");
        if (string.IsNullOrEmpty(sku)) { ConsoleHelper.Error("SKU is required."); ConsoleHelper.WaitForKey(); return; }

        var existing = await _productRepo.GetBySKUAsync(sku);
        if (existing != null) { ConsoleHelper.Error($"SKU '{sku}' already exists."); ConsoleHelper.WaitForKey(); return; }

        var name = ConsoleHelper.Prompt("Name");
        if (string.IsNullOrEmpty(name)) { ConsoleHelper.Error("Name is required."); ConsoleHelper.WaitForKey(); return; }

        var description = ConsoleHelper.Prompt("Description");
        var category = ConsoleHelper.Prompt("Category");
        var price = ConsoleHelper.PromptDecimal("Unit Price");
        var reorderPoint = ConsoleHelper.PromptInt("Reorder Point (default: 10)") ?? 10;
        var reorderQty = ConsoleHelper.PromptInt("Reorder Quantity (default: 50)") ?? 50;

        // Show suppliers
        var suppliers = await _supplierRepo.GetAllAsync();
        if (suppliers.Count > 0)
        {
            System.Console.WriteLine();
            ConsoleHelper.PrintSubHeader("Available Suppliers");
            foreach (var s in suppliers)
                System.Console.WriteLine($"  [{s.Id}] {s.Name}");
        }
        var supplierId = ConsoleHelper.PromptInt("Supplier ID (optional, press Enter to skip)");

        var product = new Product
        {
            SKU = sku,
            Name = name,
            Description = description ?? string.Empty,
            Category = category ?? "General",
            UnitPrice = price ?? 0,
            ReorderPoint = reorderPoint,
            ReorderQuantity = reorderQty,
            SupplierId = supplierId
        };

        await _productRepo.AddAsync(product);
        ConsoleHelper.Success($"Product '{name}' (SKU: {sku}) added successfully with ID {product.Id}.");
        ConsoleHelper.WaitForKey();
    }

    private async Task EditProductAsync()
    {
        ConsoleHelper.PrintSubHeader("Edit Product");
        var id = ConsoleHelper.PromptInt("Enter Product ID to edit");
        if (id == null) return;

        var product = await _productRepo.GetByIdAsync(id.Value);
        if (product == null) { ConsoleHelper.Error("Product not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.Info($"Editing: {product.Name} (SKU: {product.SKU})");
        ConsoleHelper.Info("Press Enter to keep current value.");
        System.Console.WriteLine();

        var name = ConsoleHelper.Prompt($"Name [{product.Name}]");
        if (!string.IsNullOrEmpty(name)) product.Name = name;

        var desc = ConsoleHelper.Prompt($"Description [{product.Description}]");
        if (!string.IsNullOrEmpty(desc)) product.Description = desc;

        var cat = ConsoleHelper.Prompt($"Category [{product.Category}]");
        if (!string.IsNullOrEmpty(cat)) product.Category = cat;

        var price = ConsoleHelper.PromptDecimal($"Unit Price [{product.UnitPrice}]");
        if (price.HasValue) product.UnitPrice = price.Value;

        var rp = ConsoleHelper.PromptInt($"Reorder Point [{product.ReorderPoint}]");
        if (rp.HasValue) product.ReorderPoint = rp.Value;

        var rq = ConsoleHelper.PromptInt($"Reorder Qty [{product.ReorderQuantity}]");
        if (rq.HasValue) product.ReorderQuantity = rq.Value;

        await _productRepo.UpdateAsync(product);
        ConsoleHelper.Success($"Product '{product.Name}' updated successfully.");
        ConsoleHelper.WaitForKey();
    }

    private async Task DeleteProductAsync()
    {
        ConsoleHelper.PrintSubHeader("Delete Product");
        var id = ConsoleHelper.PromptInt("Enter Product ID to delete");
        if (id == null) return;

        var product = await _productRepo.GetByIdAsync(id.Value);
        if (product == null) { ConsoleHelper.Error("Product not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.Warning($"Are you sure you want to deactivate '{product.Name}'? (y/n)");
        var confirm = ConsoleHelper.Prompt("Confirm");
        if (confirm?.ToLower() == "y")
        {
            product.IsActive = false;
            await _productRepo.UpdateAsync(product);
            ConsoleHelper.Success($"Product '{product.Name}' has been deactivated.");
        }
        else
        {
            ConsoleHelper.Info("Deletion cancelled.");
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewByCategoryAsync()
    {
        ConsoleHelper.PrintSubHeader("View Products by Category");

        var categories = await _productRepo.GetCategoriesAsync();
        if (categories.Count == 0) { ConsoleHelper.Info("No categories found."); ConsoleHelper.WaitForKey(); return; }

        for (int i = 0; i < categories.Count; i++)
            System.Console.WriteLine($"  [{i + 1}] {categories[i]}");

        var choice = ConsoleHelper.PromptInt("Select category");
        if (choice == null || choice < 1 || choice > categories.Count) return;

        var products = await _productRepo.GetByCategoryAsync(categories[choice.Value - 1]);
        ConsoleHelper.PrintHeader($"CATEGORY: {categories[choice.Value - 1]}");

        var headers = new[] { "ID", "SKU", "Name", "Price", "Reorder Pt" };
        var widths = new[] { 4, 10, 25, 10, 10 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var p in products)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                p.Id.ToString(), p.SKU, p.Name,
                ConsoleHelper.FormatCurrency(p.UnitPrice), p.ReorderPoint.ToString()
            }, widths);
        }

        ConsoleHelper.Info($"Total: {products.Count} products in this category");
        ConsoleHelper.WaitForKey();
    }

    /// <summary>Search products by name, SKU, or description.</summary>
    public async Task SearchProductsAsync()
    {
        ConsoleHelper.PrintHeader("SEARCH PRODUCTS");
        var term = ConsoleHelper.Prompt("Search term (name, SKU, or description)");
        if (string.IsNullOrEmpty(term)) return;

        var results = await _productRepo.SearchAsync(term);
        if (results.Count == 0)
        {
            ConsoleHelper.Info($"No products found matching '{term}'.");
            ConsoleHelper.WaitForKey();
            return;
        }

        ConsoleHelper.PrintSubHeader($"Search Results for '{term}'");
        var headers = new[] { "ID", "SKU", "Name", "Category", "Price", "Supplier" };
        var widths = new[] { 4, 10, 22, 16, 10, 18 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var p in results)
        {
            ConsoleHelper.PrintTableRow(new[]
            {
                p.Id.ToString(), p.SKU, p.Name, p.Category,
                ConsoleHelper.FormatCurrency(p.UnitPrice), p.Supplier?.Name ?? "N/A"
            }, widths);
        }

        ConsoleHelper.Info($"Found {results.Count} product(s).");
        ConsoleHelper.WaitForKey();
    }

    private async Task ManageSuppliersAsync()
    {
        while (true)
        {
            var choice = ConsoleHelper.ShowMenu("SUPPLIER MANAGEMENT", new[]
            {
                "View All Suppliers",
                "Add New Supplier",
                "Edit Supplier",
                "View Supplier Products"
            });

            switch (choice)
            {
                case 1: await ListSuppliersAsync(); break;
                case 2: await AddSupplierAsync(); break;
                case 3: await EditSupplierAsync(); break;
                case 4: await ViewSupplierProductsAsync(); break;
                case 0: return;
            }
        }
    }

    private async Task ListSuppliersAsync()
    {
        ConsoleHelper.PrintHeader("ALL SUPPLIERS");
        var suppliers = await _supplierRepo.GetAllAsync();

        var headers = new[] { "ID", "Name", "Contact", "Email", "Phone", "Rating", "Lead Time" };
        var widths = new[] { 4, 22, 16, 24, 12, 6, 10 };
        ConsoleHelper.PrintTableHeader(headers, widths);

        foreach (var s in suppliers)
        {
            var stars = new string('★', s.Rating) + new string('☆', 5 - s.Rating);
            ConsoleHelper.PrintTableRow(new[]
            {
                s.Id.ToString(), s.Name, s.ContactName, s.Email,
                s.Phone, stars, $"{s.LeadTimeDays} days"
            }, widths);
        }
        ConsoleHelper.WaitForKey();
    }

    private async Task AddSupplierAsync()
    {
        ConsoleHelper.PrintHeader("ADD NEW SUPPLIER");

        var name = ConsoleHelper.Prompt("Company Name");
        if (string.IsNullOrEmpty(name)) { ConsoleHelper.Error("Name is required."); ConsoleHelper.WaitForKey(); return; }

        var contact = ConsoleHelper.Prompt("Contact Person");
        var email = ConsoleHelper.Prompt("Email");
        var phone = ConsoleHelper.Prompt("Phone");
        var address = ConsoleHelper.Prompt("Address");
        var leadTime = ConsoleHelper.PromptInt("Lead Time (days, default: 7)") ?? 7;
        var rating = ConsoleHelper.PromptInt("Rating (1-5, default: 3)") ?? 3;
        rating = Math.Clamp(rating, 1, 5);

        var supplier = new Supplier
        {
            Name = name,
            ContactName = contact ?? string.Empty,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            Address = address ?? string.Empty,
            LeadTimeDays = leadTime,
            Rating = rating
        };

        await _supplierRepo.AddAsync(supplier);
        ConsoleHelper.Success($"Supplier '{name}' added with ID {supplier.Id}.");
        ConsoleHelper.WaitForKey();
    }

    private async Task EditSupplierAsync()
    {
        ConsoleHelper.PrintSubHeader("Edit Supplier");
        var id = ConsoleHelper.PromptInt("Enter Supplier ID");
        if (id == null) return;

        var supplier = await _supplierRepo.GetByIdAsync(id.Value);
        if (supplier == null) { ConsoleHelper.Error("Supplier not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.Info($"Editing: {supplier.Name}. Press Enter to keep current value.");
        System.Console.WriteLine();

        var name = ConsoleHelper.Prompt($"Name [{supplier.Name}]");
        if (!string.IsNullOrEmpty(name)) supplier.Name = name;

        var contact = ConsoleHelper.Prompt($"Contact [{supplier.ContactName}]");
        if (!string.IsNullOrEmpty(contact)) supplier.ContactName = contact;

        var email = ConsoleHelper.Prompt($"Email [{supplier.Email}]");
        if (!string.IsNullOrEmpty(email)) supplier.Email = email;

        var phone = ConsoleHelper.Prompt($"Phone [{supplier.Phone}]");
        if (!string.IsNullOrEmpty(phone)) supplier.Phone = phone;

        var rating = ConsoleHelper.PromptInt($"Rating [{supplier.Rating}]");
        if (rating.HasValue) supplier.Rating = Math.Clamp(rating.Value, 1, 5);

        await _supplierRepo.UpdateAsync(supplier);
        ConsoleHelper.Success($"Supplier '{supplier.Name}' updated.");
        ConsoleHelper.WaitForKey();
    }

    private async Task ViewSupplierProductsAsync()
    {
        var id = ConsoleHelper.PromptInt("Enter Supplier ID");
        if (id == null) return;

        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id.Value);

        if (supplier == null) { ConsoleHelper.Error("Supplier not found."); ConsoleHelper.WaitForKey(); return; }

        ConsoleHelper.PrintHeader($"PRODUCTS FROM: {supplier.Name}");

        if (supplier.Products.Count == 0)
        {
            ConsoleHelper.Info("No products linked to this supplier.");
        }
        else
        {
            var headers = new[] { "ID", "SKU", "Name", "Category", "Price" };
            var widths = new[] { 4, 10, 25, 16, 10 };
            ConsoleHelper.PrintTableHeader(headers, widths);

            foreach (var p in supplier.Products)
            {
                ConsoleHelper.PrintTableRow(new[]
                {
                    p.Id.ToString(), p.SKU, p.Name, p.Category,
                    ConsoleHelper.FormatCurrency(p.UnitPrice)
                }, widths);
            }
        }
        ConsoleHelper.WaitForKey();
    }
}
