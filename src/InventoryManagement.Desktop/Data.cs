using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using InventoryManagement.Core.Models;
using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Desktop
{
    /// <summary>Opens the SQLite database (same relative path the old UI used).</summary>
    public static class Db
    {
        public const string File = "inventory.db";
        public static string FullPath => Path.GetFullPath(File);

        public static AppDbContext Create()
        {
            var o = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={File}").Options;
            return new AppDbContext(o);
        }

        /// <summary>Creates the DB, seeds the auto parts and makes sure one warehouse exists.</summary>
        public static void EnsureReady()
        {
            using var ctx = Create();
            ctx.Database.EnsureCreated();
            DataSeeder.SeedAsync(ctx).GetAwaiter().GetResult();
            if (!ctx.Warehouses.Any())
            {
                ctx.Warehouses.Add(new Warehouse
                {
                    Name = "Dépôt principal",
                    Address = "-",
                    Capacity = 10000,
                    IsActive = true
                });
                ctx.SaveChanges();
            }
        }
    }

    /// <summary>One line of the products grid (product + total stock).</summary>
    public sealed class ProductRow
    {
        public int Id { get; set; }
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Armoire { get; set; } = "";
        public string Marque { get; set; } = "";
        public string Emplacement { get; set; } = "";
        public string Etat { get; set; } = "";
        public int Stock { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public Dictionary<string, string> Custom { get; set; } = new();
    }

    public static class StockQueries
    {
        // Decimals are summed in memory because SQLite cannot aggregate/sort decimals in SQL.
        public static List<ProductRow> LoadRows(AppDbContext ctx, CustomColumnStore store)
        {
            var products = ctx.Products.AsNoTracking()
                .Include(p => p.InventoryItems)
                .Where(p => p.IsActive)
                .ToList();

            return products
                .Select(p => new ProductRow
                {
                    Id = p.Id,
                    SKU = p.SKU ?? "",
                    Name = p.Name ?? "",
                    Armoire = p.Category ?? "",
                    Marque = p.Marque ?? "",
                    Emplacement = p.Emplacement ?? "",
                    Etat = p.Etat ?? "",
                    Stock = p.InventoryItems?.Sum(i => i.Quantity) ?? 0,
                    PurchasePrice = p.PurchasePrice,
                    UnitPrice = p.UnitPrice,
                    ReorderPoint = p.ReorderPoint,
                    ReorderQuantity = p.ReorderQuantity,
                    Custom = store.ValuesFor(p.SKU ?? "")
                })
                .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.SKU)
                .ToList();
        }
    }

    /// <summary>
    /// Persists the user-defined columns (Product.CustomAttributes is not stored by EF)
    /// in custom_columns.json next to the database.
    /// </summary>
    public sealed class CustomColumnStore
    {
        private sealed class Model
        {
            public List<string> Columns { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> Values { get; set; } = new();
        }

        private readonly string _path;
        private Model _m = new();

        public CustomColumnStore()
        {
            _path = Path.Combine(Path.GetDirectoryName(Db.FullPath) ?? ".", "custom_columns.json");
            Load();
        }

        public IReadOnlyList<string> Columns => _m.Columns;

        public void Load()
        {
            try
            {
                _m = System.IO.File.Exists(_path)
                    ? JsonSerializer.Deserialize<Model>(System.IO.File.ReadAllText(_path)) ?? new Model()
                    : new Model();
            }
            catch { _m = new Model(); }
        }

        private void Save()
        {
            System.IO.File.WriteAllText(_path,
                JsonSerializer.Serialize(_m, new JsonSerializerOptions { WriteIndented = true }));
        }

        public bool AddColumn(string name)
        {
            name = name.Trim();
            if (name.Length == 0 || _m.Columns.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;
            _m.Columns.Add(name);
            Save();
            return true;
        }

        public void RemoveColumn(string name)
        {
            _m.Columns.Remove(name);
            foreach (var v in _m.Values.Values) v.Remove(name);
            Save();
        }

        public Dictionary<string, string> ValuesFor(string sku) =>
            _m.Values.TryGetValue(sku, out var v) ? new Dictionary<string, string>(v) : new();

        public void SetValues(string sku, IDictionary<string, string> values)
        {
            var clean = values.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                              .ToDictionary(kv => kv.Key, kv => kv.Value);
            if (clean.Count == 0) _m.Values.Remove(sku); else _m.Values[sku] = clean;
            Save();
        }

        public void RenameSku(string oldSku, string newSku)
        {
            if (oldSku == newSku || !_m.Values.TryGetValue(oldSku, out var v)) return;
            _m.Values.Remove(oldSku);
            _m.Values[newSku] = v;
            Save();
        }
    }

    public static class Num
    {
        public static bool TryDec(string s, out decimal v) =>
            decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out v);

        public static bool TryInt(string s, out int v) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    }

    /// <summary>Combo box item with an id.</summary>
    public sealed class Named
    {
        public int Id { get; }
        public string Text { get; }
        public Named(int id, string text) { Id = id; Text = text; }
        public override string ToString() => Text;
    }

    public interface IRefreshable
    {
        void RefreshData();
    }
}
