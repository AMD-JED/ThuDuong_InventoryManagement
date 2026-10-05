using Microsoft.EntityFrameworkCore;

using InventoryManagement.Services;

namespace InventoryManagement.Desktop
{
    public sealed class TxRow
    {
        public DateTime Date { get; set; }
        public string Produit { get; set; } = "";
        public string Depot { get; set; } = "";
        public string Type { get; set; } = "";
        public int Quantite { get; set; }
        public string Reference { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public sealed class OperationsView : UserControl, IRefreshable
    {
        private readonly ComboBox _op = new(), _prod = new(), _wh = new(), _dest = new();
        private readonly NumericUpDown _qty = new();
        private readonly TextBox _ref = new(), _notes = new();
        private readonly Label _lblQty = Theme.MakeLabel("Quantité"), _lblDest = Theme.MakeLabel("Dépôt destination");
        private readonly Label _stock = Theme.MakeLabel("", Theme.Bold, Theme.Accent);
        private readonly DataGridView _grid = new();
        private readonly Button _submit;

        public event Action? DataChanged;

        public OperationsView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            Padding = new Padding(16, 12, 16, 8);

            _op.DropDownStyle = ComboBoxStyle.DropDownList;
            _op.Items.AddRange(new object[] { "📥 Réception (entrée)", "📤 Sortie (vente / expédition)", "🔧 Ajustement (inventaire)", "🔁 Transfert entre dépôts" });
            _op.SelectedIndex = 0;
            _prod.DropDownStyle = ComboBoxStyle.DropDown;
            _prod.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _prod.AutoCompleteSource = AutoCompleteSource.ListItems;
            _wh.DropDownStyle = ComboBoxStyle.DropDownList;
            _dest.DropDownStyle = ComboBoxStyle.DropDownList;
            _qty.Minimum = 0; _qty.Maximum = 1_000_000; _qty.Value = 1;
            foreach (Control c in new Control[] { _op, _prod, _wh, _dest, _qty, _ref, _notes })
            {
                Theme.StyleInput(c);
                c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                c.Margin = new Padding(4, 5, 12, 5);
            }
            _qty.Anchor = AnchorStyles.Left;
            _qty.Width = 120;

            _submit = Theme.MakeButton("✔ Valider l'opération", Theme.Good, (_, _) => Submit(), 190);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, Padding = new Padding(14, 10, 6, 10),
                BackColor = Theme.Card
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            void Row(Control l1, Control c1, Control? l2, Control? c2)
            {
                l1.Anchor = AnchorStyles.Left; l1.Margin = new Padding(0, 8, 0, 0);
                table.Controls.Add(l1); table.Controls.Add(c1);
                table.Controls.Add(l2 ?? new Panel { Width = 1, Height = 1 });
                table.Controls.Add(c2 ?? new Panel { Width = 1, Height = 1 });
                if (l2 != null) { l2.Anchor = AnchorStyles.Left; l2.Margin = new Padding(0, 8, 0, 0); }
            }
            Row(Theme.MakeLabel("Opération"), _op, Theme.MakeLabel("Pièce"), _prod);
            Row(Theme.MakeLabel("Dépôt"), _wh, _lblDest, _dest);
            Row(_lblQty, _qty, Theme.MakeLabel("Référence"), _ref);
            Row(Theme.MakeLabel("Notes"), _notes, _stock, null);
            _stock.Margin = new Padding(0, 8, 0, 0);
            _submit.Margin = new Padding(4, 8, 0, 4);
            table.Controls.Add(new Panel { Width = 1, Height = 1 });
            table.Controls.Add(_submit);

            var header = Theme.MakeLabel("📊 Opérations de stock", Theme.Title, Color.White);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = 48;
            var hist = Theme.MakeLabel("Derniers mouvements", Theme.Bold, Theme.Muted);
            hist.AutoSize = false; hist.Dock = DockStyle.Top; hist.Height = 34;
            hist.TextAlign = ContentAlignment.BottomLeft;

            Theme.StyleGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(Theme.Col("Date", nameof(TxRow.Date), 85, "dd/MM/yyyy HH:mm"));
            _grid.Columns.Add(Theme.Col("Pièce", nameof(TxRow.Produit), 110));
            _grid.Columns.Add(Theme.Col("Dépôt", nameof(TxRow.Depot), 80));
            _grid.Columns.Add(Theme.Col("Type", nameof(TxRow.Type), 70));
            _grid.Columns.Add(Theme.Col("Qté", nameof(TxRow.Quantite), 45, null, true));
            _grid.Columns.Add(Theme.Col("Référence", nameof(TxRow.Reference), 70));
            _grid.Columns.Add(Theme.Col("Notes", nameof(TxRow.Notes), 130));
            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].DataPropertyName != nameof(TxRow.Quantite)) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is TxRow t)
                    e.CellStyle.ForeColor = t.Quantite < 0 ? Color.FromArgb(255, 120, 110) : Color.FromArgb(110, 215, 150);
            };

            Controls.Add(_grid);
            Controls.Add(hist);
            Controls.Add(table);
            Controls.Add(header);

            _op.SelectedIndexChanged += (_, _) => UpdateMode();
            _prod.SelectedIndexChanged += (_, _) => UpdateStockLabel();
            _prod.TextChanged += (_, _) => UpdateStockLabel();
            _wh.SelectedIndexChanged += (_, _) => UpdateStockLabel();
            UpdateMode();
            RefreshData();
        }

        private Named? CurrentProduct() =>
            _prod.SelectedItem as Named
            ?? _prod.Items.Cast<Named>().FirstOrDefault(p => p.Text == _prod.Text);

        private void UpdateMode()
        {
            bool transfer = _op.SelectedIndex == 3;
            _dest.Enabled = transfer;
            _lblDest.ForeColor = transfer ? Theme.Text : Theme.Muted;
            _lblQty.Text = _op.SelectedIndex == 2 ? "Nouvelle qté" : "Quantité";
        }

        private void UpdateStockLabel()
        {
            var p = CurrentProduct();
            var w = _wh.SelectedItem as Named;
            if (p == null || w == null) { _stock.Text = ""; return; }
            try
            {
                using var ctx = Db.Create();
                int here = ctx.InventoryItems.Where(i => i.ProductId == p.Id && i.WarehouseId == w.Id)
                                             .Select(i => i.Quantity).FirstOrDefault();
                int total = ctx.InventoryItems.Where(i => i.ProductId == p.Id)
                                              .Select(i => i.Quantity).ToList().Sum();
                _stock.Text = $"Stock : {here} ici  •  {total} au total";
            }
            catch { _stock.Text = ""; }
        }

        public void RefreshData()
        {
            try
            {
                using var ctx = Db.Create();
                var prodId = CurrentProduct()?.Id;
                var whId = (_wh.SelectedItem as Named)?.Id;
                var destId = (_dest.SelectedItem as Named)?.Id;

                var prods = ctx.Products.AsNoTracking().Where(p => p.IsActive).ToList()
                    .OrderBy(p => p.Name).ThenBy(p => p.SKU)
                    .Select(p => new Named(p.Id, $"{p.SKU} — {p.Name} ({p.Marque}, {p.Emplacement})")).ToArray();
                var whs = ctx.Warehouses.AsNoTracking().Where(w => w.IsActive).ToList()
                    .OrderBy(w => w.Name).Select(w => new Named(w.Id, w.Name)).ToArray();

                _prod.Items.Clear(); _prod.Items.AddRange(prods);
                _wh.Items.Clear(); _wh.Items.AddRange(whs);
                _dest.Items.Clear(); _dest.Items.AddRange(whs);
                if (prodId != null) _prod.SelectedItem = prods.FirstOrDefault(x => x.Id == prodId);
                _wh.SelectedItem = whs.FirstOrDefault(x => x.Id == whId) ?? whs.FirstOrDefault();
                _dest.SelectedItem = whs.FirstOrDefault(x => x.Id == destId) ?? whs.Skip(1).FirstOrDefault();

                var tx = ctx.StockTransactions.AsNoTracking()
                    .Include(t => t.Product).Include(t => t.Warehouse)
                    .ToList()
                    .OrderByDescending(t => t.TransactionDate).Take(100)
                    .Select(t => new TxRow
                    {
                        Date = t.TransactionDate.ToLocalTime(),
                        Produit = $"{t.Product?.SKU} {t.Product?.Name}",
                        Depot = t.Warehouse?.Name ?? "",
                        Type = TypeLabel(t.Type),
                        Quantite = t.Quantity,
                        Reference = t.ReferenceNumber,
                        Notes = t.Notes
                    }).ToList();
                _grid.DataSource = tx;
                UpdateStockLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string TypeLabel(Core.Enums.TransactionType t) => t switch
        {
            Core.Enums.TransactionType.Received => "Réception",
            Core.Enums.TransactionType.Shipped => "Sortie",
            Core.Enums.TransactionType.Transferred => "Transfert",
            Core.Enums.TransactionType.Adjusted => "Ajustement",
            Core.Enums.TransactionType.Returned => "Retour",
            _ => t.ToString()
        };

        private async void Submit()
        {
            var p = CurrentProduct();
            var w = _wh.SelectedItem as Named;
            if (p == null) { Warn("Choisissez une pièce dans la liste."); return; }
            if (w == null) { Warn("Aucun dépôt actif. Créez-en un dans « Warehouse Management »."); return; }
            int qty = (int)_qty.Value;
            int mode = _op.SelectedIndex;
            if (mode != 2 && qty <= 0) { Warn("La quantité doit être supérieure à 0."); return; }

            try
            {
                _submit.Enabled = false;
                using var ctx = Db.Create();
                var svc = new InventoryService(ctx);
                switch (mode)
                {
                    case 0: await svc.ReceiveStockAsync(p.Id, w.Id, qty, _ref.Text.Trim(), _notes.Text.Trim()); break;
                    case 1: await svc.ShipStockAsync(p.Id, w.Id, qty, _ref.Text.Trim(), _notes.Text.Trim()); break;
                    case 2: await svc.AdjustStockAsync(p.Id, w.Id, qty, _notes.Text.Trim()); break;
                    case 3:
                        if (_dest.SelectedItem is not Named d) { Warn("Choisissez le dépôt destination."); return; }
                        await svc.TransferStockAsync(p.Id, w.Id, d.Id, qty, _notes.Text.Trim());
                        break;
                }
                _ref.Clear(); _notes.Clear();
                RefreshData();
                DataChanged?.Invoke();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                Warn(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _submit.Enabled = true; }
        }

        private void Warn(string m) =>
            MessageBox.Show(this, m, "Opération impossible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
