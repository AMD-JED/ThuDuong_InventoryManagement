namespace InventoryManagement.Desktop
{
    public sealed class AlertRow
    {
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Marque { get; set; } = "";
        public string Emplacement { get; set; } = "";
        public int Stock { get; set; }
        public int Seuil { get; set; }
        public string Niveau { get; set; } = "";
    }

    /// <summary>Products at or below their reorder point (same rule as InventoryService.GetStockLevel).</summary>
    public sealed class AlertsView : UserControl, IRefreshable
    {
        private readonly CustomColumnStore _store = new();
        private readonly DataGridView _grid = new();
        private readonly Label _info = Theme.MakeLabel("", Theme.Base, Theme.Muted);

        public AlertsView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            Padding = new Padding(16, 12, 16, 8);

            Theme.StyleGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(Theme.Col("Niveau", nameof(AlertRow.Niveau), 60));
            _grid.Columns.Add(Theme.Col("Référence (SKU)", nameof(AlertRow.SKU), 120));
            _grid.Columns.Add(Theme.Col("Désignation", nameof(AlertRow.Name), 100));
            _grid.Columns.Add(Theme.Col("Marque", nameof(AlertRow.Marque), 70));
            _grid.Columns.Add(Theme.Col("Emplacement", nameof(AlertRow.Emplacement), 80));
            _grid.Columns.Add(Theme.Col("Stock", nameof(AlertRow.Stock), 50, null, true));
            _grid.Columns.Add(Theme.Col("Seuil", nameof(AlertRow.Seuil), 50, null, true));
            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].DataPropertyName != nameof(AlertRow.Niveau)) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is AlertRow r)
                    e.CellStyle.ForeColor = r.Niveau == "Critique" ? Color.FromArgb(255, 120, 110) : Theme.Warn;
            };

            _info.AutoSize = false; _info.Dock = DockStyle.Top; _info.Height = 30;
            var header = Theme.MakeLabel("🔔 Alertes de réapprovisionnement", Theme.Title, Color.White);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = 48;

            Controls.Add(_grid);
            Controls.Add(_info);
            Controls.Add(header);
            RefreshData();
        }

        public static int CountAlerts()
        {
            using var ctx = Db.Create();
            return StockQueries.LoadRows(ctx, new CustomColumnStore()).Count(r => r.Stock <= r.ReorderPoint * 2);
        }

        public void RefreshData()
        {
            try
            {
                using var ctx = Db.Create();
                var rows = StockQueries.LoadRows(ctx, _store)
                    .Where(r => r.Stock <= r.ReorderPoint * 2)
                    .Select(r => new AlertRow
                    {
                        SKU = r.SKU, Name = r.Name, Marque = r.Marque, Emplacement = r.Emplacement,
                        Stock = r.Stock, Seuil = r.ReorderPoint,
                        Niveau = r.Stock <= r.ReorderPoint ? "Critique" : "Faible"
                    })
                    .OrderBy(a => a.Niveau == "Critique" ? 0 : 1).ThenBy(a => a.Stock).ThenBy(a => a.Name)
                    .ToList();
                _grid.DataSource = rows;
                int crit = rows.Count(a => a.Niveau == "Critique");
                _info.Text = rows.Count == 0
                    ? "✔ Tous les stocks sont au-dessus des seuils."
                    : $"{crit} critique(s) • {rows.Count - crit} faible(s).  Utilisez « Inventory Operations » pour enregistrer une réception.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
