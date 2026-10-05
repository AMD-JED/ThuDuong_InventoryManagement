namespace InventoryManagement.Desktop
{
    public partial class Form1 : Form
    {
        private readonly Panel _sidebar = new();
        private readonly Panel _content = new();
        private readonly List<(Button Btn, string Text)> _menu = new();
        private readonly Label _footer = new();
        private readonly ToolTip _tip = new();

        private ProductsView? _products;
        private WarehousesView? _warehouses;
        private OperationsView? _operations;
        private DashboardView? _dashboard;
        private AlertsView? _alerts;
        private BackupView? _backup;
        private Control? _current;
        private Button? _activeBtn;
        private Button _btnAlerts = null!;

        public Form1()
        {
            InitializeComponent();

            Text = "📦 INVENTORY MANAGEMENT - Pièces automobiles";
            ClientSize = new Size(1320, 760);
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            Font = Theme.Base;

            try { Db.EnsureReady(); }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la base de données :\n" + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            BuildSidebar();
            _content.Dock = DockStyle.Fill;
            _content.BackColor = Theme.Bg;
            Controls.Add(_content);
            Controls.Add(_sidebar);

            ShowProducts();
            UpdateBadge();
        }

        private void BuildSidebar()
        {
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = 270;
            _sidebar.BackColor = Theme.Sidebar;

            var title = Theme.MakeLabel("INVENTORY", new Font("Segoe UI Semibold", 15f), Theme.Accent);
            title.Location = new Point(18, 18);
            var sub = Theme.MakeLabel("Pièces auto • Multi-dépôts", new Font("Segoe UI", 9f), Theme.Muted);
            sub.Location = new Point(20, 50);
            _sidebar.Controls.Add(title);
            _sidebar.Controls.Add(sub);

            AddMenu("[1]  📦  Product Management", 100, ShowProducts);
            AddMenu("[2]  🏭  Warehouse Management", 148, () => Navigate(Warehouses()));
            AddMenu("[3]  📊  Inventory Operations", 196, () => Navigate(Operations()));
            AddMenu("[4]  📈  KPI Dashboard", 244, () => Navigate(Dashboard()));
            _btnAlerts = AddMenu("[5]  🔔  Reorder Alerts", 292, () => Navigate(Alerts()));
            AddMenu("[6]  🔍  Search & Filter", 340, () => { ShowProducts(); _products!.FocusSearch(); });
            AddMenu("[7]  💾  Backup / Restore", 388, () => Navigate(Backup()));

            _footer.Text = "DB : " + Db.File + "\nCtrl+F recherche • Ctrl+N ajouter\nF5 actualiser";
            _tip.SetToolTip(_footer, Db.FullPath);
            _footer.ForeColor = Theme.Muted;
            _footer.Font = new Font("Segoe UI", 8f);
            _footer.Dock = DockStyle.Bottom;
            _footer.Height = 74;
            _footer.Padding = new Padding(14, 0, 8, 8);
            _footer.TextAlign = ContentAlignment.BottomLeft;
            _sidebar.Controls.Add(_footer);
        }

        private Button AddMenu(string text, int top, Action onClick)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(10, top),
                Size = new Size(250, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Sidebar,
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,   // otherwise '&' in "Search & Filter" is eaten
                AutoEllipsis = true,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.Hover;
            b.Click += (_, _) => { SetActive(b); onClick(); };
            _sidebar.Controls.Add(b);
            _menu.Add((b, text));
            return b;
        }

        private void SetActive(Button b)
        {
            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Theme.Sidebar;
                _activeBtn.ForeColor = Theme.Text;
            }
            _activeBtn = b;
            b.BackColor = Theme.SelectBg;
            b.ForeColor = Color.White;
        }

        // ---- lazily created screens (they refresh when shown) ----
        private ProductsView Products()
        {
            if (_products == null)
            {
                _products = new ProductsView();
                _products.DataChanged += OnDataChanged;
            }
            return _products;
        }
        private WarehousesView Warehouses()
        {
            if (_warehouses == null) { _warehouses = new WarehousesView(); _warehouses.DataChanged += OnDataChanged; }
            return _warehouses;
        }
        private OperationsView Operations()
        {
            if (_operations == null) { _operations = new OperationsView(); _operations.DataChanged += OnDataChanged; }
            return _operations;
        }
        private DashboardView Dashboard() => _dashboard ??= new DashboardView();
        private AlertsView Alerts() => _alerts ??= new AlertsView();
        private BackupView Backup()
        {
            if (_backup == null) { _backup = new BackupView(); _backup.Restored += RefreshAll; }
            return _backup;
        }

        /// <summary>After a restore every screen must re-read the database.</summary>
        private void RefreshAll()
        {
            foreach (var v in new IRefreshable?[] { _products, _warehouses, _operations, _dashboard, _alerts })
                v?.RefreshData();
            UpdateBadge();
        }

        private void OnDataChanged() => UpdateBadge();

        private void ShowProducts()
        {
            SetActive(_menu[0].Btn);
            Navigate(Products());
        }

        private void Navigate(Control view)
        {
            if (_current != view)
            {
                _content.Controls.Clear();
                _content.Controls.Add(view);
                _current = view;
            }
            (view as IRefreshable)?.RefreshData();
            UpdateBadge();
        }

        private void UpdateBadge()
        {
            try
            {
                int n = AlertsView.CountAlerts();
                var baseText = _menu.First(m => m.Btn == _btnAlerts).Text;
                _btnAlerts.Text = n > 0 ? $"{baseText}  ({n})" : baseText;
            }
            catch { /* badge is cosmetic */ }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                    ShowProducts(); Products().FocusSearch();
                    return true;
                case Keys.Control | Keys.N:
                    ShowProducts(); Products().AddNew();
                    return true;
                case Keys.F5:
                    (_current as IRefreshable)?.RefreshData(); UpdateBadge();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
