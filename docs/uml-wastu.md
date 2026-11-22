# Diagram UML — Wastu

Sumber diagram (Mermaid) untuk aplikasi **Wastu**: pemilihan bahan bangunan berbasis kesesuaian iklim lokasi dan jejak karbon tersemat. Cakupan versi target (hingga V2): komponen **dinding** dan **atap** (fleksibel: dinding saja, atap saja, atau keduanya).

Diagram ini merupakan spesifikasi UML normatif Wastu yang telah diselaraskan dengan PRD-R01 sampai PRD-R11 (§PRD-R10).

---

## 1. Use Case Diagram

Aktor:
- **Pemilik Rumah** (aktor manusia utama): calon pemilik rumah atau pemilik rumah yang merenovasi.
- **Layanan Cuaca** (aktor sistem eksternal): penyedia API cuaca pihak ketiga (Open-Meteo) yang melayani data historis iklim 10 tahun dan data prakiraan harian 14 hari.

Aplikasi bersifat desktop *single-user*, sehingga tidak memerlukan use case autentikasi/login pengguna.

```mermaid
flowchart LR
    U[Pemilik Rumah]
    W[Layanan Cuaca]

    subgraph WASTU[Aplikasi Wastu]
        direction TB
        UC1([Kelola Profil Proyek & Komponen])
        UC2([Telusuri Katalog Bahan])
        UC3([Bandingkan Bahan])
        UC4([Analisis Iklim Lokasi])
        UC5([Simulasi Emisi Jangka Panjang])
        UC6([Jadwalkan Pekerjaan])
        UC7([Ambil Prakiraan Cuaca])
        UC8([Ekspor Laporan Rekomendasi])
    end

    U --- UC1
    U --- UC2
    U --- UC3
    U --- UC6
    U --- UC8

    UC3 -.->|include| UC4
    UC3 -.->|include| UC2
    UC5 -.->|extend| UC3
    UC6 -.->|include| UC7
    UC8 -.->|extend| UC3

    UC4 --- W
    UC7 --- W
```

---

## 2. Activity Diagram

Alur utama: mulai dari pembuatan profil proyek, analisis iklim, perbandingan berperingkat, kustomisasi bobot (V2), simulasi emisi multi-horizon, penjadwalan cuaca dengan penanganan risiko, hingga ekspor laporan rekomendasi PDF.

Pengubahan kriteria/bobot mengarahkan alur kembali langsung ke penghitungan skor kesesuaian tanpa membuat proyek baru (§PRD-R01, §PRD-R10).

```mermaid
flowchart TD
    A([Mulai]) --> B[Buat / pilih profil proyek:<br/>lokasi + komponen dinding/atap]
    B --> C[/Ambil data iklim historis lokasi Open-Meteo/]
    C --> D[Susun profil iklim:<br/>curah hujan, kelembapan, suhu, korosi]
    D --> E[Ambil kandidat bahan dari katalog]
    E --> F[Hitung skor kesesuaian dan kgCO2e awal]
    F --> G[Tampilkan peringkat + alasan penilaian tiap baris]
    G --> H{Ubah bobot / kriteria?}
    H -->|Ya - Mode V2| F
    H -->|Tidak| I[Pilih bahan dinding / atap]
    I --> J[Simulasi emisi jangka panjang<br/>penggantian dini: 0 < t < H]
    J --> K[/Ambil prakiraan cuaca harian 14 hari/]
    K --> L{Status kelayakan cuaca?}
    L -->|Data tidak tersedia| M1[Tampilkan keterangan data cuaca belum tersedia]
    M1 --> K
    L -->|Tidak layak| M2{Konfirmasi risiko?}
    M2 -->|Batal / Geser| M3[Geser tanggal rencana pekerjaan]
    M3 --> K
    M2 -->|Setujui risiko| N[Kunci jadwal pekerjaan<br/>dengan persetujuan risiko]
    L -->|Layak / Waspada| N
    N --> O[Ekspor laporan rekomendasi PDF]
    O --> P([Selesai])
```

---

## 3. Domain Model

Entitas inti dan relasi domain Wastu. Diagram ini mencerminkan struktur domain logis setelah koreksi PRD-R10:
- `Proyek` memiliki relasi `0..1` ke `ProfilIklim` (profil iklim bernilai null sebelum berhasil diambil dari API atau saat koordinat baru belum selesai dianalisis).
- `Proyek` memiliki minimal satu `Komponen` (`1..*`), mendukung evaluasi dinding saja, atap saja, atau keduanya.
- `Evaluasi` memiliki relasi `0..*` ke `SimulasiEmisi` (evaluasi awal/draft boleh belum memiliki hasil simulasi emisi, dan setelah dieksekusi dapat menyimpan hasil proyeksi untuk berbagai horizon waktu 20, 30, dan 50 tahun secara bersamaan).
- Entitas `Pengguna` tidak lagi memiliki operasi `login()` karena aplikasi beroperasi secara *single-user desktop*.

```mermaid
classDiagram
    class Pengguna
    class Proyek
    class ProfilIklim
    class Komponen
    class Bahan
    class Evaluasi
    class SimulasiEmisi
    class Pekerjaan
    class PrakiraanCuaca
    class Laporan

    Pengguna "1" --> "*" Proyek : memiliki
    Proyek "1" --> "0..1" ProfilIklim : dianalisis
    Proyek "1" --> "1..*" Komponen : terdiri atas
    Proyek "1" --> "*" Pekerjaan : menjadwalkan
    Proyek "1" --> "*" Laporan : menghasilkan
    Komponen "1" --> "*" Evaluasi : dievaluasi
    Evaluasi "*" --> "1" Bahan : menilai
    Evaluasi "1" --> "0..*" SimulasiEmisi : diproyeksikan
    Pekerjaan "1" ..> "*" PrakiraanCuaca : memakai
```

---

## 4. Class Diagram

Class diagram normatif lengkap dengan atribut dan operasi yang telah diselaraskan dengan kontrak C#/.NET 8 dan skema database relasional:

```mermaid
classDiagram
    class Pengguna {
        +Guid id
        +string nama
        +string email
    }
    class Proyek {
        +Guid id
        +string nama
        +string kota
        +double latitude
        +double longitude
        +DateTime dibuatPadaUtc
        +DateTime diperbaruiPadaUtc
        +tambahKomponen(Komponen k) void
        +aturLuasKomponen(JenisKomponen jenis, double luas) void
    }
    class ProfilIklim {
        +double curahHujanTahunan
        +double kelembapanRata
        +double suhuRata
        +double jarakPesisirKm
        +string tingkatKorosi
        +string sumber
        +string provenance
        +DateTime diambilPadaUtc
    }
    class Komponen {
        +Guid id
        +JenisKomponen jenis
        +double luas
        +bandingkanBahan() Evaluasi[]
    }
    class Bahan {
        +Guid id
        +string nama
        +JenisKomponen jenis
        +double konduktivitasTermal
        +int ketahananLembap
        +int ketahananKorosi
        +double usiaNominalTahun
        +double faktorEmisiM2
        +string susunanKetebalan
        +string sumber
        +string provenance
    }
    class Evaluasi {
        +Guid id
        +double skorKesesuaian
        +double emisiAwal
        +string alasan
        +double bobotTermal
        +double bobotLembap
        +double bobotKorosi
        +double bobotUsia
        +hitungSkor(ProfilIklim iklim, KriteriaBobot bobot) double
    }
    class SimulasiEmisi {
        +Guid id
        +int horizonTahun
        +double usiaEfektifTahun
        +int jumlahPenggantian
        +double emisiKumulatif
        +string versiModel
        +proyeksikan(double emisiAwal) double
    }
    class Pekerjaan {
        +Guid id
        +string nama
        +JenisPekerjaan jenis
        +DateTime tanggalMulai
        +DateTime tanggalSelesai
        +DateTime? tanggalDikunci
        +string status
        +bool persetujuanRisiko
        +jadwalkan(PrakiraanCuaca cuaca, bool konfirmasiRisiko) bool
    }
    class PrakiraanCuaca {
        +DateTime tanggal
        +double curahHujan
        +double suhuMin
        +double suhuMaks
        +double kelembapanMaks
        +double probabilitasHujan
        +string zonaWaktu
        +evaluasiKelayakan(JenisPekerjaan jenis, OverrideAmbang? overrideAmbang) StatusKelayakan
    }
    class Laporan {
        +Guid id
        +DateTime tanggalDibuat
        +string format
        +string pathBerkas
        +ekspor() FileInfo
    }

    Pengguna "1" --> "*" Proyek : memiliki
    Proyek "1" --> "0..1" ProfilIklim : dianalisis
    Proyek "1" --> "1..*" Komponen : terdiri atas
    Proyek "1" --> "*" Pekerjaan : menjadwalkan
    Proyek "1" --> "*" Laporan : menghasilkan
    Komponen "1" --> "*" Evaluasi : dievaluasi
    Evaluasi "*" --> "1" Bahan : menilai
    Evaluasi "1" --> "0..*" SimulasiEmisi : diproyeksikan
    Pekerjaan "1" ..> "*" PrakiraanCuaca : memakai
```

---

## 5. Hubungan dengan Diagram Historis

Berkas gambar diagram kelas historis [`docs/class-diagram.png`](class-diagram.png) berasal dari tugas awal Modul 3 dan dipertahankan semata-mata sebagai catatan arsip historis. Diagram normatif yang mengikat implementasi dan pengujian adalah diagram Mermaid di atas dan berkas sumber [`docs/uml-wastu.drawio`](uml-wastu.drawio).
