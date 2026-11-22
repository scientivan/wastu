# Implementasi Class Diagram — WPF

Dokumen ini menjelaskan hubungan antara spesifikasi diagram kelas UML normatif Wastu dan implementasi kode pada folder `Models/` di root repository.

> [!NOTE]
> Berkas gambar [`docs/class-diagram.png`](class-diagram.png) adalah **diagram kelas historis tugas awal Modul 3** dan dipertahankan semata-mata sebagai arsip historis. Spesifikasi diagram normatif sistem didefinisikan secara resmi pada [`docs/uml-wastu.md`](uml-wastu.md) dan sumber visual [`docs/uml-wastu.drawio`](uml-wastu.drawio) (§PRD-R10).

## 1. Kondisi Implementasi Baseline Saat Ini (Folder `Models/`)

Model pada folder `Models/` awalnya diadaptasi dari tugas Modul 3 dan saat ini menjadi representasi data domain aplikasi di antarmuka WPF:

- **Identitas:** Model pada `Models/` (`Bangunan`, `Bahan`, `Pengguna`) masih menggunakan tipe `int id` warisan Modul 3, sedangkan `Models/Pekerjaan.cs` tidak memiliki field ID. Lapisan basis data EF Core (`Wastu.Data`) dan service domain (`Wastu.Domain`) menggunakan `Guid`. Pemetaan antara `int` dan `Guid` saat ini dijembatani pada lapisan adapter penyimpanan.
- **Tipe Primitif:** `string`, `int`, dan `double` dipetakan langsung ke tipe data C#.
- **Tanggal & Waktu:** `date` dipetakan menjadi `DateTime`.
- **Berkas Artefak:** `File` dipetakan menjadi `System.IO.FileInfo` (karena `System.IO.File` adalah static class C#).
- **Relasi & Multiplisitas Baseline:**
  - `Bangunan` ke `Komponen`: `Bangunan` menyimpan luas komponen melalui `LuasKomponen` dan mewajibkan luas dinding serta atap sekaligus. Pada `Wastu.Domain.Komponen`, konstruktor domain sudah memvalidasi penolakan nilai non-finite atau $\le 0$; batasan pada baseline adalah kebijakan antarmuka yang mewajibkan input dua komponen sekaligus dan penggunaan `init`/`with`.
  - `Komponen` ke `Evaluasi`: `Evaluasi[]` dipetakan menjadi array atau `IReadOnlyList<Evaluasi>`.
  - `Evaluasi` ke `SimulasiEmisi`: Multiplicity pada model baseline adalah `0..1` (`SimulasiEmisi?` tunggal pada `Models/Evaluasi.cs`).
  - `Pekerjaan` ke `PrakiraanCuaca`: Menggunakan dependensi use untuk mengevaluasi kelayakan kerja.
- **Tipe Enumerasi Baseline:** `JenisKomponen`, `JenisPekerjaan`, dan `StatusKelayakan` diterapkan sebagai C# `enum`. Pada `Models/Enums.cs`, `StatusKelayakan` sudah merupakan enum 3 nilai (`Layak`, `Waspada`, `TidakLayak`). Pada `Models/PrakiraanCuaca.cs`, evaluasi mengembalikan kelayakan dasar (`bool`) beserta alasan teknisnya.
- **Rumus & Ambang Baseline:**
  - `Evaluasi.HitungSkor` diselaraskan dengan prototipe awal HTML (`app.js`).
  - `SimulasiEmisi.Proyeksikan` di domain telah mengadopsi $\lceil H/L \rceil - 1$, namun UI memiliki duplikasi perhitungan dengan penanganan epsilon terpisah.
  - Ambang penjadwalan cuaca di UI masih menggunakan konstanta generik per jenis pekerjaan.
  - `Pekerjaan.TanggalDikunci` bertipe `DateTime?`, dengan dukungan penguncian hari tidak layak setelah konfirmasi pengguna.
- **Lokasi Layanan Domain Aktual:**
  - `ProjectService`: `src/Wastu.Domain/Projects/ProjectService.cs`
  - `MaterialService`: `src/Wastu.Domain/Catalog/MaterialService.cs`
  - `SuitabilityScoringService`: `src/Wastu.Domain/Scoring/SuitabilityScoringService.cs`
  - `EmissionSimulationService`: `src/Wastu.Domain/Emissions/EmissionSimulationService.cs`

## 2. Target Revisi Spesifikasi Normatif (WP01 — WP08)

Paket implementasi WP01 sampai WP08 akan menyelaraskan model baseline di atas dengan spesifikasi normatif PRD v2.0:

1. **Standardisasi Identitas:** Menyeragamkan seluruh ID entitas domain, model antarmuka, dan persistensi database menggunakan tipe `Guid` yang konsisten (§PRD-R10).
2. **Fleksibilitas Komponen Proyek:** Mengubah relasi `Proyek` ke `Komponen` menjadi `1..*` dinamis (mendukung proyek dinding saja, atap saja, atau keduanya) dengan validasi luas positif $> 0\text{ m}^2$ (§PRD-R10).
3. **Multiplisitas Profil Iklim:** Memperjelas relasi `Proyek` ke `ProfilIklim` menjadi `0..1` (profil iklim bernilai null sebelum berhasil diambil dari API atau sebelum koordinat baru selesai dianalisis) (§PRD-R10).
4. **Riwayat Simulasi Multi-Horizon:** Mengubah relasi `Evaluasi` ke `SimulasiEmisi` menjadi `0..*` (evaluasi dapat belum disimulasikan `0`, atau menyimpan banyak hasil simulasi horizon 20, 30, 50 tahun `*` secara simultan tanpa saling menimpa) (§PRD-R10).
5. **Status Kelayakan Cuaca Multi-Nilai:** Memperluas `StatusKelayakan` menjadi empat status: `Layak`, `Waspada` (suhu pengecoran > 35°C), `TidakLayak` (pelanggaran ambang keras), dan `DataTidakTersedia` (hari tanpa prakiraan atau data null yang hanya dapat disimpan sebagai draft) (§PRD-R05, §PRD-R08).
6. **Hierarki Ambang Penjadwalan:** Mendukung override spesifikasi produk material di atas ambang asumsi generik bawaan (§PRD-R05).
7. **Penyelarasan Rumus dan Ambang Label:**
   - Rumus sub-skor lembap dengan $\alpha = 0{,}5$ dan acuan hujan 3000 mm/tahun (§PRD-R04).
   - Ambang label rekomendasi terpusat: `< 55` *Tidak disarankan*, `55 s/d 74,999` *Perlu pertimbangan*, dan `≥ 75` *Cocok* (§PRD-R10).
   - Rumus penggantian strictly $0 < t < H$: $n = \max(0, \lceil H/L \rceil - 1)$ dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$ (§PRD-R02).

## Build dan Target Framework

```bash
dotnet build Wastu.csproj
```

Proyek menargetkan `net8.0-windows` dan dijalankan pada sistem operasi Windows dengan .NET 8 SDK.
