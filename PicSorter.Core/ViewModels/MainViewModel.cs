using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicSorter.Core.Models;
using PicSorter.Core.Services;

namespace PicSorter.Core.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FileScanService _scanService;
        private readonly SortStateService _stateService;
        private readonly FileOperationService _operationService;

        public MainViewModel()
        {
            _scanService = new FileScanService();
            _stateService = new SortStateService();
            _operationService = new FileOperationService();

            Modes = new ObservableCollection<string> { "Copy", "Move" };
            SelectedMode = Modes[0];
            Destinations = new ObservableCollection<DestinationFolderInfo>();
        }

        [ObservableProperty]
        private string _sourceFolder = "";

        public ObservableCollection<string> Modes { get; }

        [ObservableProperty]
        private string _selectedMode;

        public ObservableCollection<DestinationFolderInfo> Destinations { get; }

        [ObservableProperty]
        private string _statusText = "Status: Idle";

        [ObservableProperty]
        private string _progressText = "0 / 0";

        [ObservableProperty]
        private string _fileName = "File: -";

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private double _progressMaximum = 100;

        [ObservableProperty]
        private byte[]? _currentImageBytes;

        private SortState? _state;
        private List<SortItemState> _items = new();
        private int _currentIndex = -1;
        private List<SortActionRecord> _history = new();
        private bool _isSorting = false;
        private Dictionary<string, string> _destinationMap = new();

        public Action<string>? ShowMessage { get; set; }
        public Func<string, string>? BrowseFolderDialog { get; set; }

        [RelayCommand]
        private void BrowseSource()
        {
            if (BrowseFolderDialog != null)
            {
                var folder = BrowseFolderDialog("Pilih folder sumber yang berisi foto atau video");
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    SourceFolder = folder;
                }
            }
        }

        [RelayCommand]
        private void AddDestination()
        {
            if (Destinations.Count >= 10)
            {
                ShowMessage?.Invoke("Maksimal 10 folder (shortcut 1–0).");
                return;
            }

            if (BrowseFolderDialog != null)
            {
                var folder = BrowseFolderDialog("Pilih folder tujuan");
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    string shortcut = (Destinations.Count + 1).ToString();
                    if (shortcut == "10") shortcut = "0";
                    Destinations.Add(new DestinationFolderInfo { Shortcut = shortcut, FolderPath = folder });
                }
            }
        }

        [RelayCommand]
        private void ClearDestinations()
        {
            Destinations.Clear();
        }

        private bool ValidateSourceAndDest()
        {
            if (string.IsNullOrWhiteSpace(SourceFolder) || !Directory.Exists(SourceFolder))
            {
                ShowMessage?.Invoke("Pilih folder sumber yang valid terlebih dahulu.");
                return false;
            }

            if (Destinations.Count == 0)
            {
                ShowMessage?.Invoke("Tambahkan minimal satu folder tujuan.");
                return false;
            }

            return true;
        }

        private void BuildDestinationMap()
        {
            _destinationMap.Clear();
            foreach (var dest in Destinations)
            {
                if (!string.IsNullOrWhiteSpace(dest.Shortcut) && !string.IsNullOrWhiteSpace(dest.FolderPath))
                {
                    _destinationMap[dest.Shortcut] = dest.FolderPath;
                }
            }
        }

        [RelayCommand]
        private async Task StartSortingAsync()
        {
            if (!ValidateSourceAndDest()) return;

            var allFiles = new List<string>();
            await foreach (var file in _scanService.ScanFolderAsync(SourceFolder, false))
            {
                allFiles.Add(file);
            }

            if (allFiles.Count == 0)
            {
                ShowMessage?.Invoke("Tidak ada file gambar/video yang ditemukan di folder sumber.");
                return;
            }

            _state = new SortState
            {
                SourceFolder = SourceFolder,
                Mode = SelectedMode,
                Destinations = Destinations.ToList(),
                Items = allFiles.Select(path => new SortItemState
                {
                    SourcePath = path,
                    IsVideo = _scanService.IsVideo(path),
                    Sorted = false,
                    DestFolderPath = null,
                    LastAction = null,
                    Committed = false
                }).ToList()
            };

            _items = _state.Items;
            _history.Clear();
            _isSorting = true;
            BuildDestinationMap();

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            await _stateService.SaveStateAsync(stateFile, _state);

            ProgressMaximum = _items.Count;
            ProgressValue = 0;
            StatusText = "Status: Sorting in progress (state baru)...";

            await MoveToNextPendingFromAsync(-1);
        }

        [RelayCommand]
        private async Task ContinueFromLogAsync()
        {
            if (!ValidateSourceAndDest()) return;

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            var state = await _stateService.LoadStateAsync(stateFile);

            if (state == null || state.Items == null || state.Items.Count == 0)
            {
                ShowMessage?.Invoke("File state (sorting_state.json) tidak ditemukan atau kosong.");
                return;
            }

            _state = state;
            _state.Mode = SelectedMode;
            _state.Destinations = Destinations.ToList();
            _items = _state.Items;
            
            BuildDestinationMap();
            await _stateService.SaveStateAsync(stateFile, _state);

            _history.Clear();
            _isSorting = true;

            ProgressMaximum = _items.Count;
            ProgressValue = _items.Count(i => i.Sorted);
            StatusText = "Status: Continue sorting from state...";

            await MoveToNextPendingFromAsync(-1);
        }

        private async Task MoveToNextPendingFromAsync(int startIndex)
        {
            if (_items.Count == 0)
            {
                _currentIndex = -1;
                await ShowCurrentFileAsync();
                return;
            }

            int idx = startIndex;
            while (true)
            {
                idx++;
                if (idx >= _items.Count)
                {
                    _currentIndex = -1;
                    await ShowCurrentFileAsync();
                    return;
                }

                if (!_items[idx].Sorted)
                {
                    _currentIndex = idx;
                    await ShowCurrentFileAsync();
                    return;
                }
            }
        }

        private async Task ShowCurrentFileAsync()
        {
            CurrentImageBytes = null;

            if (_currentIndex < 0 || _currentIndex >= _items.Count)
            {
                FileName = "File: -";
                ProgressText = "0 / 0";
                StatusText = "Status: Finished (tidak ada file pending)";
                _isSorting = false;
                return;
            }

            var item = _items[_currentIndex];
            FileName = "File: " + Path.GetFileName(item.SourcePath);
            ProgressText = $"{_currentIndex + 1} / {_items.Count}";

            if (!item.IsVideo)
            {
                try
                {
                    using var fs = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var ms = new MemoryStream();
                    await fs.CopyToAsync(ms);
                    CurrentImageBytes = ms.ToArray();
                    StatusText = "Status: Menampilkan gambar";
                }
                catch (Exception ex)
                {
                    StatusText = "Status: Gagal memuat gambar: " + ex.Message;
                }
            }
            else
            {
                StatusText = "Status: Video file (preview belum tersedia).";
            }
        }

        public async Task TryGetCommandForKey(string keyStr)
        {
            if (!_isSorting) return;

            if (keyStr == "Back")
            {
                await HandleUndoAsync();
                return;
            }
            if (keyStr == "S")
            {
                await HandleSkipAsync();
                return;
            }
            
            if (_destinationMap.TryGetValue(keyStr, out string? destFolder))
            {
                await HandleAssignAsync(destFolder);
            }
        }

        private async Task HandleAssignAsync(string destFolder)
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count) return;

            var item = _items[_currentIndex];
            item.DestFolderPath = destFolder;
            item.Sorted = true;
            item.LastAction = "Assign";

            _history.Add(new SortActionRecord { Index = _currentIndex, Action = "Assign" });

            if (ProgressValue < ProgressMaximum) ProgressValue++;

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            await _stateService.SaveStateAsync(stateFile, _state!);

            await MoveToNextPendingFromAsync(_currentIndex);
        }

        private async Task HandleSkipAsync()
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count) return;

            var item = _items[_currentIndex];
            item.LastAction = "Skip";
            _history.Add(new SortActionRecord { Index = _currentIndex, Action = "Skip" });

            if (ProgressValue < ProgressMaximum) ProgressValue++;

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            await _stateService.SaveStateAsync(stateFile, _state!);

            await MoveToNextPendingFromAsync(_currentIndex);
        }

        private async Task HandleUndoAsync()
        {
            if (_history.Count == 0) return;

            var last = _history[^1];
            if (last.Index < 0 || last.Index >= _items.Count)
            {
                _history.RemoveAt(_history.Count - 1);
                return;
            }

            var item = _items[last.Index];
            if (last.Action == "Assign")
            {
                item.Sorted = false;
                item.DestFolderPath = null;
                item.LastAction = "UndoAssign";
            }
            else if (last.Action == "Skip")
            {
                item.LastAction = "UndoSkip";
            }

            _history.RemoveAt(_history.Count - 1);

            if (ProgressValue > 0) ProgressValue--;

            _currentIndex = last.Index;
            _isSorting = true;

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            await _stateService.SaveStateAsync(stateFile, _state!);

            await ShowCurrentFileAsync();
            StatusText = "Status: Undo last action";
        }

        [RelayCommand]
        private async Task SavePlanAsync()
        {
            if (_state == null || _items.Count == 0)
            {
                ShowMessage?.Invoke("Tidak ada state aktif. Mulai sorting terlebih dahulu.");
                return;
            }

            if (string.IsNullOrEmpty(SourceFolder) || !Directory.Exists(SourceFolder))
            {
                ShowMessage?.Invoke("Folder sumber pada state tidak ditemukan.");
                return;
            }

            bool isMove = SelectedMode == "Move";
            _state.Mode = isMove ? "Move" : "Copy";

            int appliedCount = 0;
            foreach (var item in _state.Items)
            {
                if (!item.Sorted || item.Committed) continue;
                if (string.IsNullOrEmpty(item.DestFolderPath)) continue;

                try
                {
                    await _operationService.ProcessFileAsync(item.SourcePath, item.DestFolderPath, isMove);
                    item.Committed = true;
                    appliedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessage?.Invoke($"Gagal memproses file:\n{item.SourcePath}\n\n{ex.Message}");
                }
            }

            string stateFile = Path.Combine(SourceFolder, "sorting_state.json");
            await _stateService.SaveStateAsync(stateFile, _state);

            ShowMessage?.Invoke($"Save selesai. {appliedCount} file diproses ({_state.Mode}).");
        }
    }
}
