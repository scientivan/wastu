# Diagram UML — Wastu

Sumber diagram (Mermaid) untuk aplikasi **Wastu**: pemilihan bahan bangunan berbasis
kesesuaian iklim lokasi dan jejak karbon tersemat. Cakupan versi pertama: komponen
**dinding** dan **atap**.

---

## 1. Use Case Diagram

Aktor: pemilik rumah (pengguna utama) dan layanan cuaca pihak ketiga (aktor sistem).

```mermaid
flowchart LR
    U[Pemilik Rumah]
    W[Layanan Cuaca]

    subgraph WASTU[Aplikasi Wastu]
        direction TB
        UC1([Kelola Profil Proyek])
        UC2([Telusuri Katalog Bahan])
        UC3([Bandingkan Bahan])
        UC4([Analisis Iklim Lokasi])
        UC5([Simulasi Emisi Jangka Panjang])
        UC6([Jadwalkan Pekerjaan])
        UC7([Ambil Prakiraan Cuaca])
        UC8([Ekspor Laporan])
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
    UC7 --- W
```

---

## 2. Activity Diagram

Alur utama: dari pembuatan profil proyek sampai laporan rekomendasi siap dibawa ke kontraktor.

```mermaid
flowchart TD
    A([Mulai]) --> B[Buat profil proyek:<br/>lokasi + komponen dinding/atap]
    B --> C[/Ambil data iklim historis lokasi/]
    C --> D[Susun profil iklim:<br/>curah hujan, kelembapan, suhu, korosi]
    D --> E[Ambil kandidat bahan dari katalog]
    E --> F[Hitung skor kesesuaian dan kgCO2e]
    F --> G[Tampilkan peringkat + alasan penilaian]
    G --> H{Ubah kriteria?}
    H -->|Ya| B
    H -->|Tidak| I[Pilih bahan]
    I --> J[Simulasi emisi termasuk<br/>penggantian dini]
    J --> K[/Ambil prakiraan cuaca harian/]
    K --> L{Cuaca layak kerja?}
    L -->|Tidak| M[Geser tanggal pekerjaan]
    M --> K
    L -->|Ya| N[Kunci jadwal pekerjaan]
    N --> O[Ekspor laporan rekomendasi]
    O --> P([Selesai])
```

---

## 3. Domain Model

Entitas dan relasi inti, sebelum dilengkapi attribute dan method (lihat §4 untuk versi
lengkapnya). Dua kategori tipe (jenis komponen, jenis pekerjaan) sengaja tidak dimunculkan
di sini karena itu detail attribute, bukan entitas domain tersendiri.

```mermaid
classDiagram
    class Pengguna
    class Bangunan
    class ProfilIklim
    class Komponen
    class Bahan
    class Evaluasi
    class SimulasiEmisi
    class Pekerjaan
    class PrakiraanCuaca
    class Laporan

    Pengguna "1" --> "*" Bangunan : memiliki
    Bangunan "1" --> "1" ProfilIklim : dianalisis
    Bangunan "1" --> "*" Komponen : terdiri atas
    Bangunan "1" --> "*" Pekerjaan : menjadwalkan
    Bangunan "1" --> "*" Laporan : menghasilkan
    Komponen "1" --> "*" Evaluasi : dievaluasi
    Evaluasi "*" --> "1" Bahan : menilai
    Evaluasi "1" --> "0..1" SimulasiEmisi : diproyeksikan
    Pekerjaan "1" --> "*" PrakiraanCuaca : memakai
```

---

## 4. Class Diagram

```mermaid
classDiagram
    class Pengguna {
        +int id
        +string nama
        +string email
        +login() bool
    }
    class Proyek {
        +int id
        +string nama
        +string kota
        +double latitude
        +double longitude
        +tambahKomponen(Komponen) void
    }
    class ProfilIklim {
        +double curahHujanTahunan
        +double kelembapanRata
        +double suhuRata
        +double jarakPesisirKm
        +string tingkatKorosi
    }
    class Komponen {
        +JenisKomponen jenis
        +double luas
        +bandingkanBahan() Evaluasi[]
    }
    class Bahan {
        +int id
        +string nama
        +double konduktivitasTermal
        +int ketahananLembap
        +int ketahananKorosi
        +int usiaPakaiTahun
        +double faktorEmisi
    }
    class Evaluasi {
        +double skorKesesuaian
        +double emisiAwal
        +string alasan
        +hitungSkor(ProfilIklim) double
    }
    class SimulasiEmisi {
        +int horizonTahun
        +int jumlahPenggantian
        +double emisiKumulatif
        +proyeksikan() double
    }
    class Pekerjaan {
        +string nama
        +date tanggalRencana
        +string status
        +jadwalkan(PrakiraanCuaca) bool
    }
    class PrakiraanCuaca {
        +date tanggal
        +double curahHujan
        +double suhu
        +double kelembapan
        +layakKerja() bool
    }
    class Laporan {
        +date tanggalDibuat
        +string format
        +ekspor() File
    }

    Pengguna "1" --> "*" Proyek : memiliki
    Proyek "1" --> "1" ProfilIklim : dianalisis
    Proyek "1" --> "*" Komponen : terdiri atas
    Proyek "1" --> "*" Pekerjaan : menjadwalkan
    Proyek "1" --> "*" Laporan : menghasilkan
    Komponen "1" --> "*" Evaluasi : dievaluasi
    Evaluasi "*" --> "1" Bahan : menilai
    Evaluasi "1" --> "0..1" SimulasiEmisi : diproyeksikan
    Pekerjaan "1" ..> "*" PrakiraanCuaca : memakai
```
