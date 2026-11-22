# Implementasi Class Diagram — WPF

Dokumen ini menjelaskan pemetaan antara spesifikasi diagram kelas UML normatif Wastu dan implementasi kode pada folder `Models/` di root repository.

> [!NOTE]
> Berkas gambar [`docs/class-diagram.png`](class-diagram.png) adalah **diagram historis tugas awal Modul 3** dan dipertahankan semata-mata sebagai arsip historis. Diagram normatif yang mengikat implementasi dan pengujian sistem saat ini didefinisikan secara resmi pada [`docs/uml-wastu.md`](uml-wastu.md) dan sumber visual [`docs/uml-wastu.drawio`](uml-wastu.drawio) (§PRD-R10).

Setiap atribut UML dibuat sebagai instance variable `private` dan dibungkus oleh property `public`. Model domain Wastu menggunakan istilah entitas fisik Indonesia yang selaras dengan skema basis data PostgreSQL: `Bangunan` (mewakili proyek bangunan rumah tinggal), `Komponen` (menyimpan luas bidang dinding atau atap), `Bahan` (katalog material), `Evaluasi` (skor kesesuaian komposit dan emisi awal), `SimulasiEmisi` (proyeksi emisi multi-horizon), `Pekerjaan` (jadwal konstruksi), dan `PrakiraanCuaca` (evaluasi kelayakan cuaca harian).

## Pemetaan UML ke C# (.NET 8)

- **Identitas:** Seluruh ID entitas domain dan database menggunakan tipe `Guid` yang konsisten (§PRD-R10).
- **Tipe Primitif:** `string`, `int`, dan `double` dipetakan langsung ke tipe C#.
- **Tanggal & Waktu:** `date` / `DateTime` dipetakan ke `DateTime` (dengan konvensi UTC pada penyimpanan dan zona waktu lokasi proyek pada tampilan).
- **Relasi & Multiplisitas:**
  - `Bangunan "1" --> "0..1" ProfilIklim`: Profil iklim dipetakan sebagai nullable reference property `ProfilIklim?` (bernilai null sebelum data iklim berhasil diambil dari API atau sebelum proyek selesai dianalisis).
  - `Bangunan "1" --> "1..*" Komponen`: Komponen dikelola sebagai koleksi minimal satu komponen aktif (dinding saja, atap saja, atau keduanya) dengan luas valid $> 0\text{ m}^2$.
  - `Komponen "1" --> "*" Evaluasi`: Dipetakan ke koleksi `IReadOnlyList<Evaluasi>`.
  - `Evaluasi "1" --> "*" SimulasiEmisi`: Dipetakan ke koleksi `IReadOnlyList<SimulasiEmisi>` untuk mendukung penyimpanan hasil simulasi simultan pada berbagai horizon waktu (20, 30, 50 tahun) dan versi model.
  - Multiplicity `*` lainnya dipetakan ke `List<T>` internal yang diekspos sebagai `IReadOnlyList<T>`.
- **Berkas Artefak:** `File` dipetakan menjadi `System.IO.FileInfo` (karena `System.IO.File` adalah static class).
- **Tipe Enumerasi:** `JenisKomponen` (`dinding`, `atap`), `JenisPekerjaan` (`pengecoran`, `pengecatan`), dan `StatusKelayakan` (`Layak`, `Waspada`, `TidakLayak`, `DataTidakTersedia`) diterapkan sebagai C# `enum`.
- **Autentikasi:** Entitas `Pengguna` tidak memiliki method `login()` karena aplikasi beroperasi secara *single-user desktop* tanpa kewajiban autentikasi.

## Penyesuaian Rumus dan Aturan Domain

Rumus pada model diselaraskan penuh dengan PRD v2.0 (PRD-R01 sampai PRD-R11):

- **Perhitungan Skor Kesesuaian:**
  - Menggunakan `SuitabilityScoringService` dengan bobot default MVP bernilai tepat 0,25 per kriteria (termal, lembap, korosi, usia).
  - Sub-skor lembap menggunakan model terkoreksi dengan $\alpha = 0{,}5$ dan acuan curah hujan $3000\text{ mm/tahun}$ (§PRD-R04, PRD §11.1).
  - Ambang batas rekomendasi terpusat pada skor 55 dan 75: `< 55` *Tidak disarankan*, `55 s/d 74,999` *Perlu pertimbangan*, dan `≥ 75` *Cocok* (§PRD-R10).
- **Emisi Karbon Awal:**
  - `emisi_awal = faktor_emisi_m2 × luas_komponen` dengan faktor emisi yang wajib dinormalisasi ke $\text{kgCO}_2\text{e/m}^2$ (§PRD-R03).
- **Penurunan Usia Pakai Efektif:**
  - `Evaluasi.ProyeksikanEmisi` menghitung usia efektif $L = \text{usia\_nominal} \times (a + (1 - a) \times \text{skor}/100)$ dengan parameter dasar penuaan $a = 0{,}5$ (§PRD-R02, §PRD-R11).
- **Simulasi Emisi Jangka Panjang:**
  - `SimulasiEmisi.Proyeksikan` menghitung jumlah penggantian strictly pada interval kejadian $0 < t < H$:
    $$n = \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right)$$
    dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$ (§PRD-R02).
  - Kasus acuan: nominal 15 thn, skor 20, horizon 30 thn $\rightarrow L = 9$ thn, $n = 3$ penggantian (thn 9, 18, 27), total emisi $\times 4$.
- **Penjadwalan Cuaca & Persetujuan Risiko:**
  - `PrakiraanCuaca.EvaluasiKelayakan(JenisPekerjaan, OverrideAmbang?)` mengevaluasi hari kerja dengan dukungan status multi-nilai (`Layak`, `Waspada`, `TidakLayak`, `DataTidakTersedia`) menggunakan kelembapan maksimum harian untuk pengecatan (§PRD-R05, §PRD-R08).
  - `Pekerjaan.TanggalDikunci` bertipe `DateTime?`, dan method `Jadwalkan(cuaca, persetujuanRisiko)` mengizinkan penguncian tanggal pada hari berstatus `TidakLayak` setelah pengguna menyetujui konfirmasi risiko secara eksplisit (§PRD-R10).

## Build dan Target Framework

```bash
dotnet build Wastu.csproj
```

Proyek menargetkan `net8.0-windows` dan dijalankan pada sistem operasi Windows dengan .NET 8 SDK.
