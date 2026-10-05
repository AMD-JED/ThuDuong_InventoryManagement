using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace InventoryManagement.Desktop
{
    /// <summary>
    /// Backup = one .zip with inventory.db (+ custom_columns.json).
    /// Restore validates the file, makes a safety backup of the current data first, then replaces it.
    /// </summary>
    public sealed class BackupView : UserControl, IRefreshable
    {
        private readonly Label _lblInfo = Theme.MakeLabel("", Theme.Base, Theme.Text);
        public event Action? Restored;

        private static string BackupDir =>
            Path.Combine(Path.GetDirectoryName(Db.FullPath) ?? ".", "backups");

        public BackupView()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            Padding = new Padding(16, 12, 16, 8);

            var header = Theme.MakeLabel("💾 Sauvegarde et restauration", Theme.Title, Color.White);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = 48;

            var card = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Theme.Card, Padding = new Padding(18, 14, 18, 10) };
            _lblInfo.Dock = DockStyle.Fill;
            _lblInfo.AutoSize = false;
            _lblInfo.UseMnemonic = false;
            card.Controls.Add(_lblInfo);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(0, 14, 0, 0) };
            buttons.Controls.Add(Theme.MakeButton("💾 Sauvegarder maintenant", Theme.Good, (_, _) => Backup(), 230));
            buttons.Controls.Add(Theme.MakeButton("♻ Restaurer…", Theme.Accent, (_, _) => Restore(), 150));
            buttons.Controls.Add(Theme.MakeButton("📂 Dossier de sécurité", Theme.Neutral, (_, _) => OpenFolder(), 190));

            var tips = Theme.MakeLabel(
                "• Une sauvegarde contient toutes les pièces, dépôts, mouvements de stock et colonnes personnalisées.\n" +
                "• Avant toute restauration, l'état actuel est sauvegardé automatiquement dans le dossier « backups ».\n" +
                "• Conservez une copie sur une clé USB ou un cloud : une sauvegarde sur le même disque ne protège pas d'une panne du disque.",
                Theme.Base, Theme.Muted);
            tips.AutoSize = false; tips.Dock = DockStyle.Top; tips.Height = 90;

            Controls.Add(tips);
            Controls.Add(buttons);
            Controls.Add(card);
            Controls.Add(header);
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                var fi = new FileInfo(Db.FullPath);
                int products, moves;
                using (var ctx = Db.Create())
                {
                    products = ctx.Products.Count(p => p.IsActive);
                    moves = ctx.StockTransactions.Count();
                }
                string last = Directory.Exists(BackupDir)
                    ? (Directory.GetFiles(BackupDir, "*.zip").Select(f => File.GetLastWriteTime(f))
                        .OrderByDescending(d => d).Select(d => d.ToString("dd/MM/yyyy HH:mm")).FirstOrDefault() ?? "aucune")
                    : "aucune";
                _lblInfo.Text =
                    $"Base de données :  {Db.FullPath}\n" +
                    $"Taille :  {fi.Length / 1024.0:N0} Ko      Dernière modification :  {fi.LastWriteTime:dd/MM/yyyy HH:mm}\n" +
                    $"Contenu :  {products} pièce(s) active(s), {moves} mouvement(s) de stock\n" +
                    $"Dernière sauvegarde de sécurité :  {last}";
            }
            catch (Exception ex) { _lblInfo.Text = "Lecture impossible : " + ex.Message; }
        }

        // ---------------- backup ----------------
        private static void CreateZip(string zipPath)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "inv_" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                // SQLite online-backup API: consistent copy even if the DB is in use.
                using (var src = new SqliteConnection($"Data Source={Db.File};Pooling=False"))
                using (var dst = new SqliteConnection($"Data Source={tmp};Pooling=False"))
                {
                    src.Open(); dst.Open();
                    src.BackupDatabase(dst);
                }
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using var z = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                z.CreateEntryFromFile(tmp, "inventory.db");
                if (File.Exists(CustomColumnStore.FilePath))
                    z.CreateEntryFromFile(CustomColumnStore.FilePath, "custom_columns.json");
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        private void Backup()
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Sauvegarde inventaire (*.zip)|*.zip",
                FileName = $"inventaire_{DateTime.Now:yyyyMMdd_HHmm}.zip"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                UseWaitCursor = true;
                CreateZip(dlg.FileName);
                RefreshData();
                MessageBox.Show(this, "Sauvegarde créée :\n" + dlg.FileName, "Sauvegarde",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Sauvegarde impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { UseWaitCursor = false; }
        }

        // ---------------- restore ----------------
        private static (int Products, int Moves) Validate(string dbFile)
        {
            using var c = new SqliteConnection($"Data Source={dbFile};Mode=ReadOnly;Pooling=False");
            c.Open();

            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check";
                var r = Convert.ToString(cmd.ExecuteScalar());
                if (!string.Equals(r, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Le fichier est corrompu (integrity_check : " + r + ").");
            }

            var tables = new HashSet<string>();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) tables.Add(rd.GetString(0));
            }
            foreach (var t in new[] { "Products", "Warehouses", "InventoryItems", "StockTransactions" })
                if (!tables.Contains(t))
                    throw new InvalidDataException($"Ce fichier n'est pas une base d'inventaire (table « {t} » absente).");

            long Count(string table)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            return ((int)Count("Products"), (int)Count("StockTransactions"));
        }

        private void Restore()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Sauvegarde (*.zip;*.db)|*.zip;*.db",
                Title = "Choisir la sauvegarde à restaurer"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var tmpDir = Path.Combine(Path.GetTempPath(), "inv_restore_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tmpDir);
                string tmpDb = Path.Combine(tmpDir, "inventory.db");
                string tmpJson = Path.Combine(tmpDir, "custom_columns.json");
                bool fromZip = dlg.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                bool hasJson = false;

                if (fromZip)
                {
                    using var z = ZipFile.OpenRead(dlg.FileName);
                    var dbEntry = z.GetEntry("inventory.db")
                        ?? throw new InvalidDataException("Ce .zip ne contient pas « inventory.db ».");
                    dbEntry.ExtractToFile(tmpDb, true);
                    var js = z.GetEntry("custom_columns.json");
                    if (js != null) { js.ExtractToFile(tmpJson, true); hasJson = true; }
                }
                else File.Copy(dlg.FileName, tmpDb, true);

                var (products, moves) = Validate(tmpDb);

                var ok = MessageBox.Show(this,
                    $"Restaurer cette sauvegarde ?\n\nContenu : {products} pièce(s), {moves} mouvement(s).\n\n" +
                    "Les données actuelles seront REMPLACÉES. Une sauvegarde de sécurité de l'état actuel sera créée " +
                    "dans le dossier « backups » avant le remplacement.",
                    "Confirmer la restauration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (ok != DialogResult.Yes) return;

                UseWaitCursor = true;
                Directory.CreateDirectory(BackupDir);
                var safety = Path.Combine(BackupDir, $"avant_restauration_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                CreateZip(safety);

                SqliteConnection.ClearAllPools();
                File.Copy(tmpDb, Db.FullPath, true);
                if (fromZip)
                {
                    if (hasJson) File.Copy(tmpJson, CustomColumnStore.FilePath, true);
                    else if (File.Exists(CustomColumnStore.FilePath)) File.Delete(CustomColumnStore.FilePath);
                }

                Restored?.Invoke();
                RefreshData();
                MessageBox.Show(this, "Restauration terminée.\nSauvegarde de sécurité :\n" + safety,
                    "Restauration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Restauration impossible : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        }

        private void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(BackupDir);
                Process.Start(new ProcessStartInfo { FileName = BackupDir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
