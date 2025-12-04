# PicSorter

Aplikasi sederhana untuk menyortir foto dan video dengan cepat menggunakan shortcut keyboard. Dibuat untuk workflow yang cepat dan fokus — tanpa drag-and-drop, tanpa buka folder berulang.

---

## ✨ Fitur Utama

* 🎯 Sortir menggunakan tombol angka `1–0`
* 📁 Banyak folder tujuan
* 🏷️ `Copy` atau `Move`
* ⏸️ **Skip (`S`)** — lewati file
* ↩️ **Undo (`Backspace`)** — mengembalikan aksi terakhir
* 💾 **Deferred Execution** — file baru dipindahkan saat menekan **Apply Changes**
* 🔄 Lanjutkan sesi kapan saja via `sorting_state.json`

---

## 🔧 Cara Menggunakan

1. Pilih **source folder** berisi foto/video
2. Tambahkan **folder tujuan** (1–0 shortcut otomatis)
3. Pilih mode **Copy/Move**
4. Tekan **Start Sorting**
5. Navigasi:

   * `1–0` → sortir ke folder
   * `S` → skip
   * `Backspace` → undo
6. Setelah selesai sorting, tekan **Save** untuk menerapkan perubahan ke file system.

---

## 💻 Platform

* Windows 64-bit
* .NET 8.0 Runtime

---

## 🧩 Status

🏁It works on my PC

Rencana fitur lanjutan: thumbnail video, metadata EXIF, batch tagging, reject category.
