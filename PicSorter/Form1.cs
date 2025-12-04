using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PicSorter
{
    public partial class Form1 : Form
    {
        // ============================
        // FIELD UNTUK LOGIC
        // ============================
        private List<string> _sourceFiles = new List<string>();
        private int _currentIndex = -1;
        private Dictionary<Keys, string> _destinationMap = new Dictionary<Keys, string>();
        private bool _isSorting = false;
        private bool _isMoveOperation = false;
        private Image? _currentImage;

        // Log & history
        private string? _logFilePath;
        private List<SortActionRecord> _history = new List<SortActionRecord>();

        // Mencatat aksi per file, untuk Undo & log
        private class SortActionRecord
        {
            public int Index { get; set; }
            public string SourceFile { get; set; } = "";
            public string Action { get; set; } = "";   // "Copy", "Move", "Skip"
            public string? DestFile { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;             // Form menerima event keyboard
            this.KeyDown += Form1_KeyDown;     // Handler tombol angka / S / Backspace

            // Isi mode jika belum diisi lewat designer
            if (cmbMode.Items.Count == 0)
            {
                cmbMode.Items.Add("Copy");
                cmbMode.Items.Add("Move");
            }
            if (cmbMode.SelectedIndex < 0)
            {
                cmbMode.SelectedIndex = 0;
            }

            lblStatus.Text = "Status: Idle";
        }

        // ============================
        // BROWSE FOLDER SUMBER
        // ============================
        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Pilih folder sumber yang berisi foto atau video";

            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
            {
                txtSourceFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        // ============================
        // ADD DESTINATION FOLDER
        // ============================
        private void btnAddDestination_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialogDest.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialogDest.SelectedPath))
            {
                int count = dgvDestinations.Rows.Count;

                if (count >= 10)
                {
                    MessageBox.Show("Maksimal 10 folder (shortcut 1–0).");
                    return;
                }

                string shortcut = (count + 1).ToString();
                if (shortcut == "10")
                    shortcut = "0";

                dgvDestinations.Rows.Add(shortcut, folderBrowserDialogDest.SelectedPath);
            }
        }

        // ============================
        // CLEAR DESTINATION FOLDERS
        // ============================
        private void btnClearDestination_Click(object sender, EventArgs e)
        {
            dgvDestinations.Rows.Clear();
        }

        // ============================
        // START SORTING BARU (from scratch)
        // ============================
        private void btnStartSorting_Click(object sender, EventArgs e)
        {
            if (!ValidateSourceAndDest())
                return;

            LoadSourceFiles();
            if (_sourceFiles.Count == 0)
            {
                MessageBox.Show("Tidak ada file gambar yang ditemukan di folder sumber.");
                return;
            }

            BuildDestinationMap();
            PrepareNewSessionLog();

            _isMoveOperation = (cmbMode.SelectedItem?.ToString() == "Move");
            _isSorting = true;
            _currentIndex = 0;
            _history.Clear();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = _sourceFiles.Count;
            progressBar1.Value = 0;

            lblStatus.Text = "Status: Sorting in progress...";
            ShowCurrentFile();
        }

        // ============================
        // CONTINUE FROM LOG
        // ============================
        private void btnContinueFromLog_Click(object sender, EventArgs e)
        {
            if (!ValidateSourceAndDest())
                return;

            LoadSourceFiles();
            if (_sourceFiles.Count == 0)
            {
                MessageBox.Show("Tidak ada file gambar yang ditemukan di folder sumber.");
                return;
            }

            BuildDestinationMap();
            PrepareNewSessionLog(); // lanjut append ke file yang sama

            // Filter berdasarkan log: hanya file yang
            // - belum pernah ada di log, atau
            // - terakhir statusnya SKIP
            ApplyLogFilterToSourceFiles();

            if (_sourceFiles.Count == 0)
            {
                MessageBox.Show("Semua file di folder sumber sudah di-copy/move (tidak ada yang perlu dilanjutkan).");
                return;
            }

            _isMoveOperation = (cmbMode.SelectedItem?.ToString() == "Move");
            _isSorting = true;
            _currentIndex = 0;
            _history.Clear();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = _sourceFiles.Count;
            progressBar1.Value = 0;

            lblStatus.Text = "Status: Continue sorting from log...";
            ShowCurrentFile();
        }

        // Validasi folder sumber & destinasi
        private bool ValidateSourceAndDest()
        {
            if (string.IsNullOrWhiteSpace(txtSourceFolder.Text) || !Directory.Exists(txtSourceFolder.Text))
            {
                MessageBox.Show("Pilih folder sumber yang valid terlebih dahulu.");
                return false;
            }

            if (dgvDestinations.Rows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal satu folder tujuan.");
                return false;
            }

            return true;
        }

        // Ambil semua file gambar dari folder sumber
        private void LoadSourceFiles()
        {
            string source = txtSourceFolder.Text;
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

            _sourceFiles = Directory
                .GetFiles(source)
                .Where(f => allowedExt.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToList();
        }

        // Bangun mapping Keys (D1, D2, dst) ke folder tujuan
        private void BuildDestinationMap()
        {
            _destinationMap.Clear();

            foreach (DataGridViewRow row in dgvDestinations.Rows)
            {
                if (row.IsNewRow) continue;

                string shortcut = Convert.ToString(row.Cells["ShortcutCol"].Value);
                string folder = Convert.ToString(row.Cells["FolderCol"].Value);

                if (string.IsNullOrWhiteSpace(shortcut) || string.IsNullOrWhiteSpace(folder))
                    continue;

                Keys key;
                switch (shortcut)
                {
                    case "1": key = Keys.D1; break;
                    case "2": key = Keys.D2; break;
                    case "3": key = Keys.D3; break;
                    case "4": key = Keys.D4; break;
                    case "5": key = Keys.D5; break;
                    case "6": key = Keys.D6; break;
                    case "7": key = Keys.D7; break;
                    case "8": key = Keys.D8; break;
                    case "9": key = Keys.D9; break;
                    case "0": key = Keys.D0; break;
                    default: continue;
                }

                _destinationMap[key] = folder;
            }
        }

        // Siapkan path file log (di folder sumber) dan header jika perlu
        private void PrepareNewSessionLog()
        {
            _logFilePath = Path.Combine(txtSourceFolder.Text, "sorting_log.csv");
            if (!File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, "Timestamp;Action;SourceFile;DestFile" + Environment.NewLine);
            }
            // Jika sudah ada, kita lanjut append (riwayat lama dipertahankan)
        }

        // Append satu baris ke log
        private void AppendLog(string action, string sourceFile, string? destFile)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{action};{sourceFile};{destFile ?? ""}";
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }

        // Tampilkan file saat ini di PictureBox
        private void ShowCurrentFile()
        {
            // Bersihkan image sebelumnya
            if (_currentImage != null)
            {
                picPreview.Image = null;
                _currentImage.Dispose();
                _currentImage = null;
            }

            if (_currentIndex < 0 || _currentIndex >= _sourceFiles.Count)
            {
                lblFileName.Text = "File: -";
                lblIndex.Text = "0 / 0";
                lblStatus.Text = "Status: Finished";
                _isSorting = false;
                return;
            }

            string filePath = _sourceFiles[_currentIndex];
            lblFileName.Text = "File: " + Path.GetFileName(filePath);
            lblIndex.Text = $"{_currentIndex + 1} / {_sourceFiles.Count}";

            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    _currentImage = Image.FromStream(fs);
                }
                picPreview.Image = _currentImage;
            }
            else
            {
                picPreview.Image = null;
                lblStatus.Text = "Status: File bukan gambar (preview belum didukung).";
            }
        }

        // ============================
        // HANDLER KEYBOARD
        // 1–0  = Copy/Move ke folder
        // S    = Skip
        // Back = Undo
        // ============================
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_isSorting)
                return;

            // UNDO
            if (e.KeyCode == Keys.Back)
            {
                HandleUndo();
                return;
            }

            // SKIP
            if (e.KeyCode == Keys.S)
            {
                HandleSkip();
                return;
            }

            // COPY/MOVE DENGAN ANGKA
            if (_destinationMap.ContainsKey(e.KeyCode))
            {
                HandleCopyOrMove(e.KeyCode);
                return;
            }
        }

        private void HandleCopyOrMove(Keys key)
        {
            if (_currentIndex < 0 || _currentIndex >= _sourceFiles.Count)
                return;

            string sourceFile = _sourceFiles[_currentIndex];
            string destFolder = _destinationMap[key];

            try
            {
                string destPath = Path.Combine(destFolder, Path.GetFileName(sourceFile));
                destPath = GetUniqueFilePath(destPath);

                if (_isMoveOperation)
                {
                    File.Move(sourceFile, destPath);
                    AppendLog("Move", sourceFile, destPath);
                    AddHistory("Move", sourceFile, destPath);
                }
                else
                {
                    File.Copy(sourceFile, destPath);
                    AppendLog("Copy", sourceFile, destPath);
                    AddHistory("Copy", sourceFile, destPath);
                }

                if (progressBar1.Value < progressBar1.Maximum)
                    progressBar1.Value += 1;

                _currentIndex++;
                ShowCurrentFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memproses file: " + ex.Message);
            }
        }

        private void HandleSkip()
        {
            if (_currentIndex < 0 || _currentIndex >= _sourceFiles.Count)
                return;

            string sourceFile = _sourceFiles[_currentIndex];

            AppendLog("Skip", sourceFile, null);
            AddHistory("Skip", sourceFile, null);

            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value += 1;

            _currentIndex++;
            ShowCurrentFile();
        }

        private void HandleUndo()
        {
            // Undo hanya 1 langkah terakhir
            if (_history.Count == 0)
                return;

            var last = _history[_history.Count - 1];

            try
            {
                if (last.Action == "Copy" && !string.IsNullOrEmpty(last.DestFile))
                {
                    // Hapus file hasil copy
                    if (File.Exists(last.DestFile))
                    {
                        File.Delete(last.DestFile);
                    }
                    AppendLog("UndoCopy", last.SourceFile, last.DestFile);
                }
                else if (last.Action == "Move" && !string.IsNullOrEmpty(last.DestFile))
                {
                    // Pindahkan kembali dari dest ke source
                    if (File.Exists(last.DestFile))
                    {
                        File.Move(last.DestFile, last.SourceFile);
                    }
                    AppendLog("UndoMove", last.SourceFile, last.DestFile);
                }
                else if (last.Action == "Skip")
                {
                    // Skip tidak mengubah file di disk, cukup log
                    AppendLog("UndoSkip", last.SourceFile, null);
                }

                // Kembalikan index
                _currentIndex = last.Index;

                // Sesuaikan progress bar (mundur 1 langkah)
                if (progressBar1.Value > 0)
                    progressBar1.Value -= 1;

                // Hapus history terakhir
                _history.RemoveAt(_history.Count - 1);

                // Tampilkan kembali file tersebut
                _isSorting = true;
                ShowCurrentFile();
                lblStatus.Text = "Status: Undo last action";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal melakukan undo: " + ex.Message);
            }
        }

        private void AddHistory(string action, string sourceFile, string? destFile)
        {
            _history.Add(new SortActionRecord
            {
                Index = _currentIndex,
                SourceFile = sourceFile,
                Action = action,
                DestFile = destFile
            });
        }

        // Jika nama file sudah ada di folder tujuan, tambahkan (1), (2), dst
        private string GetUniqueFilePath(string initialPath)
        {
            if (!File.Exists(initialPath))
                return initialPath;

            string dir = Path.GetDirectoryName(initialPath)!;
            string name = Path.GetFileNameWithoutExtension(initialPath);
            string ext = Path.GetExtension(initialPath);

            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{name} ({counter}){ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        // Baca sorting_log.csv dan filter _sourceFiles
        // Hanya ambil file yang:
        // - Tidak ada di log (belum pernah diproses)
        // - Atau last action = Skip
        private void ApplyLogFilterToSourceFiles()
        {
            _logFilePath = Path.Combine(txtSourceFolder.Text, "sorting_log.csv");
            if (!File.Exists(_logFilePath))
                return; // Tidak ada log, berarti tidak perlu filter

            var lastState = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var lines = File.ReadAllLines(_logFilePath);
            // Skip header
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length < 4) continue;

                string action = parts[1];
                string sourceFile = parts[2];

                // Update state terakhir per file
                // Copy/Move/Skip/UndoCopy/UndoMove/UndoSkip
                switch (action)
                {
                    case "Copy":
                    case "Move":
                    case "Skip":
                        lastState[sourceFile] = action;
                        break;
                    case "UndoCopy":
                    case "UndoMove":
                    case "UndoSkip":
                        // Undo artinya kembali ke "belum diproses"
                        if (lastState.ContainsKey(sourceFile))
                            lastState.Remove(sourceFile);
                        break;
                }
            }

            _sourceFiles = _sourceFiles
                .Where(f =>
                {
                    if (!lastState.TryGetValue(f, out var state))
                    {
                        // belum pernah ada di log
                        return true;
                    }

                    // Hanya file yang terakhir statusnya Skip yang perlu dilanjutkan
                    return state == "Skip";
                })
                .ToList();
        }

        // Handler lama yang tidak terpakai boleh dibiarkan kosong
        private void label1_Click(object sender, EventArgs e)
        {
        }
    }
}
