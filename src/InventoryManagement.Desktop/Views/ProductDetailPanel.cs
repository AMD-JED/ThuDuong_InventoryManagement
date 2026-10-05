using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Desktop
{
    public sealed class StockLine
    {
        public string Depot { get; set; } = "";
        public int Quantite { get; set; }
    }

    public sealed class MoveLine
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public int Qte { get; set; }
        public string Reference { get; set; } = "";
    }

    /// <summary>Right-hand panel of the products screen: details, stock per warehouse, movement history.</summary>
    public sealed class ProductDetailPanel : UserControl
    {
        private readonly CustomColumnStore _store;
        private readonly Label _title = new(), _sub = new();
        private readonly TableLayoutPanel _info = new();
        private readonly DataGridView _stockGrid = new(), _moveGrid = new();

        public ProductDetailPanel(CustomColumnStore store)
        {
            _store = store;
            Width = 350;
            BackColor = Theme.Card;
            Padding = new Padding(14, 12, 14, 8);

            _title.Font = new Font("Segoe UI Semibold", 13f);
            _title.ForeColor = Color.White;
            _title.AutoSize = false; _title.Dock = DockStyle.Top; _title.Height = 30;
            _title.AutoEllipsis = true; _title.UseMnemonic = false;
            _sub.Font = Theme.Base; _sub.ForeColor = Theme.Muted;
            _sub.AutoSize = false; _sub.Dock = DockStyle.Top; _sub.Height = 26;
            _sub.AutoEllipsis = true; _sub.UseMnemonic = false;

            _info.Dock = DockStyle.Top;
            _info.AutoSize = true;
            _info.ColumnCount = 2;
            _info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            _info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _info.Padding = new Padding(0, 4, 0, 8);

            SetupGrid(_stockGrid, 118);
            _stockGrid.Dock = DockStyle.Top;
            _stockGrid.Columns.Add(Theme.Col("Dépôt", nameof(StockLine.Depot), 120));
            _stockGrid.Columns.Add(Theme.Col("Qté", nameof(StockLine.Quantite), 50, null, true));

            SetupGrid(_moveGrid, 0);
            _moveGrid.Dock = DockStyle.Fill;
            _moveGrid.Columns.Add(Theme.Col("Date", nameof(MoveLine.Date), 90, "dd/MM/yy HH:mm"));
            _moveGrid.Columns.Add(Theme.Col("Type", nameof(MoveLine.Type), 70));
            _moveGrid.Columns.Add(Theme.Col("Qté", nameof(MoveLine.Qte), 45, null, true));
            _moveGrid.Columns.Add(Theme.Col("Réf.", nameof(MoveLine.Reference), 60));
            _moveGrid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || _moveGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(MoveLine.Qte)) return;
                if (_moveGrid.Rows[e.RowIndex].DataBoundItem is MoveLine m)
                    e.CellStyle.ForeColor = m.Qte < 0 ? Theme.BadText : Theme.GoodText;
            };

            Label Section(string t)
            {
                var l = Theme.MakeLabel(t, Theme.Bold, Theme.Accent);
                l.AutoSize = false; l.Dock = DockStyle.Top; l.Height = 30;
                l.TextAlign = ContentAlignment.BottomLeft;
                return l;
            }

            // Fill first, then the rest bottom-up (last added = top-most).
            Controls.Add(_moveGrid);
            Controls.Add(Section("Derniers mouvements"));
            Controls.Add(_stockGrid);
            Controls.Add(Section("Stock par dépôt"));
            Controls.Add(_info);
            Controls.Add(_sub);
            Controls.Add(_title);
            ShowFor(null);
        }

        private static void SetupGrid(DataGridView g, int height)
        {
            Theme.StyleGrid(g);
            g.AutoGenerateColumns = false;
            g.BackgroundColor = Theme.Card;
            g.DefaultCellStyle.BackColor = Theme.Card;
            g.RowTemplate.Height = 28;
            g.ColumnHeadersHeight = 32;
            g.Font = new Font("Segoe UI", 9f);
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f);
            g.RowsDefaultCellStyle.SelectionBackColor = Theme.Card;
            g.RowsDefaultCellStyle.SelectionForeColor = Theme.Text;
            g.DefaultCellStyle.SelectionBackColor = Theme.Card;
            g.DefaultCellStyle.SelectionForeColor = Theme.Text;
            g.AlternatingRowsDefaultCellStyle.BackColor = Theme.CardAlt;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.CardAlt;
            if (height > 0) g.Height = height;
        }

        private void AddInfo(string label, string value, Color? color = null)
        {
            var l = Theme.MakeLabel(label, Theme.Base, Theme.Muted);
            l.Margin = new Padding(0, 3, 0, 3);
            var v = Theme.MakeLabel(value.Length == 0 ? "-" : value, Theme.Base, color ?? Theme.Text);
            v.UseMnemonic = false;
            v.Margin = new Padding(0, 3, 0, 3);
            v.MaximumSize = new Size(190, 0);
            _info.Controls.Add(l);
            _info.Controls.Add(v);
        }

        public void ShowFor(ProductRow? r)
        {
            _info.SuspendLayout();
            _info.Controls.Clear();
            _info.RowStyles.Clear();
            _info.RowCount = 0;

            if (r == null)
            {
                _title.Text = "Détails de la pièce";
                _sub.Text = "Sélectionnez une pièce dans la liste.";
                _stockGrid.DataSource = null;
                _moveGrid.DataSource = null;
                _info.ResumeLayout();
                return;
            }

            _title.Text = r.Name;
            _sub.Text = $"{r.SKU}  •  {r.Marque}";

            var etatColor = r.Etat.Equals("CASSE", StringComparison.OrdinalIgnoreCase) ? Theme.BadText
                          : r.Etat.Equals("OUVERT", StringComparison.OrdinalIgnoreCase) ? Theme.GoodText : Theme.Text;
            AddInfo("Armoire", r.Armoire);
            AddInfo("Emplacement", r.Emplacement);
            AddInfo("État", r.Etat, etatColor);
            AddInfo("Prix d'achat", r.PurchasePrice.ToString("N2"));
            AddInfo("Prix de vente", r.UnitPrice.ToString("N2"));
            decimal margin = r.UnitPrice - r.PurchasePrice;
            string pct = r.PurchasePrice > 0 ? $"  ({margin / r.PurchasePrice * 100:0.#} %)" : "";
            AddInfo("Marge", margin.ToString("N2") + pct, margin < 0 ? Theme.BadText : Theme.Text);
            AddInfo("Seuil de réappro.", r.ReorderPoint.ToString());
            AddInfo("Stock total", r.Stock.ToString(), r.Stock <= r.ReorderPoint ? Theme.Warn : Theme.GoodText);
            foreach (var c in _store.Columns)
                AddInfo(c, r.Custom.TryGetValue(c, out var cv) ? cv : "");
            _info.ResumeLayout();

            try
            {
                using var ctx = Db.Create();
                var stock = ctx.InventoryItems.AsNoTracking().Include(i => i.Warehouse)
                    .Where(i => i.ProductId == r.Id).ToList()
                    .Select(i => new StockLine { Depot = i.Warehouse?.Name ?? "?", Quantite = i.Quantity })
                    .OrderBy(x => x.Depot).ToList();
                _stockGrid.DataSource = stock;

                var moves = ctx.StockTransactions.AsNoTracking()
                    .Where(t => t.ProductId == r.Id).ToList()
                    .OrderByDescending(t => t.TransactionDate).Take(25)
                    .Select(t => new MoveLine
                    {
                        Date = t.TransactionDate.ToLocalTime(),
                        Type = OperationsView.TypeLabel(t.Type),
                        Qte = t.Quantity,
                        Reference = t.ReferenceNumber
                    }).ToList();
                _moveGrid.DataSource = moves;
            }
            catch
            {
                _stockGrid.DataSource = null;
                _moveGrid.DataSource = null;
            }
        }
    }
}
