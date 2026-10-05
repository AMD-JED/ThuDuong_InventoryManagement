namespace InventoryManagement.Desktop
{
    public sealed class DashboardView : UserControl, IRefreshable
    {
        private readonly CustomColumnStore _store = new();
        private readonly FlowLayoutPanel _cards = new();
        private readonly BarChart _byArmoire = new() { Heading = "Références par armoire", Dock = DockStyle.Fill };
        private readonly BarChart _byMarque = new() { Heading = "Références par marque", Dock = DockStyle.Fill };

        public DashboardView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            Padding = new Padding(16, 12, 16, 8);

            var header = Theme.MakeLabel("📈 Tableau de bord", Theme.Title, Color.White);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = 48;

            _cards.Dock = DockStyle.Top;
            _cards.AutoSize = true;
            _cards.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _cards.WrapContents = true;

            var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _byArmoire.Margin = new Padding(0, 0, 8, 0);
            _byMarque.Margin = new Padding(8, 0, 0, 0);
            charts.Controls.Add(_byArmoire, 0, 0);
            charts.Controls.Add(_byMarque, 1, 0);

            Controls.Add(charts);
            Controls.Add(_cards);
            Controls.Add(header);
            RefreshData();
        }

        private Panel Card(string caption, string value, Color accent)
        {
            var p = new Panel { Width = 205, Height = 98, BackColor = Theme.Card, Margin = new Padding(0, 4, 12, 8) };
            var bar = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };
            var v = Theme.MakeLabel(value, Theme.Big, Color.White);
            v.Location = new Point(18, 12);
            var c = Theme.MakeLabel(caption, Theme.Base, Theme.Muted);
            c.Location = new Point(18, 62);
            p.Controls.Add(v); p.Controls.Add(c); p.Controls.Add(bar);
            return p;
        }

        public void RefreshData()
        {
            try
            {
                using var ctx = Db.Create();
                var rows = StockQueries.LoadRows(ctx, _store);
                int whCount = ctx.Warehouses.Count(w => w.IsActive);

                int units = rows.Sum(r => r.Stock);
                decimal cost = rows.Sum(r => r.Stock * r.PurchasePrice);
                decimal sale = rows.Sum(r => r.Stock * r.UnitPrice);
                int broken = rows.Count(r => r.Etat.Equals("CASSE", StringComparison.OrdinalIgnoreCase));
                int alerts = rows.Count(r => r.Stock <= r.ReorderPoint * 2);

                _cards.Controls.Clear();
                _cards.Controls.Add(Card("Références actives", rows.Count.ToString("N0"), Theme.Accent));
                _cards.Controls.Add(Card("Unités en stock", units.ToString("N0"), Theme.Good));
                _cards.Controls.Add(Card("Valeur stock (achat)", cost.ToString("N0"), Theme.Neutral));
                _cards.Controls.Add(Card("Valeur stock (vente)", sale.ToString("N0"), Theme.Neutral));
                _cards.Controls.Add(Card("Pièces CASSE", broken.ToString("N0"), Theme.Danger));
                _cards.Controls.Add(Card("Alertes réappro.", alerts.ToString("N0"), Theme.Warn));
                _cards.Controls.Add(Card("Dépôts actifs", whCount.ToString("N0"), Theme.Accent));

                _byArmoire.Data = rows.GroupBy(r => string.IsNullOrWhiteSpace(r.Armoire) ? "(sans armoire)" : r.Armoire)
                    .Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).Take(8).ToList();
                _byMarque.Data = rows.GroupBy(r => string.IsNullOrWhiteSpace(r.Marque) ? "(sans marque)" : r.Marque)
                    .Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).Take(8).ToList();
                _byArmoire.Invalidate();
                _byMarque.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
