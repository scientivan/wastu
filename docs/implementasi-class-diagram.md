# Implementasi Class Diagram — WPF

Class diagram tugas Modul 3 diterapkan pada folder `Models/` di root repository (sebelumnya proyek
terpisah `ImplementasiClassDiagram/`). Setiap atribut UML dibuat sebagai instance variable `private` dan
dibungkus oleh property `public`, sesuai contoh `Employee` pada modul. Model ini kini menjadi data domain
aplikasi Wastu: `Bangunan` adalah proyek, `Komponen` menyimpan luas dinding dan atap, `Evaluasi` menghitung
skor kesesuaian, `SimulasiEmisi` memproyeksikan emisi, serta `Pekerjaan` dan `PrakiraanCuaca` dipakai
pada jadwal pekerjaan.

## Pemetaan UML ke C#

- `string`, `int`, dan `double` dipetakan langsung ke tipe C#.
- `date` dipetakan menjadi `DateTime`.
- `Evaluasi[]` dipetakan menjadi array `Evaluasi[]`.
- `File` dipetakan menjadi `System.IO.FileInfo`, sebab `System.IO.File` adalah static class.
- Multiplicity `*` dipetakan ke `List<T>` yang diekspos sebagai `IReadOnlyList<T>`.
- Multiplicity `0..1` pada `SimulasiEmisi` dipetakan menjadi nullable reference `SimulasiEmisi?`.
- `JenisKomponen`, `JenisPekerjaan`, dan `StatusKelayakan` diterapkan sebagai `enum`.

## Penyesuaian untuk aplikasi

Rumus pada model diselaraskan dengan PRD dan prototipe antarmuka HTML Wastu (`app.js`), lalu diverifikasi
menghasilkan angka yang identik:

- `Evaluasi.HitungSkor` memakai rata-rata empat sub-skor (termal, lembap, korosi, usia pakai relatif
  terhadap bahan sejenis) dan menyusun `Alasan` (PRD §11.1). Overload `HitungSkor(ProfilIklim)` tetap ada.
- `Evaluasi.ProyeksikanEmisi` menurunkan usia pakai efektif dengan `0,5 + 0,5 × skor/100` (PRD §11.3).
- `SimulasiEmisi.Proyeksikan(emisiAwal)` menghitung `emisi_awal × (1 + jumlah_penggantian)`;
  `PenggantianHingga(tahun)` dipakai grafik emisi kumulatif.
- `PrakiraanCuaca.EvaluasiKelayakan(JenisPekerjaan)` menerapkan ambang pengecoran dan pengecatan (PRD §12).
- `Pekerjaan.TanggalDikunci` menjadi `DateTime?` (belum dikunci = `null`), dan `Jadwalkan(c, tetapKunci)`
  mengizinkan pengguna mengunci hari tidak layak setelah konfirmasi.
- `ProfilIklim` menambahkan `Sumber` dan `DiperbaruiPada`; `TingkatKorosi` diturunkan dari jarak pesisir.
- `Bangunan` menambahkan `LuasKomponen`, `AturLuasKomponen`, dan `HapusPekerjaan`.

## Build

```bash
dotnet build Wastu.csproj
```

Project menargetkan `net8.0-windows` dan dijalankan pada sistem operasi Windows dengan .NET 8 SDK.
