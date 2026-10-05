using System.Collections;
using System.Text;

namespace InventoryManagement.Desktop
{
    public sealed class ProductsView : UserControl, IRefreshable
    {
        private const string All = "(Tous)";
        private readonly CustomColumnStore _store = new();
        private readonly DataGridView _grid = new();
        private readonly TextBox _search = new();
        private readonly ComboBox _fArmoire = new(), _fMarque = new(), _fEtat = new();
        private readonly Label _status = new();
        private List<ProductRow> _all = new();
        private List<ProductRow> _view = new();
        private string? _sortProp;
        private bool _sortAsc = true;
        private bool _loading;

        public event Action? DataChanged;

        public ProductsView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;

            // ----- grid -----
            Theme.StyleGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(Theme.Col("Référence (SKU)", nameof(ProductRow.SKU), 130));
            _grid.Columns.Add(Theme.Col("Désignation", nameof(ProductRow.Name), 110));
            _grid.Columns.Add(Theme.Col("Armoire", nameof(ProductRow.Armoire), 80));
            _grid.Columns.Add(Theme.Col("Marque", nameof(ProductRow.Marque), 80));
            _grid.Columns.Add(Theme.Col("Emplacement", nameof(ProductRow.Emplacement), 85));
            _grid.Columns.Add(Theme.Col("État", nameof(ProductRow.Etat), 70));
            _grid.Columns.Add(Theme.Col("Stock", nameof(ProductRow.Stock), 55, null, true));
            _grid.Columns.Add(Theme.Col("Prix Achat", nameof(ProductRow.PurchasePrice), 75, "N2", true));
            _grid.Columns.Add(Theme.Col("Prix Vente", nameof(ProductRow.UnitPrice), 75, "N2", true));
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };
            _grid.ColumnHeaderMouseClick += Grid_HeaderClick;
            _grid.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; } };

            // ----- status bar -----
            _status.Dock = DockStyle.Bottom;
            _status.Height = 30;
            _status.ForeColor = Theme.Muted;
            _status.Font = Theme.Base;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Padding = new Padding(4, 0, 0, 0);

            // ----- toolbar (filters) -----
            _search.Width = 270;
            _search.PlaceholderText = "🔍 SKU, désignation, marque, emplacement...";
            Theme.StyleInput(_search);
            foreach (var cb in new[] { _fArmoire, _fMarque, _fEtat })
            {
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
                cb.Width = 130;
                Theme.StyleInput(cb);
            }
            _search.TextChanged += (_, _) => ApplyFilter();
            _fArmoire.SelectedIndexChanged += (_, _) => ApplyFilter();
            _fMarque.SelectedIndexChanged += (_, _) => ApplyFilter();
            _fEtat.SelectedIndexChanged += (_, _) => ApplyFilter();

            var filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 46, Padding = new Padding(0, 6, 0, 0), WrapContents = false
            };
            filters.Controls.Add(_search);
            foreach (var (txt, cb) in new[] { ("Armoire", _fArmoire), ("Marque", _fMarque), ("État", _fEtat) })
            {
                var l = Theme.MakeLabel(txt, Theme.Base, Theme.Muted);
                l.Margin = new Padding(14, 7, 4, 0);
                cb.Margin = new Padding(0, 3, 0, 0);
                filters.Controls.Add(l);
                filters.Controls.Add(cb);
            }
            var clear = Theme.MakeButton("✖ Effacer", Theme.Neutral, (_, _) => ClearFilters(), 90);
            clear.Margin = new Padding(14, 0, 0, 0);
            filters.Controls.Add(clear);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 6, 0, 0), WrapContents = false
            };
            actions.Controls.Add(Theme.MakeButton("➕ Ajouter", Theme.Good, (_, _) => AddProduct(), 110));
            actions.Controls.Add(Theme.MakeButton("✏️ Modifier", Theme.Accent, (_, _) => EditSelected(), 110));
            actions.Controls.Add(Theme.MakeButton("🗑 Supprimer", Theme.Danger, (_, _) => DeleteSelected(), 120));
            var sep = new Panel { Width = 18, Height = 1 };
            actions.Controls.Add(sep);
            actions.Controls.Add(Theme.MakeButton("➕ Colonne", Theme.Neutral, (_, _) => AddColumn(), 110));
            actions.Controls.Add(Theme.MakeButton("➖ Colonne", Theme.Neutral, (_, _) => RemoveColumn(), 110));
            actions.Controls.Add(Theme.MakeButton("📤 Export CSV", Theme.Neutral, (_, _) => ExportCsv(), 125));

            var header = Theme.MakeLabel("📦 Gestion des pièces", Theme.Title, Color.White);
            header.AutoSize = false;
            header.Dock = DockStyle.Top;
            header.Height = 48;

            // Fill control first, then docked ones (last added = outermost).
            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(filters);
            Controls.Add(actions);
            Controls.Add(header);
            Padding = new Padding(16, 12, 16, 8);

            RefreshData();
        }

        public void FocusSearch()
        {
            _search.Focus();
            _search.SelectAll();
        }

        public void AddNew() => AddProduct();

        // ================= data =================
        public void RefreshData()
        {
            _loading = true;
            try
            {
                _store.Load();
                using var ctx = Db.Create();
                _all = StockQueries.LoadRows(ctx, _store);

                FillCombo(_fArmoire, _all.Select(r => r.Armoire));
                FillCombo(_fMarque, _all.Select(r => r.Marque));
                FillCombo(_fEtat, _all.Select(r => r.Etat));
                RebuildCustomColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _loading = false; }
            ApplyFilter();
        }

        private static void FillCombo(ComboBox cb, IEnumerable<string> values)
        {
            var keep = cb.SelectedItem as string;
            cb.Items.Clear();
            cb.Items.Add(All);
            foreach (var v in values.Where(v => !string.IsNullOrWhiteSpace(v))
                                    .Distinct().OrderBy(v => v))
                cb.Items.Add(v);
            cb.SelectedItem = keep != null && cb.Items.Contains(keep) ? keep : All;
        }

        private void RebuildCustomColumns()
        {
            foreach (var c in _grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Tag is string).ToList())
                _grid.Columns.Remove(c);
            foreach (var name in _store.Columns)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = name,
                    Name = "custom_" + name,
                    Tag = name,
                    FillWeight = 80,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                });
            }
        }

        private void ClearFilters()
        {
            _loading = true;
            _search.Text = "";
            _fArmoire.SelectedIndex = 0;
            _fMarque.SelectedIndex = 0;
            _fEtat.SelectedIndex = 0;
            _loading = false;
            ApplyFilter();
        }

        private static bool Has(string? hay, string needle) =>
            (hay ?? "").Contains(needle, StringComparison.CurrentCultureIgnoreCase);

        private void ApplyFilter()
        {
            if (_loading) return;
            int? keepId = Selected()?.Id;

            IEnumerable<ProductRow> q = _all;
            var t = _search.Text.Trim();
            if (t.Length > 0)
                q = q.Where(r => Has(r.SKU, t) || Has(r.Name, t) || Has(r.Marque, t)
                              || Has(r.Emplacement, t) || Has(r.Armoire, t)
                              || r.Custom.Values.Any(v => Has(v, t)));
            if (_fArmoire.SelectedIndex > 0) q = q.Where(r => r.Armoire == (string)_fArmoire.SelectedItem!);
            if (_fMarque.SelectedIndex > 0) q = q.Where(r => r.Marque == (string)_fMarque.SelectedItem!);
            if (_fEtat.SelectedIndex > 0) q = q.Where(r => r.Etat == (string)_fEtat.SelectedItem!);

            var list = q.ToList();
            if (_sortProp != null)
            {
                var col = _grid.Columns.Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => KeyName(c) == _sortProp);
                if (col != null)
                {
                    var cmp = Comparer<object?>.Create((a, b) => Comparer.Default.Compare(a, b));
                    list = (_sortAsc ? list.OrderBy(r => KeyOf(r, col), cmp)
                                     : list.OrderByDescending(r => KeyOf(r, col), cmp)).ToList();
                }
            }
            _view = list;

            _grid.DataSource = null;
            _grid.DataSource = _view;
            foreach (DataGridViewColumn c in _grid.Columns)
                c.HeaderCell.SortGlyphDirection = KeyName(c) == _sortProp
                    ? (_sortAsc ? SortOrder.Ascending : SortOrder.Descending) : SortOrder.None;

            if (keepId != null)
            {
                int idx = _view.FindIndex(r => r.Id == keepId);
                if (idx >= 0 && _grid.Columns.Count > 0)
                {
                    _grid.ClearSelection();
                    _grid.CurrentCell = _grid.Rows[idx].Cells[0];
                    _grid.Rows[idx].Selected = true;
                }
            }

            int units = _view.Sum(r => r.Stock);
            decimal value = _view.Sum(r => r.Stock * r.PurchasePrice);
            _status.Text = $"{_view.Count} pièce(s) affichée(s) sur {_all.Count}   •   " +
                           $"Unités en stock : {units}   •   Valeur d'achat du stock : {value:N2}";
        }

        // ================= sorting / formatting =================
        private static string KeyName(DataGridViewColumn c) =>
            c.Tag is string custom ? "custom:" + custom : c.DataPropertyName;

        private static object? KeyOf(ProductRow r, DataGridViewColumn c)
        {
            if (c.Tag is string custom) return r.Custom.TryGetValue(custom, out var v) ? v : "";
            return typeof(ProductRow).GetProperty(c.DataPropertyName)?.GetValue(r);
        }

        private void Grid_HeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var name = KeyName(_grid.Columns[e.ColumnIndex]);
            if (_sortProp == name) _sortAsc = !_sortAsc; else { _sortProp = name; _sortAsc = true; }
            ApplyFilter();
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not ProductRow r) return;
            var col = _grid.Columns[e.ColumnIndex];

            if (col.Tag is string custom)
            {
                e.Value = r.Custom.TryGetValue(custom, out var v) ? v : "";
                e.FormattingApplied = true;
                return;
            }
            if (col.DataPropertyName == nameof(ProductRow.Etat))
            {
                if (r.Etat.Equals("CASSE", StringComparison.OrdinalIgnoreCase))
                    e.CellStyle.ForeColor = Color.FromArgb(255, 120, 110);
                else if (r.Etat.Equals("OUVERT", StringComparison.OrdinalIgnoreCase))
                    e.CellStyle.ForeColor = Color.FromArgb(110, 215, 150);
            }
            else if (col.DataPropertyName == nameof(ProductRow.Stock))
            {
                if (r.Stock <= r.ReorderPoint) e.CellStyle.ForeColor = Theme.Warn;
            }
        }

        // ================= actions =================
        private ProductRow? Selected() =>
            _grid.CurrentRow?.DataBoundItem as ProductRow;

        private void AddProduct() => Edit(null);

        private void EditSelected()
        {
            var r = Selected();
            if (r == null) { Info("Sélectionnez d'abord une pièce."); return; }
            Edit(r);
        }

        private void Edit(ProductRow? existing)
        {
            string[] etats = { "OUVERT", "CASSE" };
            var fields = new List<DialogField>
            {
                new("Référence (SKU) *", existing?.SKU ?? ""),
                new("Désignation *", existing?.Name ?? ""),
                new("Armoire", existing?.Armoire ?? "", _all.Select(r => r.Armoire).Where(s => s != "").Distinct().OrderBy(s => s).ToArray()),
                new("Marque", existing?.Marque ?? "", _all.Select(r => r.Marque).Where(s => s != "").Distinct().OrderBy(s => s).ToArray()),
                new("Emplacement", existing?.Emplacement ?? ""),
                new("État", existing?.Etat ?? "OUVERT", etats),
                new("Prix Achat", (existing?.PurchasePrice ?? 0m).ToString("0.##")),
                new("Prix Vente", (existing?.UnitPrice ?? 0m).ToString("0.##")),
                new("Seuil de réappro.", (existing?.ReorderPoint ?? 1).ToString())
            };
            foreach (var c in _store.Columns)
                fields.Add(new DialogField(c, existing != null && existing.Custom.TryGetValue(c, out var cv) ? cv : ""));

            int editId = existing?.Id ?? 0;
            var v = FormDialog.Ask(FindForm()!, existing == null ? "Nouvelle pièce" : "Modifier la pièce", fields, vals =>
            {
                if (vals[0].Length == 0) return "La référence (SKU) est obligatoire.";
                if (vals[1].Length == 0) return "La désignation est obligatoire.";
                if (!Num.TryDec(vals[6], out var pa) || pa < 0) return "Prix d'achat invalide.";
                if (!Num.TryDec(vals[7], out var pv) || pv < 0) return "Prix de vente invalide.";
                if (!Num.TryInt(vals[8], out var seuil) || seuil < 0) return "Seuil de réappro. invalide.";
                using var c = Db.Create();
                if (c.Products.Any(p => p.SKU == vals[0] && p.Id != editId))
                    return "Ce SKU existe déjà (peut-être dans une pièce supprimée/archivée).";
                return null;
            });
            if (v == null) return;

            try
            {
                Num.TryDec(v[6], out var purchase);
                Num.TryDec(v[7], out var sale);
                Num.TryInt(v[8], out var reorder);

                using var ctx = Db.Create();
                Core.Models.Product p;
                string? oldSku = null;
                if (existing == null)
                {
                    p = new Core.Models.Product { IsActive = true };
                    ctx.Products.Add(p);
                }
                else
                {
                    p = ctx.Products.First(x => x.Id == existing.Id);
                    oldSku = p.SKU;
                }
                p.SKU = v[0]; p.Name = v[1]; p.Category = v[2]; p.Marque = v[3];
                p.Emplacement = v[4]; p.Etat = v[5];
                p.PurchasePrice = purchase; p.UnitPrice = sale; p.ReorderPoint = reorder;
                ctx.SaveChanges();

                if (oldSku != null && oldSku != p.SKU) _store.RenameSku(oldSku, p.SKU!);
                var custom = new Dictionary<string, string>();
                for (int i = 0; i < _store.Columns.Count; i++) custom[_store.Columns[i]] = v[9 + i];
                _store.SetValues(p.SKU!, custom);

                RefreshData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Enregistrement impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelected()
        {
            var r = Selected();
            if (r == null) { Info("Sélectionnez d'abord une pièce."); return; }
            var ok = MessageBox.Show(this,
                $"Supprimer « {r.Name} » ({r.SKU}) ?\nLa pièce sera archivée : l'historique des mouvements est conservé.",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;
            try
            {
                using var ctx = Db.Create();
                var p = ctx.Products.First(x => x.Id == r.Id);
                p.IsActive = false;
                ctx.SaveChanges();
                RefreshData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Suppression impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddColumn()
        {
            var v = FormDialog.Ask(FindForm()!, "Ajouter une colonne",
                new[] { new DialogField("Nom de la colonne", "") },
                vals => vals[0].Length == 0 ? "Entrez un nom." : null, "Ajouter");
            if (v == null) return;
            if (!_store.AddColumn(v[0])) { Info("Cette colonne existe déjà."); return; }
            RefreshData();
        }

        private void RemoveColumn()
        {
            if (_store.Columns.Count == 0) { Info("Aucune colonne personnalisée à retirer."); return; }
            var v = FormDialog.Ask(FindForm()!, "Retirer une colonne",
                new[] { new DialogField("Colonne", _store.Columns[0], _store.Columns.ToArray()) },
                vals => _store.Columns.Contains(vals[0]) ? null : "Choisissez une colonne existante.", "Retirer");
            if (v == null) return;
            var ok = MessageBox.Show(this, $"Retirer la colonne « {v[0]} » et toutes ses valeurs ?",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;
            _store.RemoveColumn(v[0]);
            RefreshData();
        }

        private void ExportCsv()
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (Excel)|*.csv",
                FileName = $"stock_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var sb = new StringBuilder();
                var custom = _store.Columns.ToList();
                sb.AppendLine(string.Join(";", new[] { "SKU", "Désignation", "Armoire", "Marque", "Emplacement",
                    "État", "Stock", "Prix Achat", "Prix Vente" }.Concat(custom).Select(Csv)));
                foreach (var r in _view)
                {
                    var cells = new List<string>
                    {
                        r.SKU, r.Name, r.Armoire, r.Marque, r.Emplacement, r.Etat, r.Stock.ToString(),
                        r.PurchasePrice.ToString("0.00"), r.UnitPrice.ToString("0.00")
                    };
                    cells.AddRange(custom.Select(c => r.Custom.TryGetValue(c, out var x) ? x : ""));
                    sb.AppendLine(string.Join(";", cells.Select(Csv)));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                Info($"{_view.Count} ligne(s) exportée(s).");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Export impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Csv(string s) =>
            s.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

        private void Info(string msg) =>
            MessageBox.Show(this, msg, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
