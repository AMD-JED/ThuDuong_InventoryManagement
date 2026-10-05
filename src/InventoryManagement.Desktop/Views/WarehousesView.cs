using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Desktop
{
    public sealed class WarehouseRow
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string Adresse { get; set; } = "";
        public string Ville { get; set; } = "";
        public int Capacite { get; set; }
        public int Utilise { get; set; }
        public string Taux { get; set; } = "";
        public string Actif { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public sealed class WarehousesView : UserControl, IRefreshable
    {
        private readonly DataGridView _grid = new();
        public event Action? DataChanged;

        public WarehousesView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            Padding = new Padding(16, 12, 16, 8);

            Theme.StyleGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(Theme.Col("Nom", nameof(WarehouseRow.Nom), 110));
            _grid.Columns.Add(Theme.Col("Adresse", nameof(WarehouseRow.Adresse), 130));
            _grid.Columns.Add(Theme.Col("Ville", nameof(WarehouseRow.Ville), 80));
            _grid.Columns.Add(Theme.Col("Capacité", nameof(WarehouseRow.Capacite), 60, "N0", true));
            _grid.Columns.Add(Theme.Col("Utilisé", nameof(WarehouseRow.Utilise), 60, "N0", true));
            _grid.Columns.Add(Theme.Col("Remplissage", nameof(WarehouseRow.Taux), 70, null, true));
            _grid.Columns.Add(Theme.Col("Actif", nameof(WarehouseRow.Actif), 50));
            _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Edit(Selected()); };
            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not WarehouseRow r) return;
                if (!r.IsActive) e.CellStyle.ForeColor = Theme.Muted;
            };

            var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 6, 0, 0) };
            actions.Controls.Add(Theme.MakeButton("➕ Ajouter", Theme.Good, (_, _) => Edit(null), 110));
            actions.Controls.Add(Theme.MakeButton("✏️ Modifier", Theme.Accent, (_, _) => Edit(Selected()), 110));
            actions.Controls.Add(Theme.MakeButton("🔄 Activer / Désactiver", Theme.Neutral, (_, _) => Toggle(), 190));

            var header = Theme.MakeLabel("🏭 Gestion des dépôts", Theme.Title, Color.White);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = 48;

            Controls.Add(_grid);
            Controls.Add(actions);
            Controls.Add(header);
            RefreshData();
        }

        private WarehouseRow? Selected() => _grid.CurrentRow?.DataBoundItem as WarehouseRow;

        public void RefreshData()
        {
            try
            {
                using var ctx = Db.Create();
                var rows = ctx.Warehouses.AsNoTracking().Include(w => w.InventoryItems).ToList()
                    .OrderByDescending(w => w.IsActive).ThenBy(w => w.Name)
                    .Select(w =>
                    {
                        int used = w.InventoryItems.Sum(i => i.Quantity);
                        return new WarehouseRow
                        {
                            Id = w.Id, Nom = w.Name, Adresse = w.Address, Ville = w.City,
                            Capacite = w.Capacity, Utilise = used,
                            Taux = w.Capacity > 0 ? $"{100.0 * used / w.Capacity:0.#} %" : "-",
                            Actif = w.IsActive ? "Oui" : "Non", IsActive = w.IsActive
                        };
                    }).ToList();
                _grid.DataSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Edit(WarehouseRow? existing)
        {
            var fields = new[]
            {
                new DialogField("Nom *", existing?.Nom ?? ""),
                new DialogField("Adresse *", existing?.Adresse ?? ""),
                new DialogField("Ville", existing?.Ville ?? ""),
                new DialogField("Capacité (unités)", (existing?.Capacite ?? 1000).ToString())
            };
            int id = existing?.Id ?? 0;
            var v = FormDialog.Ask(FindForm()!, existing == null ? "Nouveau dépôt" : "Modifier le dépôt", fields, vals =>
            {
                if (vals[0].Length == 0) return "Le nom est obligatoire.";
                if (vals[1].Length == 0) return "L'adresse est obligatoire.";
                if (!Num.TryInt(vals[3], out var cap) || cap < 0) return "Capacité invalide.";
                using var c = Db.Create();
                if (c.Warehouses.Any(w => w.Name == vals[0] && w.Id != id)) return "Un dépôt porte déjà ce nom.";
                return null;
            });
            if (v == null) return;
            try
            {
                Num.TryInt(v[3], out var cap);
                using var ctx = Db.Create();
                var w = existing == null ? new Core.Models.Warehouse { IsActive = true } : ctx.Warehouses.First(x => x.Id == id);
                if (existing == null) ctx.Warehouses.Add(w);
                w.Name = v[0]; w.Address = v[1]; w.City = v[2]; w.Capacity = cap;
                ctx.SaveChanges();
                RefreshData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Enregistrement impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Toggle()
        {
            var r = Selected();
            if (r == null) { MessageBox.Show(this, "Sélectionnez un dépôt."); return; }
            try
            {
                using var ctx = Db.Create();
                var w = ctx.Warehouses.First(x => x.Id == r.Id);
                w.IsActive = !w.IsActive;
                ctx.SaveChanges();
                RefreshData();
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
