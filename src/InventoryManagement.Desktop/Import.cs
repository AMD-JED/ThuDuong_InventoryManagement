using System.Globalization;
using System.Text;
using InventoryManagement.Core.Models;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Desktop
{
    public sealed class ImportRow
    {
        public int Line { get; set; }
        public string Sku { get; set; } = "";
        public bool IsNew { get; set; }
        // canonical key (name, armoire, ...) -> raw text; only keys present in the file
        public Dictionary<string, string> Fields { get; } = new();
        public Dictionary<string, string> Extra { get; } = new();
        public int? Stock { get; set; }
    }

    public sealed class ImportPlan
    {
        public List<ImportRow> Rows { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> ExtraColumns { get; } = new();
        public int NewCount => Rows.Count(r => r.IsNew);
        public int UpdateCount => Rows.Count(r => !r.IsNew);
        public bool HasStock => Rows.Any(r => r.Stock != null);
    }

    /// <summary>
    /// Imports parts from a CSV file (Excel: "Save as CSV"). Columns are matched by name,
    /// unknown columns become custom columns. Files created by "Export CSV" re-import as-is.
    /// </summary>
    public static class CsvImporter
    {
        static CsvImporter() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        private static readonly Dictionary<string, string[]> Aliases = new()
        {
            ["sku"] = new[] { "sku", "reference", "referencesku", "ref", "code" },
            ["name"] = new[] { "designation", "designationarticle", "name", "nom", "article", "piece" },
            ["armoire"] = new[] { "armoire", "category", "categorie", "rayon" },
            ["marque"] = new[] { "marque", "brand" },
            ["emplacement"] = new[] { "emplacement", "location", "localisation" },
            ["etat"] = new[] { "etat", "status", "statut" },
            ["stock"] = new[] { "stock", "quantite", "qte", "quantity", "qty" },
            ["purchase"] = new[] { "prixachat", "purchaseprice", "achat" },
            ["sale"] = new[] { "prixvente", "unitprice", "vente", "prix", "price" },
            ["reorder"] = new[] { "seuil", "seuilreappro", "seuildereappro", "reorderpoint" },
        };

        private static string Norm(string s)
        {
            var d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        private static string Decode(byte[] bytes)
        {
            try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
            catch (DecoderFallbackException) { return Encoding.GetEncoding(1252).GetString(bytes); }
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var firstLine = text.Split('\n').FirstOrDefault(l => l.Trim().Length > 0) ?? "";
            char delim = new[] { ';', ',', '\t' }.OrderByDescending(c => firstLine.Count(x => x == c)).First();

            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else quoted = false;
                    }
                    else cell.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == delim) { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\r') { /* ignore */ }
                else if (c == '\n')
                {
                    row.Add(cell.ToString()); cell.Clear();
                    if (row.Any(x => x.Trim().Length > 0)) rows.Add(row);
                    row = new List<string>();
                }
                else cell.Append(c);
            }
            row.Add(cell.ToString());
            if (row.Any(x => x.Trim().Length > 0)) rows.Add(row);
            return rows;
        }

        // Excel keeps the file locked while it is open: read it with sharing allowed.
        // (Only what was last SAVED in Excel is read, not unsaved edits.)
        private static byte[] ReadShared(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            return ms.ToArray();
        }

        public static ImportPlan Parse(string path)
        {
            var plan = new ImportPlan();
            var table = ParseCsv(Decode(ReadShared(path)));
            if (table.Count < 2) { plan.Errors.Add("Le fichier est vide ou ne contient pas de lignes de données."); return plan; }

            // map each column index -> canonical key, or "extra:<header>"
            var header = table[0];
            var map = new Dictionary<int, string>();
            for (int i = 0; i < header.Count; i++)
            {
                var raw = header[i].Trim();
                if (raw.Length == 0) continue;
                var n = Norm(raw);
                var key = Aliases.FirstOrDefault(a => a.Value.Contains(n)).Key;
                if (key != null && !map.ContainsValue(key)) map[i] = key;
                else
                {
                    map[i] = "extra:" + raw;
                    if (!plan.ExtraColumns.Contains(raw, StringComparer.OrdinalIgnoreCase)) plan.ExtraColumns.Add(raw);
                }
            }
            if (!map.ContainsValue("sku"))
            {
                plan.Errors.Add("Colonne SKU / Référence introuvable dans la première ligne.");
                return plan;
            }

            HashSet<string> existing;
            using (var ctx = Db.Create())
                existing = ctx.Products.Select(p => p.SKU).ToList().Where(s => s != null).Select(s => s!).ToHashSet();

            var seen = new HashSet<string>();
            for (int r = 1; r < table.Count; r++)
            {
                int line = r + 1;
                var cells = table[r];
                var row = new ImportRow { Line = line };
                foreach (var (idx, key) in map)
                {
                    var val = idx < cells.Count ? cells[idx].Trim() : "";
                    if (key.StartsWith("extra:")) { if (val.Length > 0) row.Extra[key.Substring(6)] = val; }
                    else if (key == "sku") row.Sku = val;
                    else row.Fields[key] = val;
                }

                string? err = null;
                if (row.Sku.Length == 0) err = "SKU vide";
                else if (!seen.Add(row.Sku)) err = $"SKU « {row.Sku} » en double dans le fichier";
                else
                {
                    row.IsNew = !existing.Contains(row.Sku);
                    if (row.IsNew && string.IsNullOrWhiteSpace(row.Fields.GetValueOrDefault("name")))
                        err = $"nouvelle pièce « {row.Sku} » sans désignation";
                    foreach (var k in new[] { "purchase", "sale" })
                        if (err == null && row.Fields.TryGetValue(k, out var v) && v.Length > 0 && (!Num.TryDec(v, out var d) || d < 0))
                            err = $"prix invalide « {v} »";
                    if (err == null && row.Fields.TryGetValue("reorder", out var rv) && rv.Length > 0 && (!Num.TryInt(rv, out var ri) || ri < 0))
                        err = $"seuil invalide « {rv} »";
                    if (err == null && row.Fields.TryGetValue("stock", out var sv) && sv.Length > 0)
                    {
                        if (!Num.TryInt(sv, out var si) || si < 0) err = $"stock invalide « {sv} »";
                        else row.Stock = si;
                    }
                }
                if (err != null) plan.Errors.Add($"Ligne {line} : {err}");
                else plan.Rows.Add(row);
            }
            return plan;
        }

        /// <returns>(created, updated, stock adjustments)</returns>
        public static Task<(int Created, int Updated, int StockSet)> ApplyAsync(
            ImportPlan plan, int warehouseId, CustomColumnStore store)
        {
            return Task.Run(async () =>
            {
                int created = 0, updated = 0, stockSet = 0;
                using var ctx = Db.Create();
                await using var tx = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var bySku = new Dictionary<string, Product>();
                    foreach (var p in await ctx.Products.ToListAsync())
                        if (p.SKU != null) bySku.TryAdd(p.SKU, p);

                    var touched = new List<(ImportRow Row, Product Prod)>();
                    foreach (var r in plan.Rows)
                    {
                        string F(string k) => r.Fields.GetValueOrDefault(k) ?? "";
                        bool Has(string k) => r.Fields.TryGetValue(k, out var v) && v.Length > 0;

                        if (!bySku.TryGetValue(r.Sku, out var p))
                        {
                            p = new Product { SKU = r.Sku, IsActive = true, ReorderPoint = 1, Etat = "OUVERT" };
                            ctx.Products.Add(p);
                            bySku[r.Sku] = p;
                            created++;
                        }
                        else { p.IsActive = true; updated++; }

                        if (Has("name")) p.Name = F("name");
                        if (Has("armoire")) p.Category = F("armoire");
                        if (Has("marque")) p.Marque = F("marque");
                        if (Has("emplacement")) p.Emplacement = F("emplacement");
                        if (Has("etat")) p.Etat = F("etat");
                        if (Has("purchase")) { Num.TryDec(F("purchase"), out var d); p.PurchasePrice = d; }
                        if (Has("sale")) { Num.TryDec(F("sale"), out var d); p.UnitPrice = d; }
                        if (Has("reorder")) { Num.TryInt(F("reorder"), out var i); p.ReorderPoint = i; }
                        touched.Add((r, p));
                    }
                    await ctx.SaveChangesAsync();   // assigns the Ids of new products

                    if (plan.HasStock)
                    {
                        var current = await ctx.InventoryItems.Where(i => i.WarehouseId == warehouseId)
                            .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);
                        var svc = new InventoryService(ctx);
                        foreach (var (row, prod) in touched.Where(t => t.Row.Stock != null))
                        {
                            int target = row.Stock!.Value;
                            if (current.TryGetValue(prod.Id, out var q) && q == target) continue;
                            await svc.AdjustStockAsync(prod.Id, warehouseId, target, "Import CSV");
                            stockSet++;
                        }
                    }
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                // custom columns live in a JSON file, not in the DB
                foreach (var c in plan.ExtraColumns) store.AddColumn(c);
                var values = new Dictionary<string, Dictionary<string, string>>();
                foreach (var r in plan.Rows.Where(r => r.Extra.Count > 0)) values[r.Sku] = r.Extra;
                store.MergeValues(values);

                return (created, updated, stockSet);
            });
        }
    }
}
