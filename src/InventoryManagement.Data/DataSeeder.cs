using InventoryManagement.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Products.AnyAsync())
            {
                return; // البيانات موجودة مسبقاً
            }

            // قائمة قطع الغيار مطابقة لصورة الإكسل تماماً مع توليد SKU احترافي
            var products = new List<Product>
            {
                new Product { SKU = "REF-03L906023-LF", Name = "PCR 2.1", Category = "Armoire 01", Marque = "SEAT", Emplacement = "A1-AV1", Etat = "OUVERT", PurchasePrice = 100.00m, UnitPrice = 150.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-LI", Name = "PCR 2.1", Category = "Armoire 01", Marque = "SEAT", Emplacement = "A1-AV2", Etat = "OUVERT", PurchasePrice = 100.00m, UnitPrice = 150.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-LG", Name = "PCR 2.1", Category = "Armoire 01", Marque = "SEAT", Emplacement = "A1-AV1", Etat = "CASSE", PurchasePrice = 80.00m, UnitPrice = 120.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-PE", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AR5", Etat = "OUVERT", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-NC", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "H43", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-PE2", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "OUVERT", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MS", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV1", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-LG2", Name = "PCR 2.1", Category = "Armoire 01", Marque = "SEAT", Emplacement = "A1-AV1", Etat = "OUVERT", PurchasePrice = 100.00m, UnitPrice = 150.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L907425-C", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "OUVERT", PurchasePrice = 110.00m, UnitPrice = 160.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MQ", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-FN", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "OUVERT", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L907425-SC", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "CASSE", PurchasePrice = 110.00m, UnitPrice = 160.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MN", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AV2", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-JH", Name = "PCR 2.1", Category = "Armoire 01", Marque = "SKODA", Emplacement = "A1-AV2", Etat = "CASSE", PurchasePrice = 95.00m, UnitPrice = 140.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MG", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AR2", Etat = "OUVERT", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MA", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AR2", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-03L906023-MB", Name = "PCR 2.1", Category = "Armoire 01", Marque = "VW", Emplacement = "A1-AR2", Etat = "CASSE", PurchasePrice = 90.00m, UnitPrice = 130.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-0281032456", Name = "EDC17c60", Category = "Armoire 02", Marque = "PARTNER", Emplacement = "A2-AR1", Etat = "OUVERT", PurchasePrice = 200.00m, UnitPrice = 300.00m, ReorderPoint = 1 },
                new Product { SKU = "REF-9663376780", Name = "BSI C5", Category = "Armoire 02", Marque = "C5", Emplacement = "A2-AR2", Etat = "OUVERT", PurchasePrice = 150.00m, UnitPrice = 220.00m, ReorderPoint = 1 }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}