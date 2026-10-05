namespace InventoryManagement.Desktop
{
    /// <summary>Colors, fonts and small style helpers shared by all screens.</summary>
    public static class Theme
    {
        // Palette: "01 · SaaS Analytics" design system (dark theme tokens).
        public static readonly Color Bg = Color.FromArgb(10, 10, 15);          // --bg        #0a0a0f
        public static readonly Color Sidebar = Color.FromArgb(6, 6, 9);        // --bg-dim    #060609
        public static readonly Color Card = Color.FromArgb(17, 17, 24);        // --surface   #111118
        public static readonly Color CardAlt = Color.FromArgb(24, 24, 31);     // --surface-mid  #18181f
        public static readonly Color Header = Color.FromArgb(31, 31, 40);      // --surface-high #1f1f28
        public static readonly Color Input = Color.FromArgb(31, 31, 40);       // --surface-high
        public static readonly Color Border = Color.FromArgb(40, 40, 50);      // --surface-highest #282832
        public static readonly Color Hover = Color.FromArgb(31, 31, 40);
        public static readonly Color Text = Color.FromArgb(228, 228, 235);
        public static readonly Color Muted = Color.FromArgb(139, 139, 153);
        public static readonly Color Accent = Color.FromArgb(59, 130, 246);    // --primary   #3b82f6
        public static readonly Color SelectBg = Color.FromArgb(30, 58, 138);   // primary, darkened for row selection
        public static readonly Color Good = Color.FromArgb(22, 163, 74);       // --positive family (button bg, white text)
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);     // --error family (button bg, white text)
        public static readonly Color Warn = Color.FromArgb(245, 158, 11);      // --tertiary  #f59e0b
        public static readonly Color Neutral = Color.FromArgb(51, 51, 64);
        public static readonly Color GoodText = Color.FromArgb(74, 222, 128);  // readable green on dark rows
        public static readonly Color BadText = Color.FromArgb(248, 113, 113);  // readable red on dark rows

        public static readonly Font Base = new Font("Segoe UI", 10f);
        public static readonly Font Bold = new Font("Segoe UI Semibold", 10f);
        public static readonly Font Title = new Font("Segoe UI Semibold", 17f);
        public static readonly Font Big = new Font("Segoe UI Semibold", 22f);

        public static Button MakeButton(string text, Color? back = null, EventHandler? onClick = null, int width = 130)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = back ?? Neutral,
                ForeColor = Color.White,
                Font = Base,
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            if (onClick != null) b.Click += onClick;
            return b;
        }

        public static Label MakeLabel(string text, Font? font = null, Color? color = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = font ?? Base,
                ForeColor = color ?? Text,
                BackColor = Color.Transparent
            };
        }

        public static void StyleInput(Control c)
        {
            c.Font = Base;
            c.BackColor = Input;
            c.ForeColor = Color.White;
            if (c is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
            if (c is ComboBox cb) cb.FlatStyle = FlatStyle.Flat;
            if (c is NumericUpDown nud) nud.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleGrid(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Bg;
            g.GridColor = Border;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.Font = Base;

            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 38;
            g.ColumnHeadersDefaultCellStyle.BackColor = Header;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Bold;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Header;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

            g.RowTemplate.Height = 32;
            g.DefaultCellStyle.BackColor = Card;
            g.DefaultCellStyle.ForeColor = Text;
            g.DefaultCellStyle.SelectionBackColor = SelectBg;
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = CardAlt;
        }

        public static DataGridViewTextBoxColumn Col(string header, string prop, float weight = 100,
            string? format = null, bool right = false)
        {
            var c = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = prop,
                Name = prop,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Programmatic
            };
            if (format != null) c.DefaultCellStyle.Format = format;
            if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return c;
        }
    }
}
