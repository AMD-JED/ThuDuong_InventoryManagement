using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // أضف هذه المكتبة

namespace InventoryManagement.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? SKU { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<InventoryItem>? InventoryItems { get; set; }
        public ICollection<StockTransaction>? Transactions { get; set; }

        // خصائص إكسل الخاصة بقطع الغيار
        public string? Marque { get; set; }
        public string? Emplacement { get; set; }
        public string? Etat { get; set; }
        public decimal PurchasePrice { get; set; }

        // ⭐ هذا هو الحل: إضافة NotMapped لكي يتجاهلها قاعدة البيانات
        [NotMapped]
        public Dictionary<string, string> CustomAttributes { get; set; } = new Dictionary<string, string>();
    }
}