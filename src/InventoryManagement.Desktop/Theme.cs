namespace InventoryManagement.Desktop
{
    /// <summary>Colors, fonts and small style helpers shared by all screens.</summary>
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(30, 30, 30);
        public static readonly Color Sidebar = Color.FromArgb(37, 37, 38);
        public static readonly Color Card = Color.FromArgb(45, 45, 48);
        public static readonly Color Input = Color.FromArgb(60, 60, 64);
        public static readonly Color Border = Color.FromArgb(70, 70, 76);
        public static readonly Color Text = Color.Gainsboro;
        public static readonly Color Muted = Color.FromArgb(150, 150, 155);
        public static readonly Color Accent = Color.FromArgb(0, 184, 212);
        public static readonly Color Good = Color.FromArgb(46, 139, 87);
        public static readonly Color Danger = Color.FromArgb(198, 40, 40);
        public static readonly Color Warn = Color.FromArgb(240, 160, 0);
        public static readonly Color Neutral = Color.FromArgb(75, 75, 82);

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
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(55, 55, 60);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Bold;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 55, 60);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

            g.RowTemplate.Height = 32;
            g.DefaultCellStyle.BackColor = Card;
            g.DefaultCellStyle.ForeColor = Text;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 105, 130);
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(51, 51, 55);
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
