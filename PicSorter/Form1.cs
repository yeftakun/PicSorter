using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace PicSorter
{
    // ============================
    // MODEL STATE JSON
    // ============================
    public class DestinationFolderInfo
    {
        public string Shortcut { get; set; } = "";
        public string FolderPath { get; set; } = "";
    }

    public class SortItemState
    {
        public string SourcePath { get; set; } = "";
        public bool IsVideo { get; set; }
        public bool Sorted { get; set; } = false;           // true = sudah diputuskan (ada tujuan)
        public string? DestFolderPath { get; set; }         // null jika belum di-assign
        public string? LastAction { get; set; }             // "Assign", "Skip", "UndoAssign", dll (informasi tambahan)
        public bool Committed { get; set; } = false;        // true = sudah diproses saat Save (Copy/Move)
    }

    public class SortState
    {
        public string SourceFolder { get; set; } = "";
        public string Mode { get; set; } = "Copy";          // "Copy" / "Move"
        public List<DestinationFolderInfo> Destinations { get; set; } = new();
        public List<SortItemState> Items { get; set; } = new();
    }

    // ============================
    // FORM
    // ============================
    public partial class Form1 : Form
    {
        // STATE DALAM MEMORI
        private SortState? _state;
        private string? _stateFilePath;

        private List<SortItemState> _items = new List<SortItemState>();
        private int _currentIndex = -1;
        private Dictionary<Keys, string> _destinationMap = new Dictionary<Keys, string>();
        private bool _isSorting = false;
        private Image? _currentImage;

        // Riwayat untuk Undo (hanya 1 langkah ke belakang)
        private class SortActionRecord
        {
            public int Index { get; set; }
            public string Action { get; set; } = "";   // "Assign", "Skip"
        }

        private List<SortActionRecord> _history = new List<SortActionRecord>();

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
        // ADD / CLEAR DESTINATION FOLDER
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

        private void btnClearDestination_Click(object sender, EventArgs e)
        {
            dgvDestinations.Rows.Clear();
        }

        // ============================
        // START SORTING BARU (STATE BARU)
        // ============================
        private void btnStartSorting_Click(object sender, EventArgs e)
        {
            if (!ValidateSourceAndDest())
                return;

            string source = txtSourceFolder.Text;
            _stateFilePath = Path.Combine(source, "sorting_state.json");

            // Scan semua file (gambar + video)
            var imageExt = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".jfif" };
            var videoExt = new[] { ".mp4", ".mov", ".avi", ".mkv", ".wmv" };

            var allFiles = Directory
                .GetFiles(source)
                .Where(f =>
                {
                    string ext = Path.GetExtension(f).ToLower();
                    return imageExt.Contains(ext) || videoExt.Contains(ext);
                })
                .OrderBy(f => f)
                .ToList();

            if (allFiles.Count == 0)
            {
                MessageBox.Show("Tidak ada file gambar/video yang ditemukan di folder sumber.");
                return;
            }

            // Bangun destinasi dari UI
            var destinations = BuildDestinationList();
            if (destinations.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal satu folder tujuan.");
                return;
            }

            // Buat state baru
            _state = new SortState
            {
                SourceFolder = source,
                Mode = cmbMode.SelectedItem?.ToString() ?? "Copy",
                Destinations = destinations,
                Items = allFiles.Select(path => new SortItemState
                {
                    SourcePath = path,
                    IsVideo = videoExt.Contains(Path.GetExtension(path).ToLower()),
                    Sorted = false,
                    DestFolderPath = null,
                    LastAction = null,
                    Committed = false
                }).ToList()
            };

            _items = _state.Items;
            _history.Clear();
            _isSorting = true;

            BuildDestinationMap(); // dari UI
            SaveStateToJson();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = _items.Count;
            progressBar1.Value = 0;

            lblStatus.Text = "Status: Sorting in progress (state baru)...";

            // Mulai dari item pertama yang belum di-sort
            MoveToNextPendingFrom(-1);
        }

        // ============================
        // CONTINUE FROM STATE (JSON)
        // ============================
        private void btnContinueFromLog_Click(object sender, EventArgs e)
        {
            if (!ValidateSourceAndDest())
                return;

            string source = txtSourceFolder.Text;
            _stateFilePath = Path.Combine(source, "sorting_state.json");

            if (!File.Exists(_stateFilePath))
            {
                MessageBox.Show("File state (sorting_state.json) tidak ditemukan di folder sumber.\nSilakan mulai dengan 'Start Sorting' terlebih dahulu.");
                return;
            }

            LoadStateFromJson();

            if (_state == null || _state.Items == null || _state.Items.Count == 0)
            {
                MessageBox.Show("State kosong atau tidak valid.");
                return;
            }

            // Jika SourceFolder di state berbeda dengan folder saat ini, beri peringatan ringan saja
            if (!string.Equals(_state.SourceFolder, source, StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    "Folder sumber di state berbeda dengan folder yang dipilih saat ini.\nTetap lanjut dengan state yang ada?",
                    "Peringatan",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No) return;
            }

            // Sinkronkan mode dan destinasi dengan UI saat ini
            _state.Mode = cmbMode.SelectedItem?.ToString() ?? "Copy";
            _state.Destinations = BuildDestinationList();
            _items = _state.Items;

            BuildDestinationMap();
            SaveStateToJson();   // simpan perubahan konfigurasi

            _history.Clear();
            _isSorting = true;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = _items.Count;
            progressBar1.Value = 0;

            lblStatus.Text = "Status: Continue sorting from state...";

            MoveToNextPendingFrom(-1);
        }

        // ============================
        // VALIDASI & HELPER DESTINASI
        // ============================
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

        private List<DestinationFolderInfo> BuildDestinationList()
        {
            var list = new List<DestinationFolderInfo>();

            foreach (DataGridViewRow row in dgvDestinations.Rows)
            {
                if (row.IsNewRow) continue;

                string shortcut = Convert.ToString(row.Cells["ShortcutCol"].Value) ?? "";
                string folder = Convert.ToString(row.Cells["FolderCol"].Value) ?? "";

                if (string.IsNullOrWhiteSpace(shortcut) || string.IsNullOrWhiteSpace(folder))
                    continue;

                list.Add(new DestinationFolderInfo
                {
                    Shortcut = shortcut,
                    FolderPath = folder
                });
            }

            return list;
        }

        private void BuildDestinationMap()
        {
            _destinationMap.Clear();

            foreach (DataGridViewRow row in dgvDestinations.Rows)
            {
                if (row.IsNewRow) continue;

                string shortcut = Convert.ToString(row.Cells["ShortcutCol"].Value) ?? "";
                string folder = Convert.ToString(row.Cells["FolderCol"].Value) ?? "";

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

        // ============================
        // SIMPAN / LOAD STATE JSON
        // ============================
        private void SaveStateToJson()
        {
            if (_state == null || string.IsNullOrEmpty(_stateFilePath))
                return;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            try
            {
                string json = JsonSerializer.Serialize(_state, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan state ke JSON: " + ex.Message);
            }
        }

        private void LoadStateFromJson()
        {
            if (string.IsNullOrEmpty(_stateFilePath) || !File.Exists(_stateFilePath))
                return;

            try
            {
                string json = File.ReadAllText(_stateFilePath);
                _state = JsonSerializer.Deserialize<SortState>(json) ?? new SortState();
                _items = _state.Items ?? new List<SortItemState>();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membaca state dari JSON: " + ex.Message);
                _state = new SortState();
                _items = new List<SortItemState>();
            }
        }

        // ============================
        // NAVIGASI ITEM
        // ============================
        private void MoveToNextPendingFrom(int startIndex)
        {
            if (_items.Count == 0)
            {
                _currentIndex = -1;
                ShowCurrentFile();
                return;
            }

            int idx = startIndex;

            while (true)
            {
                idx++;

                if (idx >= _items.Count)
                {
                    // Tidak ada lagi item yang Sorted == false
                    _currentIndex = -1;
                    ShowCurrentFile();
                    return;
                }

                if (!_items[idx].Sorted)
                {
                    _currentIndex = idx;
                    ShowCurrentFile();
                    return;
                }
            }
        }

        // ============================
        // TAMPILKAN FILE SAAT INI
        // ============================
        private void ShowCurrentFile()
        {
            // Bersihkan image sebelumnya
            if (_currentImage != null)
            {
                picPreview.Image = null;
                _currentImage.Dispose();
                _currentImage = null;
            }

            if (_currentIndex < 0 || _currentIndex >= _items.Count)
            {
                lblFileName.Text = "File: -";
                lblIndex.Text = "0 / 0";
                lblStatus.Text = "Status: Finished (tidak ada file pending)";
                _isSorting = false;
                return;
            }

            var item = _items[_currentIndex];
            string filePath = item.SourcePath;

            lblFileName.Text = "File: " + Path.GetFileName(filePath);
            lblIndex.Text = $"{_currentIndex + 1} / {_items.Count}";

            string ext = Path.GetExtension(filePath).ToLower();

            // Jika file gambar → load ke PictureBox
            if (!item.IsVideo &&
                (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".jfif"))
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        _currentImage = Image.FromStream(fs);
                    }
                    picPreview.Image = _currentImage;
                    lblStatus.Text = "Status: Menampilkan gambar";
                }
                catch (Exception ex)
                {
                    picPreview.Image = null;
                    lblStatus.Text = "Status: Gagal memuat gambar: " + ex.Message;
                }
            }
            else
            {
                // File video (atau format lain yang belum didukung preview)
                picPreview.Image = null;
                lblStatus.Text = "Status: Video file (preview belum tersedia).";
            }
        }

        // ============================
        // HANDLER KEYBOARD
        // 1–0  = Assign ke folder (ubah state JSON)
        // S    = Skip (state tetap Sorted=false)
        // Back = Undo 1 langkah
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

            // ASSIGN DENGAN ANGKA
            if (_destinationMap.ContainsKey(e.KeyCode))
            {
                HandleAssign(e.KeyCode);
                return;
            }
        }

        private void HandleAssign(Keys key)
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count)
                return;

            var item = _items[_currentIndex];

            if (!_destinationMap.TryGetValue(key, out string destFolder))
                return;

            // Hanya mengubah state (belum memindah file)
            item.DestFolderPath = destFolder;
            item.Sorted = true;
            item.LastAction = "Assign";

            _history.Add(new SortActionRecord
            {
                Index = _currentIndex,
                Action = "Assign"
            });

            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value += 1;

            SaveStateToJson();

            MoveToNextPendingFrom(_currentIndex);
        }

        private void HandleSkip()
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count)
                return;

            var item = _items[_currentIndex];

            // Skip = user belum ingin memutuskan.
            // Sorted tetap false, tapi kita catat LastAction = "Skip"
            item.LastAction = "Skip";

            _history.Add(new SortActionRecord
            {
                Index = _currentIndex,
                Action = "Skip"
            });

            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value += 1;

            SaveStateToJson();

            MoveToNextPendingFrom(_currentIndex);
        }

        private void HandleUndo()
        {
            if (_history.Count == 0)
                return;

            var last = _history[_history.Count - 1];

            if (last.Index < 0 || last.Index >= _items.Count)
            {
                _history.RemoveAt(_history.Count - 1);
                return;
            }

            var item = _items[last.Index];

            if (last.Action == "Assign")
            {
                // Kembalikan ke kondisi belum diputuskan
                item.Sorted = false;
                item.DestFolderPath = null;
                item.LastAction = "UndoAssign";
            }
            else if (last.Action == "Skip")
            {
                // Kembalikan dari Skip → belum ada aksi
                item.LastAction = "UndoSkip";
                // Sorted memang sudah false dari awal
            }

            _history.RemoveAt(_history.Count - 1);

            if (progressBar1.Value > 0)
                progressBar1.Value -= 1;

            _currentIndex = last.Index;
            _isSorting = true;

            SaveStateToJson();
            ShowCurrentFile();
            lblStatus.Text = "Status: Undo last action";
        }

        // ============================
        // SAVE (APPLY CHANGES) → COPY/MOVE SEBENARNYA
        // ============================
        private void btnSavePlan_Click(object sender, EventArgs e)
        {
            if (_state == null || _items.Count == 0)
            {
                MessageBox.Show("Tidak ada state aktif. Mulai sorting terlebih dahulu.");
                return;
            }

            if (string.IsNullOrEmpty(_state.SourceFolder) || !Directory.Exists(_state.SourceFolder))
            {
                MessageBox.Show("Folder sumber pada state tidak ditemukan.");
                return;
            }

            bool isMove = (cmbMode.SelectedItem?.ToString() == "Move");
            _state.Mode = isMove ? "Move" : "Copy";

            int appliedCount = 0;

            foreach (var item in _state.Items)
            {
                // Hanya proses item yang:
                // - Sorted == true (sudah di-assign)
                // - DestFolderPath != null
                // - Belum Committed (belum pernah diproses Save)
                if (!item.Sorted || item.Committed) continue;
                if (string.IsNullOrEmpty(item.DestFolderPath)) continue;

                string sourcePath = item.SourcePath;
                string destFolder = item.DestFolderPath;

                try
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    string destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath));
                    destPath = GetUniqueFilePath(destPath);

                    if (File.Exists(sourcePath))
                    {
                        if (isMove)
                        {
                            File.Move(sourcePath, destPath);
                        }
                        else
                        {
                            File.Copy(sourcePath, destPath);
                        }

                        item.Committed = true;
                        appliedCount++;
                    }
                    else
                    {
                        // File sumber sudah hilang, lewati saja
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal memproses file:\n{sourcePath}\n\n{ex.Message}");
                }
            }

            SaveStateToJson();
            MessageBox.Show($"Save selesai. {appliedCount} file diproses ({_state.Mode}).");
        }

        // Jika nama file sudah ada di folder tujuan, tambahkan (1), (2), dst
        private string GetUniqueFilePath(string initialPath)
        {
            if (!File.Exists(initialPath))
                return initialPath;

            string? dir = Path.GetDirectoryName(initialPath);
            if (dir == null) return initialPath;

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

        // Handler lama yang tidak terpakai boleh dibiarkan kosong
        private void label1_Click(object sender, EventArgs e)
        {
        }
    }
}
