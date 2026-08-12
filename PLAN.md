# PicSorter — Rencana Upgrade

Repo: `github.com/yeftakun/PicSorting`
Status kode saat ini: WinForms (.NET 8), single-file `Form1.cs` (699 baris), semua logic (model, scan, save/load JSON, copy/move) menyatu dengan UI.

---

## 1. Fondasi Arsitektur — ✅ Arah sudah diputuskan

**Keputusan:** Migrasi ke **WPF**, dengan struktur yang mempersiapkan migrasi ke Avalonia di masa depan tanpa mengorbankan kecepatan pengembangan WPF sekarang.

### Struktur Solution
```
PicSorting.sln
├── PicSorter.Core/          (Class Library, .NET 8 — TANPA <UseWPF>true</UseWPF>)
│   ├── Models/               (SortState, SortItemState, DestinationFolderInfo, dll)
│   ├── Services/              (FileScanService, SortStateService, FileOperationService, ExifService, VideoThumbnailService)
│   └── ViewModels/            (pakai CommunityToolkit.Mvvm)
├── PicSorter.Wpf/            (WPF App project, reference ke Core)
│   ├── Views/ (*.xaml)
│   └── App.xaml.cs
└── PicSorter.Core.Tests/      (xUnit/NUnit, testing Services murni tanpa UI)
```

### Prinsip Kerja
- **Pemisahan fisik, bukan cuma disiplin coding** — `PicSorter.Core` tanpa `UseWPF` di csproj, jadi compiler yang menegakkan batas, bukan niat baik saat coding.
- **Titik konversi tipe UI di satu tempat kecil** — Core mengembalikan `byte[]`/`Stream` untuk data gambar (preview, thumbnail), bukan `BitmapImage`. WPF layer (converter/code-behind View) yang convert ke `BitmapImage` dengan `CacheOption.OnLoad` + `Freeze()` (mencegah file locking & cross-thread crash — bukan karena taruh `BitmapImage` di ViewModel itu sendiri yang jadi masalah, tapi cara load-nya).
- **Keyboard & visual routing tetap di code-behind View** — bukan dipaksa masuk ViewModel. Contoh: `KeyDown` handler di code-behind memanggil `ViewModel.TryGetCommandForKey()`, logic pemilihan command ada di ViewModel/Core (testable), tapi listening event tetap View punya kerjaan.
- **Pattern async cross-platform** untuk operasi berat (scan folder, dll): `IAsyncEnumerable<T>` + `IProgress<T>`, bukan `BackgroundWorker` (WPF-era pattern yang tidak portable).
- **CommunityToolkit.Mvvm** dipakai dari awal untuk `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` — package ini UI-framework-agnostic.
- **Jangan over-abstract di depan** — interface/abstraction (misal `IDialogService`) dibuat saat benar-benar butuh testability-nya (saat menulis unit test yang butuh mock), bukan spekulatif sejak awal.

---

## 2. Fitur Inti — Urutan Eksekusi Disepakati

Urutan: **Undo history penuh → EXIF metadata → Scan rekursif → Preview Zoom/Pan**
**Video thumbnail: DITUNDA / disabled dulu** (dependency FFmpeg paling ribet disetup, dikerjakan belakangan).

### 2.1 Undo History Penuh
- Ganti `_history` (sekarang efektif cuma 1 langkah) jadi `Stack<SortActionRecord>` tanpa batas, atau Command Pattern penuh kalau sekalian mau tambah Redo.
- 100% pure C#, tidak ada concern WPF/Avalonia — aman penuh di `PicSorter.Core`.
- Effort: kecil.

### 2.2 EXIF Metadata
- Library: `MetadataExtractor` (pure managed, ringan, tidak butuh binary eksternal).
- `PicSorter.Core/Services/ExifService.cs` → `ReadExifInfo(string imagePath)` return tanggal ambil, kamera, resolusi (tipe primitif/string saja).
- Tampilkan di panel info kecil di samping preview (detail layout masuk pembahasan poin 4 UI/UX).
- Effort: kecil-sedang. Reusable 100% ke Avalonia.

### 2.3 Scan Rekursif
- `PicSorter.Core/Services/FileScanService.cs` → `ScanFolderAsync(string root, bool recursive, CancellationToken ct)` sebagai `IAsyncEnumerable<ScannedFile>`.
- **Keputusan UX yang masih perlu diambil** (di-defer sampai dicoba):
  - Apakah asal subfolder file perlu ditampilkan/dipertahankan sebagai info (misal `RelativeSourcePath` di `SortItemState`)?
  - Apakah struktur subfolder tujuan ikut di-mirror, atau semua file tetap flat ke satu folder tujuan pilihan (default: flat)?
- Saran: tambahkan kolom `RelativeSourcePath` di model dari awal (murah), biar gampang dipakai nanti tanpa migrasi data lama, meski keputusan UX final di-defer.
- Effort: sedang.

### 2.4 Preview Zoom/Pan
- Satu-satunya fitur di poin 2 yang **murni View-layer** — akan perlu ditulis ulang total saat migrasi ke Avalonia nanti (0% reusable), karena sifatnya visual behavior WPF-specific.
- Implementasi: `Image` control dalam `ScrollViewer` + handle `MouseWheel` untuk `ScaleTransform`, atau custom `Behavior` kecil.
- Effort: kecil-sedang.

### 2.5 Video Thumbnail — DITUNDA
- Library: `FFMpegCore` (wrapper FFmpeg, butuh binary eksternal — bundle vs assume-installed masih perlu diputuskan; rekomendasi awal: bundle binary untuk zero-friction personal use, ~80-100MB tambahan installer).
- `PicSorter.Core/Services/VideoThumbnailService.cs` → `ExtractFirstFrameAsync(string videoPath, CancellationToken ct)` return `byte[]`.
- Perlu caching hasil ekstraksi (`%LocalAppData%/PicSorter/thumbcache/`, key = hash path+lastmodified) karena proses FFmpeg per file cukup berat.
- Bisa sekalian pasang `FFProbe` untuk metadata video (durasi, resolusi) saat fitur ini dikerjakan.
- Reusable ~90% ke Avalonia (logic Core reusable, binary bundling perlu disetup ulang per platform).
- **Status: belum dikerjakan, dibahas lagi belakangan.**

---

## 3. Alur Kerja & Produktivitas — Urutan Eksekusi Disepakati

Urutan: **Statistik sesi → Shortcut navigasi → Filter/Urutkan → Deteksi duplikat**
**Batch/Grid preview: DITUNDA** (direncanakan untuk versi mendatang, digarap bareng poin 4 UI/UX karena perubahan layoutnya signifikan).

### 3.1 Statistik Sesi
- Data sudah tersedia semua di `_items`/`SortState` — tinggal `ObservableProperty` teragregasi di ViewModel yang re-compute tiap kali ada Assign/Undo (`GroupBy(DestFolderPath).Count()`).
- Tidak ada ketergantungan fitur lain, bisa dikerjakan kapan saja.
- Effort: kecil.

### 3.2 Shortcut Navigasi Manual
- Panah kiri/kanan untuk browsing tanpa assign — butuh index terpisah dari `_currentIndex` sorting (`_browseIndex`), supaya browsing tidak mengganggu state sorting utama.
- Command logic (`NavigatePreviousCommand`/`NavigateNextCommand`) di ViewModel; listening `KeyDown` tetap di code-behind View (sama pola dengan keyboard routing di poin 1).
- Effort: kecil.

### 3.3 Filter/Urutkan File Sebelum Sorting
```csharp
// PicSorter.Core/Services/FileScanService.cs
public enum SortCriteria { Name, DateModified, DateCreated, FileSize }
```
- Sort logic 100% di Core (pure LINQ `OrderBy`).
- UI: dropdown/combobox di panel setup sebelum "Start Sorting".
- Bisa digabung sekalian saat mengerjakan Scan Rekursif (poin 2.3) karena sama-sama menyentuh `FileScanService`.
- Effort: kecil.

### 3.4 Deteksi Duplikat
- **Hash:** `XxHash64` dari `System.IO.Hashing` (bukan MD5/SHA1) — non-cryptographic, cukup untuk "sama/beda", jauh lebih cepat untuk file besar.
- **Strategi performa:** filter dulu by ukuran file (ukuran beda = pasti bukan duplikat, skip hash), baru hash file dengan ukuran sama persis. Progress reporting (`IProgress<T>`) wajib karena hashing folder besar bisa lama.
```csharp
// PicSorter.Core/Services/DuplicateDetectionService.cs
public async IAsyncEnumerable<DuplicateGroup> FindDuplicatesAsync(
    IReadOnlyList<ScannedFile> files, IProgress<int> progress, CancellationToken ct)
```
- **UX:** tandai visual (badge/warna) di daftar file, bukan auto-skip — menghindari risiko false-positive atau kasus user memang mau assign kedua duplikat ke folder berbeda.
- **Scope:** duplikat dibandingkan dalam 1 sesi sorting saja dulu (bukan termasuk file yang sudah ada di folder tujuan — itu lebih berguna tapi lebih lambat, bisa dipertimbangkan di iterasi berikutnya).
- Effort: sedang-besar (bagian paling kompleks di poin 3).

### 3.5 Batch/Grid Preview — DITUNDA
- Thumbnail untuk banyak file sekaligus perlu digenerate (beda dengan sekarang yang cuma load 1 gambar aktif) — berkaitan langsung dengan poin 5 (Ketahanan & Skala).
- Wajib virtualization: WPF `ListView`/`ItemsControl` + `VirtualizingStackPanel` (built-in).
- Thumbnail resize on-the-fly pakai `DecodePixelWidth` di `BitmapImage` (decode langsung ke resolusi kecil, hemat memori vs decode full-res lalu resize).
- Effort besar, perubahan layout signifikan — digarap bareng redesign UI (poin 4).
- **Status: belum dikerjakan, direncanakan untuk versi mendatang.**

## 4. Redesign UI/UX — Keputusan Disepakati

**Styling/theme:** `WPF-UI` (Fluent Design) — dependency ringan, hasil visual modern (Windows 11 native look), built-in dark mode, dan konsepnya punya counterpart langsung di Avalonia (`FluentAvalonia`) untuk mempermudah migrasi styling nanti.
**Drag-and-drop reorder folder tujuan:** DITUNDA ke versi mendatang.

### 4.1 Struktur Layout Utama
```
┌─────────────────────────────────────────┐
│  Toolbar: Source/Dest setup, Mode, Start │
├──────────────┬──────────────────────────┤
│              │                          │
│   Preview    │   Panel Info (kanan)     │
│   (besar,    │   - EXIF metadata        │
│   fokus      │   - Statistik sesi       │
│   utama)     │   - Daftar folder tujuan │
│              │     (shortcut 1-0)       │
├──────────────┴──────────────────────────┤
│  Status bar: progress + nama file +      │
│  shortcut aktif                          │
└─────────────────────────────────────────┘
```
Panel kanan menampung EXIF metadata (poin 2.2) dan statistik sesi (poin 3.1) — dirancang punya tempat alami dari awal.

### 4.2 Styling & Theme (WPF-UI)
- Dark/Light/Auto theme built-in, tidak perlu implementasi manual.
- `InfoBadge` control bawaan dipakai untuk badge visual duplikat (poin 3.4) — dot/angka kecil di thumbnail atau baris list file, warna kuning/oranye.

### 4.3 Indikator Mode Copy/Move
- Warna/badge berbeda untuk mode **Copy** (biru/netral) vs **Move** (merah/oranye) — safety UX karena Move bersifat destruktif (file hilang dari folder asal).
- Progress bar + teks status + shortcut aktif digabung jadi satu baris ringkas di status bar.

### 4.4 Antisipasi Struktur untuk Batch/Grid Preview (Masih Ditunda)
- Preview area dan panel kanan dipisah jadi `UserControl` terpisah dari awal (`SinglePreviewView`, nanti `GridPreviewView`) — supaya saat batch preview (poin 3.5) akhirnya digarap, preview area bisa di-swap tanpa merombak total layout. Investasi kecil sekarang, hemat besar nanti.

### 4.5 Drag-and-Drop Reorder Folder Tujuan — DITUNDA
- WPF: `ListView`/`ListBox` + `PreviewMouseMove` + `DragDrop.DoDragDrop` (pattern standar, tanpa library tambahan) — untuk direalisasikan nanti.
- Murni View-layer behavior, tidak reusable ke Avalonia (akan ditulis ulang dengan API drag-drop Avalonia saat migrasi).
- **Status: belum dikerjakan, direncanakan untuk versi mendatang.**

## 5. Ketahanan & Skala — Detail

### 5.1 Lazy Loading / Virtualization untuk Folder Besar
- Mode single-preview yang dipakai sekarang **sudah relatif aman** — `ShowCurrentFile()` cuma load 1 gambar aktif ke memori.
- Metadata list (`_items`/`SortState.Items`) untuk data ringan (path, flag video, status) masih oke di-load penuh sampai skala cukup besar — tidak perlu virtualization di level ini.
- Virtualization sungguhan baru relevan kalau Batch/Grid Preview (poin 3.5, masih ditunda) digarap — sudah diantisipasi strukturnya di poin 4.4 (`GridPreviewView` terpisah). **Tidak ada kerjaan tambahan di iterasi sekarang**, cukup pastikan pola ini tidak berubah saat fitur poin 2 & 3 diimplementasi.
- **Wajib untuk poin 3.4 (Deteksi Duplikat):** hashing pakai `Stream`-based read (chunk demi chunk), bukan `File.ReadAllBytes()`, khususnya untuk file video besar.

### 5.2 Error Handling Lebih Baik
- **Logging:** `Microsoft.Extensions.Logging` + provider file sederhana, log ke `%LocalAppData%/PicSorter/logs/`. Pure C#, aman 100% di `PicSorter.Core`.
- **Pesan error actionable**, bukan `ex.Message` mentah:
  - File terkunci aplikasi lain (`IOException` saat Move) → pesan jelas: tutup aplikasi lain dulu.
  - Permission denied → arahkan cek folder permission.
  - Disk penuh saat Copy → tampilkan sisa space kalau memungkinkan.
- Pola: `PicSorter.Core` throw exception custom terkategori (`FileLockedException`, `InsufficientSpaceException`, dll), `PicSorter.Wpf` terjemahkan ke pesan UI via `ContentDialog` (WPF-UI, lebih modern dari `MessageBox`).

### 5.3 Config Lintas Sesi
```csharp
// PicSorter.Core/Services/AppSettingsService.cs
// Simpan ke %AppData%/PicSorter/settings.json — TERPISAH dari sorting_state.json
```
- Folder tujuan favorit (histori, quick-pick).
- Mode terakhir dipakai (Copy/Move) sebagai default.
- Ukuran & posisi window terakhir.
- Preferensi tema (Light/Dark/Auto dari WPF-UI).
- Scope terpisah dari `sorting_state.json` (settings.json = preferensi user, sorting_state.json = state sesi sorting per-folder) — tidak bentrok.

## 6. Distribusi & Kualitas — Detail (Mengikuti Pola hall-config)

**Distribusi:** Self-contained, konsisten dengan keputusan bundle FFmpeg (poin 2.5) — prioritas zero-friction dibanding ukuran installer.
**Pola release:** Publish manual lokal (bukan GitHub Actions), mengikuti pola `hall-config` yang sudah terbukti jalan.

### 6.1 Publish & Installer (Pola dari hall-config)

**`PicSorter.Wpf.csproj`** — tambahkan properti publish langsung di project file:
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

**`publish.ps1`** — script PowerShell yang: clean output dir → `dotnet publish` single-file self-contained → copy native libraries (untuk PicSorter: binary FFmpeg jika Video Thumbnail sudah aktif) & assets (icon) ke publish folder → auto-compile Inno Setup installer kalau `ISCC.exe` terdeteksi di mesin.

**`installer.iss`** — Inno Setup script: app metadata, desktop icon opsional (`Tasks`), output ke `dist/PicSorter_Setup_v{version}.exe`, uninstaller otomatis. (PicSorter kemungkinan tidak butuh opsi "run on startup" seperti hall-config, karena sifat aplikasinya dipakai sesi per-sesi, bukan background service.)

### 6.2 Unit Testing

`PicSorter.Core.Tests` sudah masuk struktur solution sejak poin 1 (konsisten dengan `HallConfig.Core.Tests`), tinggal diisi begitu tiap service selesai.

**Prioritas test berdasarkan risiko:**
| Service | Prioritas | Alasan |
|---|---|---|
| `FileOperationService` (Copy/Move + rename konflik) | **Tinggi** | Operasi destruktif (Move), bug = data hilang |
| `DuplicateDetectionService` | Tinggi | Logic hashing + size-filter rawan edge case (file 0 byte, dll) |
| `SortStateService` (load/save JSON, undo stack) | Sedang | State corruption bisa merusak sesi sorting |
| `FileScanService` (scan + sort + filter) | Sedang | Edge case: folder kosong, ekstensi campur |
| `ExifService` | Rendah | Read-only, fallback ke "no metadata" cukup aman |

**Framework:** `xUnit`.

### 6.3 Dokumentasi
- **README update**: refresh bagian "Rencana fitur" (thumbnail video, EXIF) begitu fitur selesai per-tahap — pindah dari rencana ke daftar fitur aktif.
- **`change_log.md`**: mengikuti pola hall-config, dicatat bertahap seiring tiap poin selesai.

---

---

## Referensi Silang Antar Poin

- Struktur solution (`*.Core` tanpa UseWPF + `*.App`/`*.Wpf` + `*.Core.Tests`) di poin 1 tervalidasi oleh pola yang sudah dipakai di `hall-config`.
- Panel EXIF (2.2) & Statistik Sesi (3.1) sudah punya tempat di layout poin 4.1 sejak didesain.
- Badge duplikat (3.4) memakai `InfoBadge` dari WPF-UI (4.2).
- Struktur `SinglePreviewView`/`GridPreviewView` (4.4) mengantisipasi Batch/Grid Preview (3.5) yang ditunda, sekaligus jadi titik virtualization (5.1) saat digarap nanti.
- Distribusi self-contained (6) konsisten dengan keputusan bundle FFmpeg untuk Video Thumbnail (2.5) — sama-sama prioritas zero-friction.

## Fitur yang Ditunda (Backlog untuk Versi Mendatang)

- Video Thumbnail (2.5) — perlu setup FFmpeg bundling
- Batch/Grid Preview (3.5) — perlu virtualization, digarap bareng redesign lanjutan
- Drag-and-drop reorder folder tujuan (4.5)

---

*Dokumen ini adalah draft rencana lengkap poin 1–6. Siap dipakai sebagai acuan mulai eksekusi.*