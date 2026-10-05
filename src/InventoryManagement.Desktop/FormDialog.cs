namespace InventoryManagement.Desktop
{
    public sealed class DialogField
    {
        public string Label { get; }
        public string Value { get; }
        public string[]? Options { get; }
        public DialogField(string label, string value = "", string[]? options = null)
        {
            Label = label; Value = value; Options = options;
        }
    }

    /// <summary>Small reusable dark dialog: a list of labelled fields + validation.</summary>
    public sealed class FormDialog : Form
    {
        private readonly List<Control> _inputs = new();
        private readonly Func<string[], string?>? _validate;
        public string[] Values { get; private set; } = Array.Empty<string>();

        private FormDialog(string title, IReadOnlyList<DialogField> fields,
            Func<string[], string?>? validate, string okText)
        {
            _validate = validate;
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Base;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(18, 16, 18, 4)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));

            foreach (var f in fields)
            {
                Control input;
                if (f.Options != null)
                {
                    var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 270 };
                    cb.Items.AddRange(f.Options.Cast<object>().ToArray());
                    cb.Text = f.Value;
                    input = cb;
                }
                else input = new TextBox { Text = f.Value, Width = 270 };

                Theme.StyleInput(input);
                input.Margin = new Padding(0, 6, 0, 6);
                var lbl = Theme.MakeLabel(f.Label);
                lbl.Margin = new Padding(0, 10, 0, 6);
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(lbl);
                table.Controls.Add(input);
                _inputs.Add(input);
            }

            var ok = Theme.MakeButton(okText, Theme.Good, null, 120);
            var cancel = Theme.MakeButton("Annuler", Theme.Neutral, null, 100);
            cancel.DialogResult = DialogResult.Cancel;
            ok.Click += (_, _) => Confirm();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 56,
                Padding = new Padding(14, 10, 10, 0)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(buttons);
            Controls.Add(table);
            AcceptButton = ok;
            CancelButton = cancel;
            ClientSize = new Size(460, Math.Min(fields.Count * 44 + 100, 720));
        }

        private void Confirm()
        {
            var vals = _inputs.Select(c => c.Text.Trim()).ToArray();
            var err = _validate?.Invoke(vals);
            if (err != null)
            {
                MessageBox.Show(this, err, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Values = vals;
            DialogResult = DialogResult.OK;
        }

        /// <returns>The entered values, or null if cancelled.</returns>
        public static string[]? Ask(IWin32Window owner, string title, IReadOnlyList<DialogField> fields,
            Func<string[], string?>? validate = null, string okText = "Enregistrer")
        {
            using var d = new FormDialog(title, fields, validate, okText);
            return d.ShowDialog(owner) == DialogResult.OK ? d.Values : null;
        }
    }

    /// <summary>Horizontal bar chart drawn with GDI+ (no external library).</summary>
    public sealed class BarChart : Control
    {
        public string Heading { get; set; } = "";
        public List<(string Label, int Value)> Data { get; set; } = new();

        public BarChart()
        {
            DoubleBuffered = true;
            BackColor = Theme.Card;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var headFont = new Font("Segoe UI Semibold", 11f);
            using var textBrush = new SolidBrush(Theme.Text);
            using var mutedBrush = new SolidBrush(Theme.Muted);
            using var barBrush = new SolidBrush(Theme.Accent);

            g.DrawString(Heading, headFont, textBrush, 14, 10);
            if (Data.Count == 0)
            {
                g.DrawString("Aucune donnée", Theme.Base, mutedBrush, 14, 48);
                return;
            }

            int max = Math.Max(1, Data.Max(d => d.Value));
            int top = 48, rowH = 30, labelW = 150, right = 70;
            int barMax = Math.Max(20, Width - labelW - right - 20);
            for (int i = 0; i < Data.Count; i++)
            {
                int y = top + i * rowH;
                if (y + rowH > Height) break;
                var (label, value) = Data[i];
                g.DrawString(label, Theme.Base, textBrush, new RectangleF(14, y + 3, labelW - 8, rowH));
                int w = (int)Math.Round((double)value / max * barMax);
                g.FillRectangle(barBrush, labelW + 14, y + 5, Math.Max(w, 3), rowH - 12);
                g.DrawString(value.ToString(), Theme.Base, mutedBrush, labelW + 20 + w, y + 3);
            }
        }
    }
}
