# Wastu — Product Requirements Document

**Pembanding Bahan Bangunan Berbasis Iklim dan Jejak Karbon untuk Pemilik Rumah**

| | |
|---|---|
| **Kelompok** | Manut |
| **Mata Kuliah** | [Nama Mata Kuliah — Junior Project] |
| **Kategori** | Sustainable Construction / Climate Action |
| **Tipe Aplikasi** | Desktop, Windows Presentation Foundation (.NET, C#), basis data PostgreSQL, integrasi layanan cuaca pihak ketiga |
| **Versi Dokumen** | 1.0 |
| **Tanggal** | 4 September 2026 |
| **Status** | Draf untuk direview tim |

---

## 1. Ringkasan Eksekutif

Wastu adalah aplikasi desktop yang membantu calon pemilik rumah memilih bahan dinding dan atap berdasarkan **kesesuaian dengan iklim lokasi** dan **jejak karbon tersemat**, lalu membantu menjadwalkan pekerjaan konstruksi mengikuti prakiraan cuaca.

Perbedaannya dengan kebiasaan yang berlaku hari ini terletak pada satu hal: pemilihan bahan bangunan rumah tinggal di Indonesia hampir selalu ditentukan oleh harga dan kebiasaan tukang, bukan oleh kecocokan bahan dengan curah hujan, kelembapan, suhu, dan risiko korosi di lokasi bangunan berdiri. Akibatnya bahan cepat rusak, penggantian dini terjadi lebih sering dari yang seharusnya, dan setiap penggantian memicu emisi karbon berulang yang tidak pernah dihitung siapa pun.

**Segmen MVP:** calon pemilik rumah tinggal tapak di Indonesia yang sedang merencanakan atau merenovasi **dinding dan atap** — versi pertama sengaja dibatasi pada dua komponen ini.

**Tiga keputusan yang membedakan Wastu dari sekadar "kalkulator RAB" atau "kalkulator karbon":**

1. **Iklim dan karbon dinilai berdampingan, bukan terpisah.** Skor kesesuaian iklim dan kgCO2e ditampilkan pada satu peringkat yang sama, disertai alasan tekstual, sehingga pengguna awam tidak perlu menafsirkan dua angka dari dua alat berbeda.
2. **Emisi dihitung sebagai konsekuensi jangka panjang, bukan angka sekali hitung.** Bahan yang tidak cocok dengan iklim diberi usia pakai efektif lebih pendek, sehingga simulasi emisi menangkap **penggantian dini** sebagai pengganda emisi — inilah argumen inti yang membedakan Wastu dari kalkulator jejak karbon material generik (§11).
3. **Penjadwalan kerja terintegrasi dengan rekomendasi bahan, bukan fitur terpisah.** Ambang cuaca layak-kerja untuk pengecoran dan pengecatan diturunkan dari sifat bahan yang sama yang dipakai untuk skor kesesuaian, sehingga satu basis data melayani dua kebutuhan (§12).

**Batasan yang dinyatakan terbuka:** basis data faktor emisi dan properti bahan pada MVP bersumber dari basis data publik internasional (ICE Database, EPD generik) yang didominasi konteks Eropa/Amerika Utara — persis celah yang ingin diisi Wastu untuk pasar lokal (§13). Kurasi nilai spesifik pasar Indonesia adalah pekerjaan berkelanjutan, bukan sesuatu yang selesai di MVP, dan dinyatakan sebagai batasan di antarmuka (§11.4).

---

## 2. Latar Belakang dan Masalah

### 2.1 Bagaimana bahan bangunan dipilih hari ini

Pada konstruksi rumah tinggal non-korporat di Indonesia, keputusan bahan dinding dan atap umumnya diambil berdasarkan harga per satuan dan kebiasaan tukang atau kontraktor kecil, bukan berdasarkan kecocokan bahan dengan kondisi iklim setempat. Curah hujan tinggi, kelembapan tinggi, kedekatan dengan pesisir (risiko korosi garam), dan fluktuasi suhu harian adalah faktor yang secara fisik menentukan seberapa cepat bahan terdegradasi, tetapi faktor-faktor ini jarang masuk ke pertimbangan pembelian.

### 2.2 Konsekuensi berganda dari pemilihan yang salah

Kesalahan pemilihan bahan menimbulkan tiga konsekuensi yang saling memperkuat:

1. **Kerusakan dini.** Bahan yang tidak tahan lembap atau korosi pada iklim tropis lembap atau lokasi pesisir mengalami degradasi jauh sebelum usia pakai nominalnya tercapai.
2. **Emisi berulang.** Setiap penggantian dini mengulang emisi produksi bahan dari awal. Emisi tersemat (*embodied carbon*) bukan biaya satu kali seperti yang diasumsikan kebanyakan kalkulator karbon material — ia berulang setiap kali penggantian terjadi.
3. **Pengerjaan ulang akibat cuaca.** Pengecoran atau pengecatan yang dilakukan pada kondisi cuaca yang salah (hujan saat curing beton, kelembapan tinggi saat pengecatan) berujung pada pengerjaan ulang, yang menambah biaya sekaligus emisi dari bahan yang terbuang.

### 2.3 Celah yang belum terisi

Calon pemilik rumah tidak memiliki alat sederhana untuk membandingkan jejak karbon antar bahan yang fungsinya setara, apalagi yang mengaitkannya dengan iklim lokasi. Alat yang tersedia hari ini terbelah menjadi dua kutub yang sama-sama tidak menjawab kebutuhan ini (rincian di §13):

- Alat analisis siklus hidup bangunan yang mapan (One Click LCA, Embodied Carbon in Construction Calculator) ditujukan bagi praktisi proyek besar, berbasis web, dan basis materialnya didominasi produk pasar Eropa/Amerika Utara.
- Alat pemodelan termal mendalam (EnergyPlus) menuntut keahlian tinggi dan tidak memberi rekomendasi bahan.
- Aplikasi lokal yang beredar berfokus pada rencana anggaran biaya dan volume material, tanpa aspek iklim maupun karbon sama sekali.

### 2.4 Rumusan masalah

1. Bagaimana merancang alat yang membandingkan bahan dinding dan atap berdasarkan kesesuaian iklim lokasi **dan** jejak karbon tersemat secara berdampingan, sehingga pengguna non-profesional bisa membuat keputusan berbasis kedua pertimbangan sekaligus?
2. Bagaimana merepresentasikan konsekuensi jangka panjang dari pemilihan yang salah — penggantian dini dan emisi berulang — dengan cara yang dapat dipahami tanpa latar belakang teknik?
3. Bagaimana menghubungkan rekomendasi bahan dengan pelaksanaan di lapangan, sehingga jadwal pekerjaan yang sensitif cuaca (pengecoran, pengecatan) ikut terbantu, bukan menjadi keputusan terpisah yang diambil secara manual?

---

## 3. Target Pengguna

### Persona 1 — Bu Wulan, calon pemilik rumah (pengguna utama)

Sedang merencanakan pembangunan rumah tapak pertamanya di kota pesisir. Sudah punya gambar arsitektur dari desainer, tapi keputusan bahan dinding dan atap masih mengikuti saran tukang. Melek aplikasi desktop dasar (spreadsheet, aplikasi RAB), tidak punya latar belakang teknik sipil atau lingkungan.

**Yang ia butuhkan:** cara membandingkan pilihan bahan yang tidak memaksanya memahami istilah teknik, dengan alasan yang bisa ia sampaikan ulang ke kontraktornya.

### Persona 2 — Pak Herman, pemilik rumah yang merenovasi

Atap rumahnya bocor dan lapuk lebih cepat dari perkiraan setelah 6 tahun, ia curiga bahan yang dipakai tidak cocok untuk daerahnya yang curah hujannya tinggi. Ingin memastikan penggantian kali ini tidak terulang masalah yang sama.

**Yang ia butuhkan:** pembanding yang secara eksplisit memperhitungkan riwayat kerusakan dini sebagai bagian dari simulasi emisi dan biaya jangka panjang, bukan hanya harga beli.

### Persona 3 — Kontraktor kecil / tukang (penerima laporan, bukan pengguna aplikasi)

Menerima laporan rekomendasi dari pemilik rumah dan menjalankannya. Tidak membuka aplikasi Wastu sendiri.

**Yang ia butuhkan:** laporan ekspor yang ringkas dan bisa dicetak atau dibawa ke lokasi proyek, memuat bahan yang direkomendasikan, alasannya, dan jadwal pekerjaan yang disarankan — bukan dashboard interaktif.

---

## 4. Tujuan dan Metrik Keberhasilan

### 4.1 Metrik produk

| Metrik | Target |
|---|---|
| Waktu dari profil proyek dibuat sampai peringkat perbandingan pertama tampil | < 3 menit |
| Jumlah bahan dinding pada basis data MVP | ≥ 15 |
| Jumlah bahan atap pada basis data MVP | ≥ 10 |
| Waktu hitung peringkat perbandingan untuk satu komponen | < 2 detik pada perangkat kelas menengah |
| Pengguna dapat menjelaskan ulang alasan peringkat teratas tanpa istilah teknik | Diverifikasi lewat uji pengguna kualitatif, bukan otomatis |

### 4.2 Metrik kualitas rekomendasi

| Metrik | Target | Catatan |
|---|---|---|
| Konsistensi arah skor dengan intuisi domain | Bahan tahan lembap harus selalu mengungguli bahan tidak tahan lembap pada profil iklim curah hujan tinggi yang identik selain atribut itu | Diuji dengan skenario sintetis, bukan data pengguna nyata |
| Sensitivitas simulasi emisi terhadap usia pakai efektif | Bahan dengan usia pakai efektif lebih pendek pada iklim tertentu harus menghasilkan emisi kumulatif horizon-panjang yang lebih tinggi, ceteris paribus | Uji unit pada modul simulasi emisi (§11.3) |
| Keberhasilan panggilan API cuaca (prakiraan harian) | ≥ 95% saat online | Kegagalan harus terdegradasi dengan pesan yang jelas, bukan macet (§8.4) |

**Aturan main:** karena basis data bahan dan faktor emisi pada MVP bersumber dari data publik internasional (§11.4), tim wajib melaporkan secara terbuka bahan mana yang nilainya adalah perkiraan literatur, bukan pengukuran lokal — konsisten dengan batasan yang dinyatakan di §1.

---

## 5. Scope MVP dan Non-Scope

### 5.1 Di dalam scope

- Aplikasi desktop WPF (.NET, C#) single-user, basis data PostgreSQL lokal
- Manajemen profil proyek: lokasi (kota/koordinat) dan komponen yang dikerjakan
- Basis data bahan untuk **dinding dan atap** dengan properti termal, ketahanan lembap, ketahanan korosi, usia pakai nominal, dan faktor emisi
- Analisis iklim lokasi dari data historis curah hujan, kelembapan, suhu, dan kedekatan pesisir, diambil dari API cuaca pihak ketiga (§8)
- Perbandingan berperingkat: skor kesesuaian dan kgCO2e berdampingan, disertai alasan penilaian per bahan
- Simulasi emisi jangka panjang yang memperhitungkan pengulangan akibat penggantian dini (§11.3)
- Penjadwal pekerjaan sensitif cuaca berbasis prakiraan harian, untuk pengecoran dan pengecatan (§12)
- Ekspor laporan rekomendasi ke PDF untuk dibawa ke kontraktor
- Mode tersambung dan mode luring terbatas: data iklim dan prakiraan yang sudah pernah diambil disimpan lokal dan tetap bisa ditinjau tanpa koneksi (§8.4)

### 5.2 Di luar scope — dinyatakan eksplisit

| Tidak dibangun | Alasan |
|---|---|
| Komponen selain dinding dan atap (lantai, pondasi, plafon) | Basis data properti dan faktor emisi per komponen berbeda signifikan; satu pasang komponen dulu, sesuai brief |
| Aplikasi mobile atau web | Tipe aplikasi ditetapkan sebagai desktop WPF; sinkronisasi lintas perangkat pasca-MVP |
| Rencana anggaran biaya (RAB) dan estimasi harga material | Sudah dilayani aplikasi lokal lain (§13.3); Wastu fokus pada iklim dan karbon, bukan biaya |
| Perhitungan struktural (kekuatan, beban, tulangan) | Di luar kompetensi domain tim; Wastu menilai kesesuaian iklim dan emisi, bukan keselamatan struktural |
| Marketplace atau integrasi pemesanan bahan ke pemasok | Pasca-MVP; MVP berhenti pada rekomendasi dan ekspor laporan |
| Kalibrasi faktor emisi dan properti bahan khusus pasar Indonesia | Membutuhkan riset lapangan/EPD lokal yang belum tersedia; MVP memakai basis data publik internasional dengan batasan dinyatakan terbuka (§11.4) |
| Sinkronisasi multi-pengguna atau multi-perangkat | PostgreSQL pada MVP berjalan sebagai instans lokal untuk satu pengguna; server bersama adalah pekerjaan lanjutan |
| Notifikasi push/WhatsApp untuk perubahan cuaca | Aplikasi desktop dicek aktif oleh pengguna, bukan berjalan di latar belakang terus-menerus |

---

## 6. Fitur dan User Stories

### F1 — Profil Proyek

> Sebagai calon pemilik rumah, saya ingin mencatat lokasi dan komponen yang sedang saya kerjakan agar rekomendasi yang saya terima sesuai dengan proyek saya yang sebenarnya.

**Acceptance criteria**

- Pengguna dapat membuat, mengubah, dan menghapus profil proyek: nama proyek, kota, koordinat (lat/lon, otomatis dari kota atau diisi manual)
- Satu proyek dapat memiliki lebih dari satu komponen (dinding, atap), masing-masing dengan luas area
- Mengubah lokasi proyek memicu pengambilan ulang profil iklim (§F3)
- Validasi: koordinat harus berada dalam wilayah Indonesia pada MVP

### F2 — Basis Data Bahan

> Sebagai calon pemilik rumah, saya ingin melihat daftar bahan yang tersedia untuk dinding dan atap beserta karakteristiknya agar saya tahu apa saja pilihannya sebelum membandingkan.

**Acceptance criteria**

- Setiap bahan memuat: nama, jenis komponen (dinding/atap), konduktivitas termal, indeks ketahanan lembap, indeks ketahanan korosi, usia pakai nominal (tahun), faktor emisi (kgCO2e per satuan luas atau volume)
- Bahan dapat difilter berdasarkan jenis komponen
- Sumber data setiap nilai (basis data publik yang dipakai) ditampilkan agar transparan, bukan disembunyikan sebagai angka pasti (§11.4)
- Admin/pengembang dapat menambah bahan baru melalui basis data tanpa mengubah kode aplikasi

### F3 — Analisis Iklim Lokasi

> Sebagai calon pemilik rumah, saya ingin sistem menganalisis iklim lokasi proyek saya secara otomatis agar saya tidak perlu mencari data cuaca sendiri.

**Acceptance criteria**

- Sistem mengambil data historis curah hujan tahunan, kelembapan rata-rata, suhu rata-rata dari API cuaca pihak ketiga (§8) berdasarkan koordinat proyek
- Kedekatan pesisir dihitung dari koordinat (jarak ke garis pantai terdekat) sebagai proksi risiko korosi akibat udara bergaram
- Profil iklim disimpan per proyek dan dipakai ulang sampai pengguna mengubah lokasi atau meminta pembaruan manual
- Profil iklim ditampilkan dalam bentuk ringkas (bukan grafik mentah) sebelum masuk ke perbandingan bahan

### F4 — Perbandingan Berperingkat

> Sebagai calon pemilik rumah, saya ingin melihat bahan-bahan diurutkan dari yang paling cocok, lengkap dengan skor kesesuaian dan jejak karbonnya, agar saya bisa membandingkan tanpa menghitung sendiri.

**Acceptance criteria**

- Untuk komponen terpilih, sistem menampilkan daftar bahan terurut berdasarkan skor kesesuaian iklim (§11.1)
- Setiap baris menampilkan skor kesesuaian (0–100) dan kgCO2e berdampingan, bukan pada tab terpisah
- Setiap bahan disertai alasan penilaian dalam kalimat (contoh: "Ketahanan lembap rendah untuk curah hujan tahunan tinggi di lokasi Anda")
- Pengguna dapat mengubah bobot kriteria (termal/lembap/korosi/usia pakai) dan melihat peringkat berubah secara langsung
- Bahan dengan skor kesesuaian di bawah ambang minimum ditandai visual sebagai "tidak disarankan", bukan disembunyikan

### F5 — Simulasi Emisi Jangka Panjang

> Sebagai calon pemilik rumah, saya ingin tahu berapa total emisi karbon suatu bahan sepanjang usia rumah saya, termasuk kalau bahan itu harus diganti lebih cepat karena tidak cocok dengan iklim saya.

**Acceptance criteria**

- Pengguna memilih horizon simulasi (contoh: 20, 30, 50 tahun)
- Usia pakai efektif bahan disesuaikan turun dari usia pakai nominal berdasarkan skor kesesuaian iklim (§11.3)
- Sistem menghitung jumlah penggantian yang diproyeksikan dalam horizon tersebut dan emisi kumulatif hasilnya
- Hasil ditampilkan sebagai perbandingan visual sederhana antar bahan (bukan grafik teknis rumit), dengan angka kgCO2e kumulatif sebagai sorotan utama
- Antarmuka menyatakan terbuka bahwa usia pakai efektif adalah estimasi, bukan jaminan (§11.4)

### F6 — Penjadwal Pekerjaan Sensitif Cuaca

> Sebagai calon pemilik rumah, saya ingin tahu tanggal mana yang cuacanya aman untuk pengecoran atau pengecatan, agar saya tidak perlu mengerjakan ulang karena cuaca salah.

**Acceptance criteria**

- Pengguna mendaftarkan pekerjaan terjadwal (contoh: "Pengecoran dinding", "Pengecatan atap") dengan rentang tanggal rencana
- Sistem mengambil prakiraan cuaca harian untuk lokasi proyek dan mengevaluasi tiap hari terhadap ambang layak-kerja per jenis pekerjaan (§12)
- Hari yang tidak layak ditandai jelas beserta alasannya (contoh: "Probabilitas hujan 70%, di atas ambang pengecoran")
- Pengguna dapat mengunci satu tanggal sebagai jadwal final setelah melihat evaluasi
- Jika prakiraan tidak tersedia (di luar jangkauan API, biasanya >14 hari ke depan), sistem menyatakan itu secara eksplisit, bukan menampilkan status kosong tanpa penjelasan

### F7 — Ekspor Laporan Rekomendasi

> Sebagai calon pemilik rumah, saya ingin mengunduh ringkasan rekomendasi bahan dan jadwal dalam satu berkas agar bisa saya tunjukkan ke kontraktor.

**Acceptance criteria**

- Menghasilkan PDF berisi: profil proyek, profil iklim ringkas, peringkat bahan terpilih dengan skor dan kgCO2e, simulasi emisi horizon terpilih, dan jadwal pekerjaan yang telah dikunci
- Laporan memuat tanggal pembuatan dan sumber data (API cuaca, basis data bahan) sebagai catatan kaki transparansi
- Laporan disimpan sebagai berkas lokal dan dapat dibuka ulang dari dalam aplikasi

---

## 7. Arsitektur Sistem

```
┌─ ANTARMUKA PENGGUNA ───────────────────────────────────────┐
│  WPF (.NET 8 LTS, C#), pola MVVM                            │
│   ├─ Modul Profil Proyek & Komponen                        │
│   ├─ Modul Katalog Bahan                                    │
│   ├─ Modul Perbandingan & Simulasi Emisi                    │
│   ├─ Modul Penjadwal Pekerjaan                              │
│   └─ Modul Ekspor Laporan                                   │
└───────────────────────┬──────────────────────────────────────┘
                        │ binding via ViewModel
┌───────────────────────▼──────────────────────────────────────┐
│  LAPISAN LAYANAN APLIKASI (Application/Domain Services)     │
│   ├─ ProjectService, MaterialService                        │
│   ├─ ClimateAnalysisService     ──┐                         │
│   ├─ SuitabilityScoringService    │  §11                    │
│   ├─ EmissionSimulationService  ──┘                         │
│   ├─ WorkSchedulingService          §12                     │
│   └─ ReportExportService (PDF)                               │
└───────┬───────────────────────────────────┬──────────────────┘
        │                                   │
┌───────▼───────────────┐        ┌──────────▼───────────────────┐
│ LAPISAN DATA           │        │ KLIEN LAYANAN CUACA           │
│ EF Core + Npgsql        │        │ WeatherApiClient               │
│ PostgreSQL (lokal)      │        │  ├─ HistoricalClimateClient    │
│ (§9)                    │        │  └─ DailyForecastClient        │
└─────────────────────────┘        │  + cache lokal (§8.4)          │
                                    └──────────┬──────────────────┘
                                                │ HTTPS
                                    ┌───────────▼──────────────────┐
                                    │ API Cuaca Pihak Ketiga (§8)   │
                                    └────────────────────────────────┘
```

### 7.1 Pilihan teknologi

| Lapisan | Pilihan | Alasan |
|---|---|---|
| Antarmuka | WPF, .NET 8 (LTS), pola MVVM | Ditetapkan brief; .NET 8 dipilih atas versi preview agar dukungan jangka panjang terjamin sampai aplikasi selesai dan dinilai |
| Data access | Entity Framework Core + Npgsql | ORM standar .NET untuk PostgreSQL; migrasi skema terkelola versi (§9) |
| Basis data | PostgreSQL, instans lokal per perangkat pengguna pada MVP | Ditetapkan brief; instans lokal cukup untuk aplikasi single-user, menghindari kompleksitas server bersama di luar scope MVP (§5.2) |
| PDF | Pustaka pembuatan PDF pihak ketiga (mis. QuestPDF) | Menghindari menulis ulang mesin layout PDF; lisensi komunitas cukup untuk skala akademik |
| Klien cuaca | `HttpClient` terbungkus `WeatherApiClient` dengan cache lokal | Memisahkan pemanggilan API mentah dari logika domain; memudahkan penggantian penyedia API tanpa mengubah `ClimateAnalysisService` (§8) |

### 7.2 Struktur repositori (usulan)

```
src/Wastu.App/            WPF — Views, ViewModels, App.xaml
src/Wastu.Domain/         Entity, aturan skoring, simulasi emisi (§11)
src/Wastu.Data/           DbContext EF Core, migrasi, repository
src/Wastu.Weather/        Klien API cuaca, model respons, cache (§8)
src/Wastu.Reporting/      Pembuatan PDF (§F7)
tests/Wastu.Domain.Tests/ Uji unit skoring dan simulasi emisi
docs/                     PRD, diagram UML, referensi basis data bahan
```

---

## 8. Integrasi Layanan Cuaca Pihak Ketiga

### 8.1 Dua kebutuhan data yang berbeda

Wastu membutuhkan dua jenis data cuaca yang berbeda sifat dan tidak boleh disatukan sembarangan:

| | **Data historis iklim (§F3)** | **Prakiraan harian (§F6)** |
|---|---|---|
| Tujuan | Profil iklim jangka panjang lokasi (curah hujan tahunan, kelembapan rata-rata, suhu rata-rata) | Keputusan layak-kerja per hari untuk 1–14 hari ke depan |
| Rentang waktu | Rata-rata/agregat bertahun-tahun | Harian, jangka pendek |
| Frekuensi pengambilan | Sekali per proyek, diperbarui atas permintaan pengguna | Setiap kali penjadwal dibuka, dengan cache singkat |

### 8.2 Rekomendasi penyedia — Open-Meteo

Brief tidak menetapkan penyedia API secara spesifik ("layanan cuaca pihak ketiga"). Rekomendasi untuk MVP adalah **Open-Meteo**, dengan alasan:

- Menyediakan **kedua** kebutuhan di atas dari satu penyedia: *Historical Weather API* (arsip harian sejak beberapa dekade lalu, dapat diagregasi menjadi rata-rata tahunan) dan *Forecast API* (prakiraan harian hingga 16 hari).
- Tidak memerlukan API key untuk kuota non-komersial/akademik, sehingga tidak ada biaya berulang maupun proses verifikasi bisnis yang menghambat pengerjaan proyek kuliah.
- Cakupan global termasuk wilayah Indonesia dengan resolusi grid yang memadai untuk kebutuhan rumah tinggal (bukan riset meteorologi presisi tinggi).

**Alternatif yang dipertimbangkan dan alasan tidak dipakai sebagai jalur utama:** BMKG Open Data lebih "resmi" secara konteks Indonesia, tetapi cakupan endpoint publiknya berfokus pada prakiraan jangka pendek per wilayah administratif, bukan arsip historis harian yang mudah diagregasi menjadi profil iklim tahunan per koordinat bebas. Wastu tetap dapat menambahkan BMKG sebagai sumber pelengkap prakiraan pasca-MVP tanpa mengubah domain, karena `WeatherApiClient` dirancang sebagai lapisan abstraksi (§7).

### 8.3 Kontrak internal `WeatherApiClient`

Terlepas dari penyedia akhir yang dipilih tim, lapisan domain (`ClimateAnalysisService`, `WorkSchedulingService`) hanya bergantung pada dua kontrak berikut, bukan pada bentuk respons API mentah:

```csharp
interface IHistoricalClimateClient {
    Task<ClimateProfile> GetAnnualProfileAsync(double lat, double lon);
}

interface IDailyForecastClient {
    Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days);
}
```

`ClimateProfile` memuat curah hujan tahunan, kelembapan rata-rata, suhu rata-rata. `DailyForecast` memuat tanggal, probabilitas hujan, curah hujan perkiraan, suhu min/maks, kelembapan. Penggantian penyedia API cukup mengganti implementasi kedua antarmuka ini.

### 8.4 Perilaku luring dan kegagalan

- Profil iklim historis (§F3) disimpan permanen di PostgreSQL setelah diambil sekali — pengguna tetap bisa melihat perbandingan bahan tanpa koneksi internet selama tidak meminta pembaruan.
- Prakiraan harian (§F6) di-cache lokal dengan masa berlaku singkat (misalnya 6 jam); permintaan berikutnya dalam masa itu memakai cache, bukan memanggil API ulang.
- Kegagalan panggilan API (jaringan turun, kuota habis) ditangani dengan pesan eksplisit ke pengguna dan fallback ke data cache terakhir jika ada — aplikasi tidak boleh membeku menunggu respons tanpa batas waktu.

---

## 9. Skema Data

PostgreSQL. Entitas mengikuti diagram kelas pada [`docs/uml-wastu.md`](uml-wastu.md).

```sql
users (
  id                uuid PRIMARY KEY,
  name              text NOT NULL,
  email             citext UNIQUE,
  created_at        timestamptz NOT NULL DEFAULT now()
)

projects (
  id                uuid PRIMARY KEY,
  user_id           uuid NOT NULL REFERENCES users(id),
  name              text NOT NULL,
  city              text NOT NULL,
  latitude          double precision NOT NULL,
  longitude         double precision NOT NULL,
  created_at        timestamptz NOT NULL DEFAULT now()
)

climate_profiles (
  project_id            uuid PRIMARY KEY REFERENCES projects(id),
  annual_rainfall_mm    real NOT NULL,
  avg_humidity_pct      real NOT NULL,
  avg_temperature_c     real NOT NULL,
  coastal_distance_km   real NOT NULL,
  corrosion_level       text NOT NULL,      -- 'rendah' | 'sedang' | 'tinggi'
  fetched_at            timestamptz NOT NULL
)

components (
  id                uuid PRIMARY KEY,
  project_id        uuid NOT NULL REFERENCES projects(id),
  kind              text NOT NULL,          -- 'dinding' | 'atap'
  area_m2           real NOT NULL
)

materials (
  id                    uuid PRIMARY KEY,
  name                  text NOT NULL,
  kind                  text NOT NULL,          -- 'dinding' | 'atap'
  thermal_conductivity  real NOT NULL,          -- W/m·K
  moisture_resistance   smallint NOT NULL,      -- indeks 1-5
  corrosion_resistance  smallint NOT NULL,      -- indeks 1-5
  nominal_lifespan_yr   real NOT NULL,
  emission_factor       real NOT NULL,          -- kgCO2e per m2
  data_source           text NOT NULL           -- rujukan basis data publik (§11.4)
)

evaluations (
  id                    bigserial PRIMARY KEY,
  component_id          uuid NOT NULL REFERENCES components(id),
  material_id           uuid NOT NULL REFERENCES materials(id),
  suitability_score     real NOT NULL,          -- 0-100
  initial_emission_kg   real NOT NULL,
  reasoning             text NOT NULL,
  computed_at           timestamptz NOT NULL
)

emission_simulations (
  id                    bigserial PRIMARY KEY,
  evaluation_id         bigint NOT NULL REFERENCES evaluations(id),
  horizon_years         smallint NOT NULL,
  effective_lifespan_yr real NOT NULL,          -- usia pakai setelah penyesuaian iklim
  projected_replacements smallint NOT NULL,
  cumulative_emission_kg real NOT NULL
)

work_items (
  id                uuid PRIMARY KEY,
  project_id        uuid NOT NULL REFERENCES projects(id),
  name              text NOT NULL,             -- 'Pengecoran dinding', dst.
  work_type         text NOT NULL,             -- 'pengecoran' | 'pengecatan'
  planned_start     date NOT NULL,
  planned_end       date NOT NULL,
  locked_date       date,
  status            text NOT NULL              -- 'draft' | 'terkunci' | 'selesai'
)

daily_forecasts_cache (
  project_id        uuid NOT NULL REFERENCES projects(id),
  forecast_date     date NOT NULL,
  rain_probability_pct  real,
  rainfall_mm       real,
  temp_min_c        real,
  temp_max_c        real,
  humidity_pct      real,
  fetched_at        timestamptz NOT NULL,
  PRIMARY KEY (project_id, forecast_date)
)

reports (
  id                uuid PRIMARY KEY,
  project_id        uuid NOT NULL REFERENCES projects(id),
  file_path         text NOT NULL,
  generated_at      timestamptz NOT NULL
)
```

**Catatan implementasi**

- `materials.data_source` wajib diisi untuk setiap baris — konsisten dengan komitmen transparansi sumber data di §1 dan §11.4.
- `daily_forecasts_cache` dipangkas berkala (misalnya baris lebih tua dari 30 hari) karena prakiraan basi tidak berguna dan tidak perlu disimpan permanen, berbeda dengan `climate_profiles` yang memang dipertahankan (§8.4).
- Migrasi skema dikelola lewat EF Core Migrations agar basis data lokal pengguna dapat diperbarui otomatis saat aplikasi diperbarui.

---

## 10. Spesifikasi Modul dan Kontrak Layanan

Wastu adalah aplikasi desktop, bukan layanan berjaringan — tidak ada REST API publik. Kontrak di bawah adalah antarmuka internal antar lapisan (§7), dipakai ViewModel untuk memanggil lapisan layanan.

| Layanan | Metode | Keterangan |
|---|---|---|
| `ProjectService` | `CreateProject`, `UpdateLocation`, `AddComponent` | Mengubah lokasi memicu `ClimateAnalysisService.RefreshProfile` (§F1) |
| `ClimateAnalysisService` | `GetOrFetchProfile(projectId)` | Mengembalikan profil tersimpan atau mengambil baru via `IHistoricalClimateClient` (§8.3) |
| `MaterialService` | `ListByKind(kind)`, `GetById(id)` | Sumber untuk katalog bahan (§F2) |
| `SuitabilityScoringService` | `Rank(componentId, weights?)` | Menghasilkan daftar `Evaluation` terurut (§11.1) |
| `EmissionSimulationService` | `Simulate(evaluationId, horizonYears)` | Menghasilkan `EmissionSimulation` (§11.3) |
| `WorkSchedulingService` | `EvaluateDays(workItemId)`, `LockDate(workItemId, date)` | Mengevaluasi kelayakan tiap hari via `IDailyForecastClient` (§12) |
| `ReportExportService` | `Export(projectId, options)` | Menghasilkan PDF dan mencatat baris `reports` (§F7) |

---

## 11. Metode Penilaian Kesesuaian Iklim dan Emisi Karbon

### 11.1 Skor kesesuaian

Skor kesesuaian per bahan dihitung sebagai jumlah berbobot dari empat sub-skor yang masing-masing dinormalisasi ke rentang 0–100:

```
skor_kesesuaian = w_termal · skor_termal
                + w_lembap · skor_lembap
                + w_korosi · skor_korosi
                + w_usia   · skor_usia
```

dengan `w_termal + w_lembap + w_korosi + w_usia = 1`. Bobot awal disarankan `0.25` masing-masing, dapat diubah pengguna di antarmuka (§F4).

| Sub-skor | Dihitung dari | Arah |
|---|---|---|
| `skor_termal` | Selisih konduktivitas termal bahan terhadap rentang ideal untuk suhu rata-rata lokasi | Konduktivitas terlalu tinggi pada suhu rata-rata tinggi → skor turun |
| `skor_lembap` | `moisture_resistance` bahan relatif terhadap `annual_rainfall_mm` dan `avg_humidity_pct` lokasi | Curah hujan/kelembapan tinggi + ketahanan lembap rendah → skor turun tajam |
| `skor_korosi` | `corrosion_resistance` bahan relatif terhadap `coastal_distance_km` | Semakin dekat pesisir, bobot efektif ketahanan korosi semakin dominan |
| `skor_usia` | `nominal_lifespan_yr` dinormalisasi relatif terhadap bahan lain di kategori yang sama | Usia pakai nominal lebih panjang → skor lebih tinggi, sebelum disesuaikan iklim (§11.3) |

Setiap sub-skor yang berkontribusi signifikan pada skor akhir dirender menjadi kalimat alasan (`reasoning`) — bukan hanya angka — supaya persona non-teknis (§3) bisa memahami tanpa membaca rumus.

### 11.2 kgCO2e awal

```
emisi_awal = emission_factor(bahan) × luas_area(komponen)
```

Ditampilkan berdampingan dengan `skor_kesesuaian` pada peringkat (§F4) — inilah pasangan angka inti yang menjadi pembeda Wastu dari kalkulator karbon murni maupun alat kesesuaian iklim murni (§1).

### 11.3 Simulasi emisi jangka panjang — pengganda penggantian dini

Ini adalah mekanisme yang menerjemahkan masalah di §2.2 menjadi angka. Usia pakai efektif diturunkan dari usia pakai nominal, disesuaikan oleh skor kesesuaian:

```
usia_pakai_efektif = usia_pakai_nominal × f(skor_kesesuaian)
```

dengan `f` fungsi monoton naik memetakan skor 0–100 ke faktor penyesuaian, misalnya `f(skor) = 0.5 + 0.5 × (skor / 100)` — bahan dengan skor kesesuaian 0 diasumsikan hanya bertahan separuh usia nominalnya, bahan dengan skor 100 mempertahankan usia nominal penuh. Konstanta ini adalah nilai awal yang **wajib** dinyatakan sebagai asumsi yang dapat dikalibrasi (§11.4), bukan hasil pengukuran.

Jumlah penggantian dalam horizon simulasi:

```
jumlah_penggantian = floor(horizon_tahun / usia_pakai_efektif)
emisi_kumulatif     = emisi_awal × (1 + jumlah_penggantian)
```

**Pemeriksaan kewajaran.** Untuk bahan dengan usia pakai nominal 15 tahun dan horizon simulasi 30 tahun: pada skor kesesuaian 100 (`usia_efektif = 15 tahun`) terjadi 1 penggantian (emisi ×2); pada skor kesesuaian 20 (`usia_efektif = 9 tahun`) terjadi 2 penggantian (emisi ×3). Arah hasil ini konsisten dengan argumen inti Wastu — bahan yang buruk untuk iklim setempat memicu emisi berulang — dan menjadi kasus uji unit wajib untuk `EmissionSimulationService` (§4.2).

### 11.4 Batasan yang harus dinyatakan di antarmuka

> Nilai properti bahan dan faktor emisi pada MVP bersumber dari basis data publik internasional (mis. ICE Database, EPD generik), yang belum tentu merepresentasikan produk spesifik yang tersedia di pasar lokal Indonesia. Fungsi penyesuaian usia pakai efektif (§11.3) adalah model sederhana, bukan hasil studi lapangan. Kurasi data spesifik pasar lokal dan kalibrasi fungsi penyesuaian masuk daftar pekerjaan lanjutan (§14, R5).

---

## 12. Penjadwal Pekerjaan Sensitif Cuaca — Logika Keputusan

Setiap `work_type` memiliki ambang layak-kerja sendiri, dievaluasi terhadap `DailyForecast` (§8.3):

| `work_type` | Ambang tidak layak | Alasan |
|---|---|---|
| `pengecoran` | Probabilitas hujan > 60%, atau curah hujan perkiraan > 5 mm | Hujan saat curing awal beton mengganggu proses hidrasi semen dan merusak permukaan |
| `pengecoran` | Suhu maksimum > 35 °C tanpa mitigasi (dicatat sebagai peringatan, bukan larangan keras) | Suhu tinggi mempercepat penguapan air campuran, berisiko retak susut |
| `pengecatan` | Kelembapan relatif > 85% | Sebagian besar cat berbasis air/solvent mensyaratkan kelembapan aplikasi di bawah ambang ini agar kering sempurna |
| `pengecatan` | Probabilitas hujan > 40% | Cat yang belum kering tersapu hujan, memicu pengerjaan ulang (§2.2) |
| `pengecatan` | Suhu di luar rentang 10–35 °C | Rentang aplikasi umum cat arsitektur; di luar rentang ini daya rekat menurun |

Nilai ambang di atas adalah nilai awal berbasis praktik umum konstruksi dan spesifikasi cat generik — **wajib dinyatakan dapat dikonfigurasi**, bukan konstanta tersembunyi, karena spesifikasi produk aktual bervariasi per merek bahan (§14, R6).

Hari yang gagal pada ambang manapun ditandai "tidak layak" beserta ambang mana yang dilanggar (§F6, acceptance criteria). Pengguna tetap dapat mengunci tanggal yang ditandai tidak layak jika memilih menanggung risikonya — Wastu memberi informasi, bukan memaksakan keputusan.

---

## 13. Analisis Kompetitor

### 13.1 One Click LCA dan Embodied Carbon in Construction Calculator — kompetitor tidak langsung

Alat analisis siklus hidup bangunan yang mapan, dipakai praktisi proyek besar untuk pelaporan kepatuhan karbon.

| Kelebihan | Kekurangan |
|---|---|
| Basis data material sangat luas dan divalidasi pihak ketiga | Berbasis web, berlangganan berorientasi proyek komersial besar |
| Diakui pada standar pelaporan karbon internasional | Basis material didominasi produk Eropa/Amerika Utara, minim representasi pasar Indonesia |
| Mendukung analisis siklus hidup penuh (cradle-to-grave) | Menuntut input teknis rinci yang tidak realistis bagi pemilik rumah awam |

**Posisi Wastu:** kedua alat ini menyasar praktisi proyek besar dengan kebutuhan pelaporan formal. Wastu menyasar pemilik rumah individual dengan alat yang tidak menuntut keahlian LCA, dan secara eksplisit mengaitkan rekomendasi dengan iklim Indonesia (§1).

### 13.2 EnergyPlus — kompetitor tidak langsung

Perangkat lunak simulasi performa termal bangunan yang sangat mendalam, dipakai untuk pemodelan energi bangunan profesional.

| Kelebihan | Kekurangan |
|---|---|
| Pemodelan termal sangat akurat dan tervalidasi luas di dunia akademik/industri | Kurva belajar sangat curam, menuntut keahlian teknik bangunan |
| Dapat memodelkan interaksi kompleks antar komponen bangunan | Tidak merekomendasikan bahan — hanya memprediksi performa dari input yang sudah ditentukan pengguna |
| Gratis dan open-source | Tidak memiliki aspek jejak karbon maupun penjadwalan kerja |

**Posisi Wastu:** EnergyPlus menjawab "bagaimana performa termal bangunan ini" bagi yang sudah tahu bahannya. Wastu menjawab pertanyaan sebelumnya — "bahan mana yang sebaiknya dipilih" — dengan model kesesuaian yang jauh lebih sederhana namun cukup untuk keputusan rumah tinggal individual.

### 13.3 Aplikasi RAB/volume material lokal — kompetitor tidak langsung

Aplikasi mobile dan desktop lokal yang membantu menghitung rencana anggaran biaya dan volume kebutuhan material konstruksi.

| Kelebihan | Kekurangan |
|---|---|
| Fokus pada kebutuhan paling mendesak pemilik rumah: biaya | Tidak ada aspek kesesuaian iklim maupun jejak karbon sama sekali |
| Familiar dan banyak dipakai kontraktor kecil di Indonesia | Tidak membantu memilih *jenis* bahan, hanya menghitung volume dari bahan yang sudah diputuskan |
| Antarmuka berbahasa Indonesia, sesuai konteks lokal | Tidak terhubung dengan data cuaca atau penjadwalan pekerjaan |

**Posisi Wastu:** tidak bersaing pada RAB — Wastu secara eksplisit menyatakan estimasi biaya di luar scope (§5.2). Wastu mengisi tahap *sebelum* RAB dibuat: menentukan bahan mana yang layak dipertimbangkan sama sekali.

### 13.4 Status quo — mengikuti saran tukang

Bagi sebagian besar calon pemilik rumah, pembanding sebenarnya bukan aplikasi lain, melainkan kebiasaan mengikuti saran tukang atau kontraktor tanpa verifikasi independen.

| Kelebihan | Kekurangan |
|---|---|
| Tanpa biaya, tanpa perlu belajar aplikasi apa pun | Saran tukang mencerminkan kebiasaan dan ketersediaan, bukan kecocokan iklim (§2.1) |
| Dipercaya karena pengalaman lapangan tukang yang nyata | Tidak ada pertimbangan jejak karbon dan tidak ada bukti tertulis untuk dibandingkan |

**Posisi Wastu:** hambatan adopsi terbesar bukan kompetitor perangkat lunak, melainkan meyakinkan pengguna bahwa keputusan bahan layak diverifikasi sebelum diikuti. Laporan ekspor (§F7) dirancang agar mudah dibawa dan didiskusikan dengan tukang/kontraktor yang sama, bukan menggantikannya.

---

## 14. Risiko dan Mitigasi

| # | Risiko | Dampak | Mitigasi |
|---|---|---|---|
| **R1** | Basis data bahan dan faktor emisi bersumber dari data publik internasional, belum representatif pasar Indonesia | Rekomendasi dituding tidak relevan secara lokal | Sumber data dicatat per bahan (`data_source`) dan dinyatakan sebagai batasan di antarmuka (§11.4); kurasi lokal masuk pekerjaan lanjutan |
| **R2** | Fungsi penyesuaian usia pakai efektif (§11.3) adalah model sederhana tanpa validasi lapangan | Simulasi emisi jangka panjang bisa menyesatkan jika dianggap presisi | Konstanta fungsi dinyatakan sebagai asumsi yang dapat dikonfigurasi; hasil disajikan sebagai estimasi, bukan angka pasti |
| **R3** | API cuaca pihak ketiga berubah format, membatasi kuota, atau tidak tersedia | Fitur analisis iklim (F3) dan penjadwal (F6) berhenti berfungsi | `WeatherApiClient` diabstraksi lewat `IHistoricalClimateClient`/`IDailyForecastClient` (§8.3) sehingga penyedia dapat diganti tanpa mengubah domain; cache lokal menjaga fungsi dasar tetap berjalan (§8.4) |
| **R4** | Ambang layak-kerja cuaca (§12) bersifat generik, tidak spesifik merek bahan | Rekomendasi jadwal bisa terlalu konservatif atau longgar untuk produk tertentu | Ambang dinyatakan dapat dikonfigurasi per jenis pekerjaan, bukan konstanta tersembunyi |
| **R5** | Kedekatan pesisir dihitung sebagai proksi jarak geometris ke garis pantai, bukan pengukuran salinitas udara aktual | Skor ketahanan korosi bisa meleset di lokasi dengan mikroklimat khusus | Dinyatakan sebagai proksi, bukan pengukuran langsung, di alasan penilaian (§F4) |
| **R6** | Instans PostgreSQL lokal per pengguna menyulitkan berbagi data lintas perangkat | Pengguna yang berganti perangkat kehilangan riwayat proyek | Dinyatakan eksplisit sebagai non-scope MVP (§5.2); ekspor/impor basis data proyek dicatat sebagai pekerjaan lanjutan |
| **R7** | Pengguna awam mengunci jadwal pada hari yang ditandai "tidak layak" tanpa memahami konsekuensinya | Pengerjaan ulang tetap terjadi meski sudah diperingatkan | Peringatan disertai alasan tekstual yang jelas (§12); penguncian tetap diizinkan sebagai keputusan pengguna, bukan diblokir paksa |

---

## 15. Rencana Rilis Bertahap

Tanpa penanggalan kalender — urutan dan ketergantungan yang mengikat, bukan tanggalnya.

### MVP — alur inti berjalan ujung ke ujung

Tujuan: dari profil proyek sampai peringkat bahan tampil, dengan data iklim nyata.

- Skema basis data dan migrasi EF Core (§9)
- Seed basis data bahan dinding dan atap (≥ 15 dan ≥ 10 entri, §4.1) dengan sumber data tercatat
- `WeatherApiClient`: implementasi `IHistoricalClimateClient` terhadap Open-Meteo (§8)
- F1 — profil proyek dan komponen
- F3 — analisis iklim lokasi
- F2 — katalog bahan
- F4 — perbandingan berperingkat dengan `SuitabilityScoringService` (§11.1–11.2)

**Kriteria selesai:** pengguna membuat proyek baru, memasukkan lokasi, dan melihat peringkat bahan dengan skor dan kgCO2e untuk satu komponen.

### V1 — konsekuensi jangka panjang dan pelaksanaan

Tujuan: menjawab "kalau saya salah pilih, apa akibatnya" dan "kapan sebaiknya saya kerjakan".

- F5 — simulasi emisi jangka panjang dengan penyesuaian usia pakai efektif (§11.3)
- Implementasi `IDailyForecastClient` terhadap Open-Meteo, cache prakiraan (§8.4)
- F6 — penjadwal pekerjaan sensitif cuaca (§12)

**Kriteria selesai:** simulasi emisi menunjukkan pengganda penggantian dini pada kasus uji §11.3; penjadwal menandai hari tidak layak dengan alasan yang benar untuk minimal satu skenario cuaca buruk.

### V2 — pelaporan dan penghalusan

Tujuan: hasil aplikasi bisa dibawa keluar aplikasi dan pengalaman pengguna dihaluskan.

- F7 — ekspor laporan PDF
- Pembobotan kriteria yang dapat diubah pengguna pada F4
- Uji pengguna kualitatif dengan persona non-teknis (§4.1)
- Penghalusan alasan tekstual (`reasoning`) berdasarkan umpan balik uji pengguna

**Kriteria selesai:** satu proyek lengkap dari pembuatan sampai laporan PDF berhasil diekspor dan dapat dibuka ulang, tanpa istilah teknis yang tidak dijelaskan di dalam laporan.

### Pasca-MVP — didokumentasikan, tidak dibangun

Kurasi faktor emisi dan properti bahan khusus pasar Indonesia; kalibrasi fungsi penyesuaian usia pakai efektif terhadap data lapangan; komponen selain dinding/atap; sinkronisasi multi-perangkat; integrasi pemesanan bahan ke pemasok; aplikasi mobile pendamping.

---

## 16. Lampiran

### 16.1 Peran tim

| Nama | NIM | Peran | Tanggung jawab utama di PRD ini |
|---|---|---|---|
| Dien Muhammad Scientivan Kurniapramono | 24/533571/TK/59114 | Software Architect | Arsitektur sistem (§7), integrasi cuaca (§8), skema data (§9), kontrak modul (§10) |
| Ramzi Alfito Rizky | 24/540550/TK/60008 | Backend Developer | Lapisan layanan dan domain (§10, §11), simulasi emisi (§11.3) |
| Yohanes Anthony Saputra | 24/536237/TK/59524 | Frontend Developer | Antarmuka WPF dan pengalaman pengguna (§6), penjadwal pekerjaan (§12) |

Metode penilaian kesesuaian dan simulasi emisi (§11) serta logika penjadwalan (§12) dikerjakan bersama, karena keduanya menjadi titik temu antara data bahan, data iklim, dan pengalaman pengguna akhir.

### 16.2 Rujukan awal

Daftar berikut adalah titik awal riset, bukan daftar pustaka final — tim wajib memverifikasi tautan dan lisensi sebelum dipakai sebagai sumber data produksi (konsisten dengan komitmen transparansi §11.4).

**Data iklim**

1. Open-Meteo — Historical Weather API & Forecast API. https://open-meteo.com/
2. BMKG Open Data (alternatif/pelengkap prakiraan). https://data.bmkg.go.id/

**Properti bahan dan faktor emisi**

3. Inventory of Carbon and Energy (ICE) Database, University of Bath — faktor emisi tersemat material konstruksi umum.
4. Environmental Product Declaration (EPD) generik untuk kategori bahan dinding dan atap yang relevan.

**Konteks masalah dan kompetitor**

5. One Click LCA — https://www.oneclicklca.com/
6. Embodied Carbon in Construction Calculator (EC3) — https://www.buildingtransparency.org/
7. EnergyPlus — https://energyplus.net/

### 16.3 Keterkaitan dengan diagram UML

Entitas dan alur pada dokumen ini konsisten dengan [`docs/uml-wastu.md`](uml-wastu.md) (use case, activity, dan class diagram). Penyesuaian yang dilakukan PRD ini terhadap diagram awal:

| Aspek | Diagram UML awal | PRD ini | Alasan |
|---|---|---|---|
| `Evaluasi.hitungSkor` | Disebut sebagai satu metode tanpa rincian | Dirinci menjadi jumlah berbobot 4 sub-skor (§11.1) | Diperlukan agar dapat diimplementasikan dan diuji |
| `SimulasiEmisi.proyeksikan` | Disebut sebagai satu metode tanpa rincian | Dirinci menjadi penyesuaian usia pakai efektif + penggandaan penggantian (§11.3) | Menjadikan argumen "penggantian dini memicu emisi berulang" (§1) sebagai perhitungan konkret, bukan pernyataan |
| `PrakiraanCuaca.layakKerja` | Disebut sebagai satu metode boolean | Dirinci menjadi ambang per `work_type` (§12) | Pengecoran dan pengecatan punya sensitivitas cuaca yang berbeda dan tidak bisa memakai satu ambang yang sama |
