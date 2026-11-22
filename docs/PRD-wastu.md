# Wastu — Product Requirements Document

**Pembanding Bahan Bangunan Berbasis Iklim dan Jejak Karbon untuk Pemilik Rumah**

| | |
|---|---|
| **Kelompok** | Manut |
| **Mata Kuliah** | Junior Project |
| **Kategori** | Sustainable Construction / Climate Action |
| **Tipe Aplikasi** | Desktop Windows Presentation Foundation (.NET 8, C#), basis data relasional PostgreSQL (lokal/remote) dengan snapshot lokal luring dan mode JSON kompatibilitas, integrasi layanan cuaca pihak ketiga |
| **Versi Dokumen** | 2.0 (Revisi Penyelarasan Spesifikasi Target V2 — PRD-R01–R11) |
| **Tanggal** | 22 November 2026 |
| **Status** | Spesifikasi Normatif Final (Menyatukan PRD-R01 sampai PRD-R11) |

---

## 1. Ringkasan Eksekutif

Wastu adalah aplikasi desktop yang membantu calon pemilik rumah memilih bahan dinding dan atap berdasarkan **kesesuaian dengan iklim lokasi** dan **jejak karbon tersemat**, lalu membantu menjadwalkan pekerjaan konstruksi mengikuti prakiraan cuaca.

Perbedaannya dengan kebiasaan yang berlaku hari ini terletak pada satu hal: pemilihan bahan bangunan rumah tinggal di Indonesia hampir selalu ditentukan oleh harga dan kebiasaan tukang, bukan oleh kecocokan bahan dengan curah hujan, kelembapan, suhu, dan risiko korosi di lokasi bangunan berdiri. Akibatnya bahan cepat rusak, penggantian dini terjadi lebih sering dari yang seharusnya, dan setiap penggantian mengulang emisi karbon produksi bahan.

**Segmen Pengguna:** calon pemilik rumah tinggal tapak di Indonesia yang sedang merencanakan atau merenovasi **dinding dan atap**. Aplikasi mendukung fleksibilitas komponen: pemilik proyek dapat mengevaluasi dinding saja, atap saja, atau kedua komponen sekaligus (§PRD-R10).

**Tiga keputusan yang membedakan Wastu dari sekadar "kalkulator RAB" atau "kalkulator karbon":**

1. **Iklim dan karbon dinilai berdampingan, bukan terpisah.** Skor kesesuaian iklim (0–100) dan emisi awal ($\text{kgCO}_2\text{e}$) ditampilkan pada satu peringkat yang sama berdampingan di setiap baris, disertai alasan tekstual berbasis properti fisik material terhadap iklim lokasi, sehingga pengguna awam tidak perlu menafsirkan dua angka dari dua alat berbeda (§11.1–11.2).
2. **Emisi dihitung sebagai konsekuensi jangka panjang, bukan angka sekali hitung.** Bahan yang tidak cocok dengan iklim diberi usia pakai efektif lebih pendek. Simulasi emisi jangka panjang (20, 30, 50 tahun) menghitung kejadian penggantian pada interval $0 < t < H$ dengan rumus $n = \max(0, \lceil H/L \rceil - 1)$ (§PRD-R02, §11.3).
3. **Penjadwalan kerja terintegrasi dengan rekomendasi bahan, bukan fitur terpisah.** Ambang cuaca layak-kerja untuk pengecoran dan pengecatan menggunakan ambang jenis pekerjaan dan lembar spesifikasi produk bahan terpilih, bukan mengarang ambang otomatis dari indeks ketahanan material (§PRD-R05, §12).

**Batasan yang dinyatakan terbuka:** basis data faktor emisi dan properti bahan bersumber dari basis data publik internasional (ICE Database, EPD generik) yang didominasi konteks Eropa/Amerika Utara. Kurasi nilai spesifik pasar Indonesia adalah pekerjaan berkelanjutan, bukan sesuatu yang selesai di MVP, dan dinyatakan sebagai batasan di antarmuka (§11.4).

Dokumen revisi ini menetapkan target spesifikasi terpadu untuk **seluruh scope produk hingga V2** tanpa mengurangi cakupan 31 Acceptance Criteria F1–F7 (§PRD-R01).

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

| Metrik | Target | Metode Pengukuran & Pembuktian |
|---|---|---|
| Waktu dari profil proyek dibuat sampai peringkat perbandingan pertama tampil | < 3 menit | Uji pengguna kualitatif terhadap persona Bu Wulan dan Pak Herman dengan dataset proyek nyata. |
| Jumlah bahan dinding pada basis data MVP | ≥ 15 | Verifikasi dataset basis data (`kind = 'dinding'`) dengan properti lengkap dan tervalidasi. |
| Jumlah bahan atap pada basis data MVP | ≥ 10 | Verifikasi dataset basis data (`kind = 'atap'`) dengan properti lengkap dan tervalidasi. |
| Waktu hitung peringkat perbandingan untuk satu komponen | < 2 detik pada perangkat kelas menengah | Benchmark komputasi domain dan render UI pada perangkat kelas menengah (Core i5 / Ryzen 5, 8GB RAM). |
| Pengguna dapat menjelaskan ulang alasan peringkat teratas tanpa istilah teknik | Diverifikasi lewat uji pengguna kualitatif, bukan otomatis | Evaluasi wawancara kualitatif pasca-alur pada pengujian pengguna V2 (§PRD-R11). |

### 4.2 Metrik kualitas rekomendasi

| Metrik | Target | Catatan |
|---|---|---|
| Konsistensi arah skor dengan intuisi domain | Bahan tahan lembap harus selalu mengungguli bahan tidak tahan lembap pada profil iklim curah hujan tinggi yang identik selain atribut itu; peningkatan ketahanan tidak menurunkan skor; daerah plateau pada saturasi dijelaskan terbuka (§PRD-R04). | Diuji dengan skenario sintetis numerik pada `PerhitunganSkor`. |
| Sensitivitas simulasi emisi terhadap usia pakai efektif | Bahan dengan usia pakai efektif lebih pendek pada iklim tertentu harus menghasilkan emisi kumulatif horizon-panjang yang lebih tinggi atau sama ($n$ monoton tidak turun), ceteris paribus (§PRD-R04). | Uji unit pada `tests/Wastu.Domain.Tests/Emissions/PerhitunganSimulasiTests.cs` (§11.3). |
| Keberhasilan panggilan API cuaca (prakiraan harian) | ≥ 95% saat online | Diukur dari data yang **valid dan dapat digunakan** (bukan sekadar status HTTP 200). Kegagalan harus terdegradasi dengan pesan yang jelas, bukan macet (§8.4). |

**Aturan main:** karena basis data bahan dan faktor emisi bersumber dari data publik internasional (§11.4), tim wajib melaporkan secara terbuka bahan mana yang nilainya adalah perkiraan literatur, bukan pengukuran lokal — konsisten dengan batasan yang dinyatakan di §1.

---

## 5. Scope MVP dan Non-Scope

### 5.1 Scope Produk F1–F7 (Target Penuh Sampai V2) dan Pemisahan Tahapan

Rencana revisi Wastu mencakup implementasi fungsional penuh dari F1 sampai F7 hingga rilis **V2** (§PRD-R01). Pemisahan tahapan rilis diatur sebagai berikut:

| Tahapan Rilis | Fitur yang Dicakup | Karakteristik Operasional | Kriteria Selesai Tahapan |
|---|---|---|---|
| **MVP** | **F1, F2, F3, F4** | Alur inti pembuatan proyek, pengambilan iklim historis 10 tahun, katalog bahan dinding/atap, perbandingan berperingkat. **Bobot kriteria bernilai tetap 0,25 masing-masing** (kontrol bobot kustom nonaktif pada MVP). Penyimpanan PostgreSQL lokal/remote dan cache cuaca terstruktur. | Pengguna dapat membuat proyek (dinding/atap), mengambil profil iklim riil, dan melihat peringkat bahan berdampingan dengan emisi awal. |
| **V1** | **F5, F6** (melanjutkan F1–F4) | Simulasi emisi kumulatif jangka panjang (20, 30, 50 tahun) dengan fungsi penggantian terkoreksi ($0 < t < H$). Penjadwal pekerjaan cuaca harian 14 hari dengan status multi-nilai (`Layak`, `Waspada`, `TidakLayak`, `DataTidakTersedia`) dan penguncian jadwal kerja. | Simulasi multi-horizon konsisten terhadap rumus penggantian baru; penjadwal mengevaluasi hari kerja dengan indikator cache TTL 6 jam. |
| **V2** | **F7, Pembobotan Kustom, Uji Pengguna** | Ekspor laporan PDF rekomendasi dengan riwayat dan peninjauan luring. Kontrol pembobotan kriteria kustom pada F4 (dengan validasi jumlah 100%, finite, non-negatif, dan tombol reset). Pelaksanaan uji pengguna kualitatif persona. | Dokumen PDF berhasil diekspor dan dibuka kembali; bobot kustom bekerja tanpa request API iklim ulang; bukti uji pengguna terdokumentasi. |

### 5.2 Di Luar Scope — Dinyatakan Eksplisit

| Area / Fitur | Alasan Pengecualian | Status Penanganan |
|---|---|---|
| Autentikasi pengguna / Halaman Login | Wastu adalah aplikasi desktop *single-user*. Seluruh fungsionalitas dapat diakses langsung tanpa kewajiban pendaftaran atau login (§PRD-R10). | Dihapus dari use case dan diagram kelas; tidak ada form login. |
| Sinkronisasi database terdistribusi multi-perangkat | Peninjauan luring pada koneksi database remote diselesaikan melalui **snapshot lokal atomik read-only**, bukan replikasi multi-master terdistribusi (§PRD-R06). | Mode luring remote bersifat read-only; pencatatan ekspor PDF lokal direkonsiliasi secara idempoten saat online. |
| Penegakan batas negara administratif presisi | Bounding box koordinat ($-11 \le \text{lat} \le 6$, $95 \le \text{lon} \le 141$) adalah filter kasar area layanan Wastu, bukan verifikasi batas administratif (*reverse geocoding* administratif di luar scope, §PRD-R07). | Koordinat di luar batas ditolak dengan pesan informatif wilayah layanan. |
| Komponen struktur selain dinding dan atap | Pembatasan pada dinding dan penutup atap menjaga fokus komparasi material arsitektural rumah tinggal. Pondasi, kolom beton bertulang, dan interior berada di luar scope. | Dipertahankan hanya untuk dinding dan atap. |
| Notifikasi push background / layanan daemon | Wastu adalah aplikasi desktop interaktif yang berjalan saat dibuka oleh pengguna, bukan background service yang berjalan terus-menerus. | Pemeriksaan cuaca dilakukan saat halaman penjadwal dibuka. |

---

## 6. Fitur dan User Stories

### F1 — Profil Proyek

> Sebagai calon pemilik rumah, saya ingin mencatat lokasi dan komponen yang sedang saya kerjakan agar rekomendasi yang saya terima sesuai dengan proyek saya yang sebenarnya.

**Acceptance criteria**

- Pengguna dapat membuat, mengubah, dan menghapus profil proyek: nama proyek, kota, koordinat (lat/lon, otomatis dari kota atau diisi manual).
- Satu proyek dapat memiliki satu atau lebih komponen (dinding saja, atap saja, atau keduanya), masing-masing dengan luas area positif ($> 0\text{ m}^2$). Pembuatan proyek tanpa komponen ditolak (§PRD-R10).
- Mengubah lokasi proyek memicu pengambilan ulang profil iklim (§F3), menginvalidasi cache cuaca lokasi lama, dan menandai jadwal kerja lama untuk ditinjau ulang.
- Validasi: koordinat divalidasi pada rentang inklusif lintang $-11{,}0$ s/d $+6{,}0$ dan bujur $+95{,}0$ s/d $+141{,}0$. Data legacy yang berada pada rentang batas 94°–95° atau 141°–142° diaudit aman dengan penanda peninjauan tanpa penggagalan startup atau migrasi (§PRD-R07).

### F2 — Basis Data Bahan

> Sebagai calon pemilik rumah, saya ingin melihat daftar bahan yang tersedia untuk dinding dan atap beserta karakteristiknya agar saya tahu apa saja pilihannya sebelum membandingkan.

**Acceptance criteria**

- Setiap bahan memuat: nama, jenis komponen (dinding/atap), konduktivitas termal, indeks ketahanan lembap, indeks ketahanan korosi, usia pakai nominal (tahun), faktor emisi yang dinormalisasi wajib ke **$\text{kgCO}_2\text{e/m}^2$** untuk susunan/ketebalan tertentu (§PRD-R03).
- Bahan dapat difilter berdasarkan jenis komponen tanpa percampuran kategori.
- Sumber data setiap nilai dicatat bersama status perkiraan atau terverifikasi agar transparan, bukan disembunyikan sebagai angka pasti (§11.4, §PRD-R09). Jika data asal per $m^3$ atau per $kg$, konversi ke per $m^2$ wajib mencatat asumsi ketebalan atau massa jenis.
- Admin/pengembang dapat menambah bahan baru melalui basis data tanpa mengubah kode aplikasi.

### F3 — Analisis Iklim Lokasi

> Sebagai calon pemilik rumah, saya ingin sistem menganalisis iklim lokasi proyek saya secara otomatis agar saya tidak perlu mencari data cuaca sendiri.

**Acceptance criteria**

- Sistem mengambil data historis 10 tahun curah hujan tahunan, kelembapan rata-rata, suhu rata-rata dari API cuaca pihak ketiga (§8) berdasarkan koordinat proyek.
- Kedekatan pesisir dihitung dari koordinat (jarak ke garis pantai terdekat) sebagai proksi risiko korosi akibat udara bergaram (dengan batas efektif kalkulasi 50 km).
- Profil iklim disimpan per proyek (multiplisitas relasi $0..1$ sebelum data berhasil diambil) dan dipakai ulang sampai pengguna mengubah lokasi atau meminta pembaruan manual. Provenance iklim dicatat terpisah (`ApiHistoris`, `EstimasiBawaan`, `CacheHistoris`, `LegacyUnknown`) (§PRD-R09, §PRD-R10).
- Profil iklim ditampilkan dalam bentuk ringkas (bukan grafik mentah) sebelum masuk ke perbandingan bahan.

### F4 — Perbandingan Berperingkat

> Sebagai calon pemilik rumah, saya ingin melihat bahan-bahan diurutkan dari yang paling cocok, lengkap dengan skor kesesuaian dan jejak karbonnya, agar saya bisa membandingkan tanpa menghitung sendiri.

**Acceptance criteria**

- Untuk komponen terpilih, sistem menampilkan daftar bahan terurut berdasarkan skor kesesuaian iklim (§11.1) secara menurun. Jika skor identik, urutan kedua adalah emisi terendah, diikuti kestabilan nama/ID bahan.
- Setiap baris menampilkan skor kesesuaian (0–100) dan kgCO2e berdampingan, bukan pada tab terpisah.
- Setiap bahan disertai alasan penilaian dalam kalimat (contoh: "Ketahanan lembap rendah untuk curah hujan tahunan tinggi di lokasi Anda").
- Pada mode MVP, empat bobot kriteria bernilai tetap 0,25 masing-masing. Pada mode V2, pengguna dapat mengubah bobot kriteria (termal/lembap/korosi/usia pakai, tervalidasi jumlah 100% dan tombol reset) dan melihat peringkat berubah secara langsung tanpa memanggil ulang API iklim (§PRD-R01).
- Ambang rekomendasi label terpusat: skor $< 55$ ditandai visual sebagai **"Tidak disarankan"** (bahan tetap ditampilkan, tidak disembunyikan); skor $55 \le \text{skor} < 75$ berlabel **"Perlu pertimbangan"**; skor $\ge 75$ berlabel **"Cocok"** (§PRD-R10).

### F5 — Simulasi Emisi Jangka Panjang

> Sebagai calon pemilik rumah, saya ingin tahu berapa total emisi karbon suatu bahan sepanjang usia rumah saya, termasuk kalau bahan itu harus diganti lebih cepat karena tidak cocok dengan iklim saya.

**Acceptance criteria**

- Pengguna memilih horizon simulasi (minimal 20, 30, 50 tahun). Hasil simulasi berbagai horizon disimpan persisten tanpa saling menimpa (relasi `Evaluasi` ke `SimulasiEmisi` adalah `0..*`, §PRD-R10).
- Usia pakai efektif bahan disesuaikan turun dari usia pakai nominal berdasarkan skor kesesuaian iklim dengan parameter dasar penuaan $a = 0{,}5$ (§11.3, §PRD-R02).
- Sistem menghitung jumlah penggantian strictly pada interval kejadian $0 < t < H$:
  $$n = \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right)$$
  dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$. Penggantian tepat pada batas akhir horizon ($t = H$) tidak dihitung (§PRD-R02).
- Hasil ditampilkan sebagai perbandingan visual grafik bertingkat (*step chart*) antar bahan yang menampilkan kejadian interior dan tingkat akhir, dengan angka kgCO2e kumulatif sebagai sorotan utama.
- Antarmuka menyatakan terbuka bahwa usia pakai efektif adalah estimasi model, bukan jaminan (§11.4).

### F6 — Penjadwal Pekerjaan Sensitif Cuaca

> Sebagai calon pemilik rumah, saya ingin tahu tanggal mana yang cuacanya aman untuk pengecoran atau pengecatan, agar saya tidak perlu mengerjakan ulang karena cuaca salah.

**Acceptance criteria**

- Pengguna mendaftarkan pekerjaan terjadwal (contoh: "Pengecoran dinding", "Pengecatan atap") dengan rentang tanggal rencana, serta relasi opsional ke bahan/komponen terkait (§PRD-R05).
- Sistem mengambil prakiraan cuaca harian 14 hari untuk lokasi proyek dan mengevaluasi tiap hari terhadap ambang layak-kerja per jenis pekerjaan (§12), menggunakan kelembapan maksimum harian untuk pengecatan.
- Evaluasi harian mengembalikan status multi-nilai: `Layak`, `Waspada`, `TidakLayak`, dan `DataTidakTersedia`. Hari yang tidak layak ditandai jelas beserta alasannya (contoh: "Probabilitas hujan 70%, di atas ambang pengecoran").
- Pengguna dapat mengunci satu tanggal sebagai jadwal final setelah melihat evaluasi. Penguncian pada hari `TidakLayak` memerlukan dialog konfirmasi persetujuan risiko eksplisit; penguncian pada cache cuaca kedaluwarsa memerlukan konfirmasi khusus data lama.
- Jika prakiraan tidak tersedia (di luar rentang API >14 hari ke depan atau data cuaca null/parsial), sistem menyatakan itu secara eksplisit sebagai **"Data tidak tersedia"**, dan hari tersebut hanya dapat disimpan sebagai draft (tidak dapat dikunci final) (§PRD-R08).

### F7 — Ekspor Laporan Rekomendasi

> Sebagai calon pemilik rumah, saya ingin mengunduh ringkasan rekomendasi bahan dan jadwal dalam satu berkas agar bisa saya tunjukkan ke kontraktor.

**Acceptance criteria**

- Menghasilkan PDF berisi: profil proyek, profil iklim ringkas, peringkat bahan terpilih dengan skor dan kgCO2e, simulasi emisi horizon terpilih, dan jadwal pekerjaan yang telah dikunci.
- Laporan memuat tanggal pembuatan (timestamp UTC dan waktu lokal proyek) serta sumber data (API cuaca, basis data bahan) sebagai catatan kaki transparansi (§PRD-R09).
- Laporan disimpan sebagai berkas lokal, dicatat ke dalam riwayat laporan aplikasi, dan dapat dibuka ulang dari dalam aplikasi (termasuk saat luring) (§PRD-R06).

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
┌───────▼───────────────────────────┐   ┌───▼──────────────────────────┐
│ LAPISAN DATA                      │   │ KLIEN LAYANAN CUACA          │
│ EF Core + Npgsql                  │   │ Open-Meteo Client            │
│ PostgreSQL (lokal/remote)         │   │  ├─ IHistoricalClimateClient │
│  ├─ Snapshot Lokal Read-Only      │   │  └─ IDailyForecastClient     │
│  └─ Kompatibilitas JSON           │   │  └─ Cache TTL 6 Jam UTC      │
└───────────────────────────────────┘   └──────────────────────────────┘
```

Aplikasi dirancang sebagai desktop *single-user* tanpa login. Koneksi database ke PostgreSQL (lokal atau cloud Supabase) dikelola oleh EF Core dengan mekanisme snapshot lokal atomik read-only untuk peninjauan saat luring (§PRD-R06).

---

## 8. Integrasi Layanan Cuaca Pihak Ketiga

### 8.1 Dua kebutuhan data yang berbeda

| Karakteristik | Analisis Iklim Lokasi (F3) | Prakiraan Cuaca Harian (F6) |
|---|---|---|
| Rentang waktu | Rata-rata/agregat bertahun-tahun (10 tahun) | Harian, jangka pendek (14 hari) |
| Frekuensi pengambilan | Sekali per proyek, diperbarui atas permintaan pengguna | Setiap kali penjadwal dibuka, dengan cache TTL 6 jam |

### 8.2 Rekomendasi penyedia — Open-Meteo

Rekomendasi penyedia cuaca adalah **Open-Meteo**, dengan alasan:
- Menyediakan **kedua** kebutuhan di atas dari satu penyedia: *Historical Weather API* (arsip harian 10 tahun, diagregasi menjadi rata-rata tahunan) dan *Forecast API* (prakiraan harian hingga 14–16 hari).
- Tidak memerlukan API key untuk kuota non-komersial/akademik.
- Cakupan global termasuk wilayah Indonesia dengan resolusi grid yang memadai untuk rumah tinggal.

### 8.3 Kontrak internal `WeatherApiClient`

Lapisan domain (`ClimateAnalysisService`, `WorkSchedulingService`) bergantung pada dua kontrak abstraksi:

```csharp
public interface IHistoricalClimateClient {
    Task<ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default);
}

public interface IDailyForecastClient {
    Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days, CancellationToken ct = default);
}
```

`ProfilIklim` memuat curah hujan tahunan, kelembapan rata-rata, suhu rata-rata, jarak pesisir, sumber, dan waktu pengambilan. `DailyForecast` memuat tanggal, probabilitas hujan, curah hujan perkiraan, suhu min/maks, kelembapan. Perluasan target untuk membawa wrapper metadata terstruktur (provider, zona waktu, kualitas data, fetched_at UTC) dapat ditambahkan sebagai implementasi konkret tanpa memecahkan kontrak ini (§PRD-R08).

### 8.4 Perilaku luring dan kegagalan

- Profil iklim historis (§F3) disimpan permanen di basis data setelah diambil sekali — pengguna tetap bisa melihat perbandingan bahan tanpa koneksi internet selama tidak meminta pembaruan.
- Prakiraan harian (§F6) di-cache lokal dengan masa berlaku default **6 jam** dihitung dari timestamp UTC `fetched_at` (§PRD-R08). Permintaan dalam rentang umur cache memakai cache, bukan memanggil API ulang. Tanggal prakiraan ditafsirkan menurut zona waktu lokasi proyek dari respons penyedia API.
- Kegagalan panggilan API ditangani dengan pesan eksplisit dan fallback ke data cache terakhir berpenanda "Data lama". Jika cache tidak ada, hari diberi status `DataTidakTersedia`. Aplikasi tidak boleh membeku menunggu respons tanpa batas waktu.

---

## 9. Skema Data

PostgreSQL. Penamaan tabel fisik pada database menggunakan Bahasa Indonesia, dipetakan secara terstruktur dengan entitas domain (§PRD-R10).

```sql
-- Tabel Proyek Bangunan (menggantikan projects)
CREATE TABLE bangunan (
  id                uuid PRIMARY KEY,
  nama              text NOT NULL,
  kota              text NOT NULL,
  latitude          double precision NOT NULL CHECK (latitude BETWEEN -11.0 AND 6.0),
  longitude         double precision NOT NULL CHECK (longitude BETWEEN 95.0 AND 141.0),
  created_at        timestamptz NOT NULL DEFAULT now()
);

-- Tabel Profil Iklim Lokasi
CREATE TABLE profil_iklim (
  bangunan_id           uuid PRIMARY KEY REFERENCES bangunan(id) ON DELETE CASCADE,
  curah_hujan_tahunan   real NOT NULL CHECK (curah_hujan_tahunan >= 0),
  kelembapan_rata       real NOT NULL CHECK (kelembapan_rata BETWEEN 0 AND 100),
  suhu_rata             real NOT NULL,
  jarak_pesisir_km      real NOT NULL CHECK (jarak_pesisir_km >= 0),
  tingkat_korosi        text NOT NULL,      -- 'rendah' | 'sedang' | 'tinggi'
  sumber                text NOT NULL,
  provenance            text NOT NULL DEFAULT 'ApiHistoris',
  diambil_pada_utc      timestamptz NOT NULL
);

-- Tabel Komponen Proyek
CREATE TABLE komponen (
  id                uuid PRIMARY KEY,
  bangunan_id       uuid NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
  jenis             text NOT NULL CHECK (jenis IN ('dinding', 'atap')),
  luas_m2           real NOT NULL CHECK (luas_m2 > 0),
  UNIQUE (bangunan_id, jenis)
);

-- Tabel Basis Data Bahan
CREATE TABLE bahan (
  id                    uuid PRIMARY KEY,
  nama                  text NOT NULL,
  jenis                 text NOT NULL CHECK (jenis IN ('dinding', 'atap')),
  konduktivitas_termal  real NOT NULL CHECK (konduktivitas_termal > 0),
  ketahanan_lembap      smallint NOT NULL CHECK (ketahanan_lembap BETWEEN 1 AND 5),
  ketahanan_korosi      smallint NOT NULL CHECK (ketahanan_korosi BETWEEN 1 AND 5),
  usia_nominal_tahun    real NOT NULL CHECK (usia_nominal_tahun > 0),
  faktor_emisi_m2       real NOT NULL CHECK (faktor_emisi_m2 >= 0), -- kgCO2e per m2
  susunan_ketebalan     text,
  sumber                text NOT NULL,
  status_verifikasi     text NOT NULL DEFAULT 'perkiraan' -- 'perkiraan' | 'terverifikasi'
);

-- Tabel Evaluasi Kesesuaian
CREATE TABLE evaluasi (
  id                    uuid PRIMARY KEY,
  komponen_id          uuid NOT NULL REFERENCES komponen(id) ON DELETE CASCADE,
  bahan_id              uuid NOT NULL REFERENCES bahan(id) ON DELETE RESTRICT,
  skor_kesesuaian       real NOT NULL CHECK (skor_kesesuaian BETWEEN 0 AND 100),
  emisi_awal_kg         real NOT NULL CHECK (emisi_awal_kg >= 0),
  alasan                text NOT NULL,
  bobot_termal          real NOT NULL DEFAULT 0.25,
  bobot_lembap          real NOT NULL DEFAULT 0.25,
  bobot_korosi          real NOT NULL DEFAULT 0.25,
  bobot_usia            real NOT NULL DEFAULT 0.25,
  dihitung_pada_utc     timestamptz NOT NULL DEFAULT now()
);

-- Tabel Simulasi Emisi Jangka Panjang (relasi Evaluasi ke SimulasiEmisi: 0..*)
CREATE TABLE simulasi_emisi (
  id                    uuid PRIMARY KEY,
  evaluasi_id           uuid NOT NULL REFERENCES evaluasi(id) ON DELETE CASCADE,
  horizon_tahun         smallint NOT NULL CHECK (horizon_tahun IN (20, 30, 50)),
  usia_efektif_tahun    real NOT NULL CHECK (usia_efektif_tahun > 0),
  jumlah_penggantian    smallint NOT NULL CHECK (jumlah_penggantian >= 0),
  emisi_kumulatif_kg    real NOT NULL CHECK (emisi_kumulatif_kg >= 0),
  versi_model           text NOT NULL DEFAULT 'v2.0',
  UNIQUE (evaluasi_id, horizon_tahun, versi_model)
);

-- Tabel Pekerjaan Konstruksi Terjadwal
CREATE TABLE pekerjaan (
  id                uuid PRIMARY KEY,
  bangunan_id       uuid NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
  nama              text NOT NULL,
  jenis             text NOT NULL CHECK (jenis IN ('pengecoran', 'pengecatan')),
  tanggal_mulai     date NOT NULL,
  tanggal_selesai   date NOT NULL CHECK (tanggal_selesai >= tanggal_mulai),
  tanggal_dikunci   date,
  status            text NOT NULL DEFAULT 'draft', -- 'draft' | 'terkunci' | 'selesai'
  persetujuan_risiko boolean NOT NULL DEFAULT false,
  komponen_id       uuid REFERENCES komponen(id) ON DELETE SET NULL,
  bahan_id          uuid REFERENCES bahan(id) ON DELETE SET NULL
);

-- Tabel Cache Prakiraan Cuaca Harian
CREATE TABLE prakiraan_cache (
  id                    uuid PRIMARY KEY,
  bangunan_id           uuid NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
  tanggal_cuaca         date NOT NULL,
  probabilitas_hujan    real,
  curah_hujan_mm        real,
  suhu_min_c            real,
  suhu_maks_c           real,
  kelembapan_maks_pct   real,
  zona_waktu            text NOT NULL,
  diambil_pada_utc      timestamptz NOT NULL,
  UNIQUE (bangunan_id, tanggal_cuaca)
);

-- Tabel Riwayat Laporan Ekspor
CREATE TABLE laporan (
  id                uuid PRIMARY KEY,
  bangunan_id       uuid NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
  path_berkas       text NOT NULL,
  format            text NOT NULL DEFAULT 'PDF',
  dibuat_pada_utc   timestamptz NOT NULL DEFAULT now()
);
```

**Catatan implementasi:**
- Seluruh identitas entitas menggunakan tipe `Guid` konsisten (§PRD-R10).
- `bahan.sumber` dan status verifikasi wajib diisi untuk transparansi data (§PRD-R09).
- Bounding box bujur 95–141 dan lintang -11–6 diterapkan sebagai constraint SQL dan validasi domain (§PRD-R07).

---

## 10. Spesifikasi Modul dan Kontrak Layanan

Kontrak antarmuka internal antar lapisan yang dipakai ViewModel dan Domain Services:

| Layanan | Metode Kontrak Aktual | Tanggung Jawab Operasional |
|---|---|---|
| `IMaterialService` | `ListByKindAsync(jenis)`, `GetByIdAsync(id)` | Sumber untuk katalog bahan dinding/atap (§F2) |
| `IProjectService` | `CreateProjectAsync`, `UpdateProjectAsync`, `UpdateLocationAsync`, `AddComponentAsync`, `DeleteAsync`, `ListAsync` | Manajemen proyek dan komponen; perubahan lokasi memicu refresh iklim (§F1) |
| `IClimateAnalysisService` | `GetOrFetchProfileAsync(proyekId)`, `RefreshProfileAsync(proyekId)` | Mengembalikan profil iklim tersimpan atau mengambil baru via `IHistoricalClimateClient` (§F3, §8.3) |
| `ISuitabilityScoringService` | `RankAsync(komponenId, bobot)` | Menghasilkan daftar `Evaluasi` terurut berdasarkan 4 sub-skor (§F4, §11.1) |
| `IEmissionSimulationService` | `SimulateAsync(evaluasiId, horizonTahun)` | Menghasilkan `SimulasiEmisi` dengan rumus penggantian $0 < t < H$ (§F5, §11.3) |
| `IWorkSchedulingService` | `EvaluateDaysAsync(pekerjaanId)`, `LockDateAsync(pekerjaanId, tanggal)` | Mengevaluasi kelayakan tiap hari via `IDailyForecastClient` dan mengunci jadwal (§F6, §12) |
| `IReportExportService` | `ExportAsync(proyekId, opsi)` | Menghasilkan PDF rekomendasi dan mencatat riwayat laporan (§F7) |

---

## 11. Metode Penilaian Kesesuaian Iklim dan Emisi Karbon

### 11.1 Skor kesesuaian

Skor kesesuaian per bahan dihitung sebagai jumlah berbobot dari empat sub-skor (skala 0–100):

$$\text{skor\_kesesuaian} = w_{\text{termal}} \cdot s_{\text{termal}} + w_{\text{lembap}} \cdot s_{\text{lembap}} + w_{\text{korosi}} \cdot s_{\text{korosi}} + w_{\text{usia}} \cdot s_{\text{usia}}$$

dengan syarat $w_{\text{termal}} + w_{\text{lembap}} + w_{\text{korosi}} + w_{\text{usia}} = 1{,}0$. Pada MVP, bobot bernilai tepat $0{,}25$ masing-masing (§PRD-R01).

| Sub-skor | Dihitung dari | Model Perhitungan & Perilaku Monoton |
|---|---|---|
| `skor_termal` | Konduktivitas termal bahan ($\lambda$) relatif terhadap suhu rata-rata lokasi | Pita bawah 0,10 W/m·K. Batas atas $\lambda_{\max}$ bergeser dari 0,60 (suhu $\le 26^\circ\text{C}$) ke 0,30 (suhu $\ge 32^\circ\text{C}$). Skor $= \operatorname{clamp}(100 \cdot \lambda_{\max}/\lambda, 0, 100)$ jika $\lambda > \lambda_{\max}$, dan 100 jika dalam pita. |
| `skor_lembap` | Ketahanan lembap (1–5) relatif terhadap curah hujan tahunan dan kelembapan rata-rata | Rumus terkoreksi (§PRD-R04): $r = \operatorname{clamp}(\text{hujan}/3000, 0, 1)$, $h = \operatorname{clamp}(\text{RH}/100, 0, 1)$, $\text{beban} = 0{,}5 \cdot r + 0{,}5 \cdot h$, $\text{kebutuhan} = 1 + 4 \cdot \text{beban}$, skor $= \operatorname{clamp}(100 \cdot \text{ketahanan}/\text{kebutuhan}, 0, 100)$. |
| `skor_korosi` | Ketahanan korosi (1–5) relatif terhadap jarak pesisir | Jarak efektif $d = \min(\text{jarakKm}, 50)$. Kebutuhan $= 1 + 4 \cdot (1 - d/50)$. Skor $= \operatorname{clamp}(100 \cdot \text{ketahanan}/\text{kebutuhan}, 0, 100)$. Jarak $\ge 50\text{ km}$ memberi skor 100 untuk semua bahan. |
| `skor_usia` | Usia pakai nominal dinormalisasi relatif terhadap seluruh bahan di kategori yang sama | Normalisasi min-maks seluruh katalog kategori: $100 \cdot (\text{usia} - \min)/(\max - \min)$. Pencarian atau perbandingan dua bahan tidak mengubah nilai referensi. |

*Ambang Batas Rekomendasi Terpusat (§PRD-R10):*
- Skor $< 55{,}0$: **Tidak disarankan** (ditandai visual, tetap ditampilkan).
- $55{,}0 \le \text{Skor} < 75{,}0$: **Perlu pertimbangan**.
- Skor $\ge 75{,}0$: **Cocok**.

### 11.2 kgCO2e awal

$$\text{emisi\_awal} = \text{faktor\_emisi} (\text{kgCO}_2\text{e/m}^2) \times \text{luas\_komponen} (\text{m}^2)$$

Faktor emisi yang masuk perhitungan wajib ternormalisasi ke $\text{kgCO}_2\text{e/m}^2$ untuk susunan/ketebalan tertentu (§PRD-R03). Ditampilkan berdampingan dengan skor kesesuaian pada peringkat (§F4).

### 11.3 Simulasi emisi jangka panjang — pengganda penggantian dini

Usia pakai efektif diturunkan dari usia pakai nominal menggunakan fungsi penyesuaian:

$$L = \text{usia\_nominal} \times \left(a + (1 - a) \cdot \frac{\text{skor\_kesesuaian}}{100}\right), \quad \text{default } a = 0{,}5$$

Jumlah penggantian dalam horizon simulasi dihitung strictly pada interval kejadian $0 < t < H$:

$$n = \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right)$$

dan total emisi kumulatif dihitung dengan:

$$\text{emisi\_kumulatif} = \text{emisi\_awal} \times (1 + n)$$

Penggantian tepat pada batas akhir horizon ($t = H$) tidak dihitung (§PRD-R02).

**Tabel Pemeriksaan Kewajaran (Horizon $H = 30$ tahun, $a = 0{,}5$):**
- Pada skor kesesuaian 100 ($L = 15\text{ tahun}$): terjadi **1 penggantian** pada tahun 15 (total emisi $\times 2$).
- Pada skor kesesuaian 20 ($L = 9\text{ tahun}$): terjadi **3 penggantian** pada tahun 9, 18, dan 27 (total emisi $\times 4$).
- Pada skor kesesuaian 100, horizon 15 tahun: terjadi **0 penggantian** (total emisi $\times 1$).
- Pada skor kesesuaian 100, horizon 31 tahun: terjadi **2 penggantian** pada tahun 15 dan 30 (total emisi $\times 3$).

### 11.4 Batasan yang harus dinyatakan di antarmuka

> Nilai properti bahan dan faktor emisi bersumber dari basis data publik internasional (mis. ICE Database, EPD generik), yang menjadi estimasi awal untuk pasar Indonesia. Fungsi penyesuaian usia pakai efektif (§11.3) adalah model matematis sederhana, bukan hasil studi lapangan. Seluruh parameter asumsi perhitungan berversi dan dapat dilihat pada panel antarmuka serta di-reset ke nilai default (§PRD-R11).

---

## 12. Penjadwal Pekerjaan Sensitif Cuaca — Logika Keputusan

### 12.1 Ambang layak-kerja cuaca (Asumsi generik)

Setiap jenis pekerjaan dievaluasi terhadap prakiraan cuaca harian 14 hari dengan ambang batas asumsi generik awal:

| `work_type` | Parameter Cuaca | Ambang Tidak Layak | Status Waspada | Dasar Pertimbangan Teknis |
|---|---|---|---|---|
| `pengecoran` | Probabilitas hujan | $> 60\%$ | — | Hujan saat curing awal beton mengganggu proses hidrasi semen dan merusak permukaan |
| `pengecoran` | Curah hujan | $> 5{,}0\text{ mm}$ | — | Akumulasi air berlebih merusak rasio air-semen campuran beton |
| `pengecoran` | Suhu maksimum | — | $> 35{,}0^\circ\text{C}$ | Suhu tinggi mempercepat penguapan air campuran, berisiko retak susut (peringatan mitigasi, bukan larangan keras) |
| `pengecatan` | Kelembapan maksimum | $> 85\%$ | — | Kelembapan aplikasi di atas batas menghalangi pengeringan sempurna |
| `pengecatan` | Probabilitas hujan | $> 40\%$ | — | Cat yang belum kering tersapu hujan, memicu pengerjaan ulang |
| `pengecatan` | Suhu udara | Di luar $10{,}0 - 35{,}0^\circ\text{C}$ | — | Rentang aplikasi umum cat; di luar rentang ini daya rekat menurun |

*Batas Tepat:* Nilai cuaca yang tepat berada pada batas (misalnya probabilitas 60% atau curah hujan 5,0 mm pada pengecoran; probabilitas 40%, kelembapan 85%, suhu 10°C atau 35°C pada pengecatan) **tidak melanggar** (masih berstatus `Layak`). Nilai di atasnya baru dinyatakan `TidakLayak`. Status `Waspada` secara default hanya muncul pada suhu pengecoran $> 35^\circ\text{C}$ saat tidak ada pelanggaran keras.

*Hierarki Ambang:* Override Pekerjaan $\rightarrow$ Spesifikasi Produk Bahan Terpilih $\rightarrow$ Default Asumsi Generik (§PRD-R05).

### 12.2 Aturan Penguncian Jadwal dan Penanganan Data

- **Status Multi-Nilai:** Evaluasi harian menghasilkan status `Layak`, `Waspada`, `TidakLayak`, atau `DataTidakTersedia`.
- **Penguncian Hari Tidak Layak:** Pengguna dapat mengunci tanggal yang ditandai tidak layak hanya setelah menyetujui dialog konfirmasi risiko eksplisit (§PRD-R10).
- **Penguncian Cache Kedaluwarsa:** Penguncian pada data cuaca yang berumur $\ge 6\text{ jam}$ memerlukan konfirmasi khusus data lama (§PRD-R08).
- **Data Tidak Tersedia:** Tanggal di luar jangkauan prakiraan (>14 hari ke depan) atau hari dengan data null/parsial hanya dapat disimpan sebagai **draft**, tidak dapat dikunci final.
- **Perubahan Lokasi:** Saat koordinat proyek diubah, keputusan jadwal yang lama tidak dihapus otomatis, melainkan diberi status **"Perlu ditinjau"** (*pending review*).

---

## 13. Analisis Kompetitor

### 13.1 One Click LCA dan Embodied Carbon in Construction Calculator — kompetitor tidak langsung
Alat analisis siklus hidup bangunan komersial berbasis web untuk pelaporan formal proyek besar. Basis material didominasi Eropa/Amerika Utara dan menuntut keahlian LCA mendalam.

### 13.2 EnergyPlus — kompetitor tidak langsung
Simulasi performa termal bangunan mendalam. Tidak merekomendasikan bahan dan tidak menghitung jejak karbon tersemat maupun jadwal kerja.

### 13.3 Aplikasi RAB/volume material lokal — kompetitor tidak langsung
Berfokus pada estimasi biaya dan volume bahan, tanpa analisis iklim maupun pertimbangan karbon.

### 13.4 Status quo — mengikuti saran tukang
Kebiasaan mengikuti saran tukang tanpa verifikasi independen. Laporan rekomendasi Wastu (§F7) dirancang agar mudah dibawa dan didiskusikan dengan tukang/kontraktor.

---

## 14. Risiko dan Mitigasi

| # | Risiko | Dampak | Mitigasi |
|---|---|---|---|
| **R1** | Basis data bahan dan faktor emisi bersumber dari data publik internasional, belum representatif pasar Indonesia | Rekomendasi dituding tidak relevan secara lokal | Sumber data dicatat per bahan (`data_source`) dan dinyatakan sebagai batasan di antarmuka (§11.4); kurasi lokal masuk pekerjaan lanjutan |
| **R2** | Fungsi penyesuaian usia pakai efektif (§11.3) adalah model sederhana tanpa validasi lapangan | Simulasi emisi jangka panjang bisa menyesatkan jika dianggap presisi | Konstanta fungsi dinyatakan sebagai asumsi yang dapat dikonfigurasi; hasil disajikan sebagai estimasi, bukan angka pasti |
| **R3** | API cuaca pihak ketiga berubah format, membatasi kuota, atau tidak tersedia | Fitur analisis iklim (F3) dan penjadwal (F6) berhenti berfungsi | `WeatherApiClient` diabstraksi lewat `IHistoricalClimateClient`/`IDailyForecastClient` (§8.3) sehingga penyedia dapat diganti tanpa mengubah domain; cache lokal menjaga fungsi dasar tetap berjalan (§8.4) |
| **R4** | Ambang layak-kerja cuaca (§12) bersifat generik, tidak spesifik merek bahan | Rekomendasi jadwal bisa terlalu konservatif atau longgar untuk produk tertentu | Ambang dinyatakan dapat dikonfigurasi per jenis pekerjaan, bukan konstanta tersembunyi |
| **R5** | Kedekatan pesisir dihitung sebagai proksi jarak geometris ke garis pantai, bukan pengukuran salinitas udara aktual | Skor ketahanan korosi bisa meleset di lokasi dengan mikroklimat khusus | Dinyatakan sebagai proksi, bukan pengukuran langsung, di alasan penilaian (§F4) |
| **R6** | Instans PostgreSQL lokal per pengguna menyulitkan berbagi data lintas perangkat | Pengguna yang berganti perangkat kehilangan riwayat proyek | Dinyatakan eksplisit sebagai non-scope MVP (§5.2); mode remote dilengkapi snapshot lokal read-only untuk peninjauan luring (§PRD-R06) |
| **R7** | Pengguna awam mengunci jadwal pada hari yang ditandai "tidak layak" tanpa memahami konsekuensinya | Pengerjaan ulang tetap terjadi meski sudah diperingatkan | Peringatan disertai alasan tekstual yang jelas (§12); penguncian diizinkan setelah konfirmasi persetujuan risiko eksplisit, bukan diblokir paksa |

---

## 15. Rencana Rilis Bertahap

### MVP — alur inti berjalan ujung ke ujung
Tujuan: dari profil proyek sampai peringkat bahan tampil, dengan data iklim nyata.
- Skema basis data relasional PostgreSQL dan snapshot lokal luring (§9, §PRD-R06).
- Seed basis data bahan dinding dan atap (≥ 15 dan ≥ 10 entri, §4.1) dengan faktor emisi $\text{kgCO}_2\text{e/m}^2$ (§PRD-R03).
- Implementasi `IHistoricalClimateClient` terhadap Open-Meteo (§8).
- F1 — profil proyek dan komponen (dinding saja, atap saja, atau keduanya) (§PRD-R10).
- F3 — analisis iklim lokasi dan jarak pesisir.
- F2 — katalog bahan dan penapisan kategori.
- F4 — perbandingan berperingkat dengan skor kesesuaian dan kgCO2e berdampingan, **bobot tetap 0,25** (§PRD-R01).

### V1 — konsekuensi jangka panjang dan pelaksanaan
Tujuan: menjawab "kalau saya salah pilih, apa akibatnya" dan "kapan sebaiknya saya kerjakan".
- F5 — simulasi emisi jangka panjang dengan fungsi penggantian $0 < t < H$ (§11.3, §PRD-R02).
- Implementasi `IDailyForecastClient` terhadap Open-Meteo, cache prakiraan TTL 6 jam UTC (§8.4, §PRD-R08).
- F6 — penjadwal pekerjaan sensitif cuaca dengan status multi-nilai dan dialog konfirmasi risiko (§12).

### V2 — pelaporan dan penghalusan
Tujuan: hasil aplikasi bisa dibawa keluar aplikasi dan pengalaman pengguna dihaluskan.
- F7 — ekspor laporan PDF dan peninjauan riwayat lokal luring (§PRD-R06).
- Pembobotan kriteria kustom yang dapat diubah pengguna pada F4 dengan validasi jumlah 100% dan reset (§PRD-R01).
- Panel peninjauan asumsi perhitungan dan reset parameter model (§PRD-R11).
- Uji pengguna kualitatif dengan persona non-teknis Bu Wulan dan Pak Herman (§4.1, §PRD-R11).
- Penghalusan alasan tekstual (`reasoning`) berdasarkan umpan balik uji pengguna.

### Pasca-MVP — didokumentasikan, tidak dibangun
Kurasi faktor emisi dan properti bahan khusus pasar Indonesia; kalibrasi fungsi penyesuaian usia pakai efektif terhadap data lapangan; komponen selain dinding/atap; sinkronisasi multi-perangkat; integrasi pemesanan bahan ke pemasok; aplikasi mobile pendamping.

---

## 16. Lampiran

### 16.1 Peran tim

| Nama | NIM | Peran | Tanggung jawab utama di PRD ini |
|---|---|---|---|
| Dien Muhammad Scientivan Kurniapramono | 24/533571/TK/59114 | Software Architect | Arsitektur sistem (§7), integrasi cuaca (§8), skema data (§9), kontrak modul (§10) |
| Ramzi Alfito Rizky | 24/540550/TK/60008 | Backend Developer | Lapisan layanan dan domain (§10, §11), simulasi emisi (§11.3) |
| Yohanes Anthony Saputra | 24/536327/TK/59524 | Frontend Developer | Antarmuka WPF dan pengalaman pengguna (§6), penjadwal pekerjaan (§12) |

Metode penilaian kesesuaian dan simulasi emisi (§11) serta logika penjadwalan (§12) dikerjakan bersama, karena keduanya menjadi titik temu antara data bahan, data iklim, dan pengalaman pengguna akhir.

### 16.2 Rujukan awal

1. Open-Meteo — Historical Weather API & Forecast API. https://open-meteo.com/
2. BMKG Open Data (alternatif/pelengkap prakiraan). https://data.bmkg.go.id/
3. Inventory of Carbon and Energy (ICE) Database, University of Bath — faktor emisi tersemat material konstruksi umum.
4. Environmental Product Declaration (EPD) generik untuk kategori bahan dinding dan atap yang relevan.
5. One Click LCA — https://www.oneclicklca.com/
6. Embodied Carbon in Construction Calculator (EC3) — https://www.buildingtransparency.org/
7. EnergyPlus — https://energyplus.net/

### 16.3 Keterkaitan dengan diagram UML

Entitas dan alur pada dokumen ini konsisten dengan [`docs/uml-wastu.md`](uml-wastu.md) dan sumber visual [`docs/uml-wastu.drawio`](uml-wastu.drawio):

| Aspek | Diagram UML Awal | Penyelarasan Normatif Target | Alasan Penyelarasan |
|---|---|---|---|
| `Pengguna.login` | Terdapat metode login di UML historis | Dihapus dari use case, activity, dan class diagram | Wastu adalah aplikasi desktop single-user tanpa kebutuhan autentikasi (§PRD-R10) |
| Multiplisitas `Proyek` ke `ProfilIklim` | `1` ke `1` kaku | Diubah menjadi `1` ke `0..1` | Profil iklim bernilai null sebelum berhasil diambil dari API (§PRD-R10) |
| Multiplisitas `Proyek` ke `Komponen` | `1` ke `*` | Diperjelas menjadi `1` ke `1..*` | Proyek minimal memiliki 1 komponen aktif (dinding saja, atap saja, atau keduanya) (§PRD-R10) |
| Multiplisitas `Evaluasi` ke `SimulasiEmisi` | `1` ke `0..1` | Diubah menjadi `1` ke `0..*` | Evaluasi dapat belum disimulasikan (`0`) atau menyimpan banyak hasil simulasi horizon 20, 30, 50 tahun (`*`) (§PRD-R10) |
| Aktor Layanan Cuaca | Hanya terhubung ke Ambil Prakiraan Cuaca | Terhubung ke Analisis Iklim Lokasi dan Ambil Prakiraan Cuaca | Open-Meteo melayani data iklim historis 10 tahun dan prakiraan harian 14 hari (§PRD-R10) |
| Alur Ubah Bobot pada Activity Diagram | Mengarah kembali ke pembuatan proyek | Mengarah kembali langsung ke Hitung Skor Kesesuaian | Mengubah kriteria/bobot tidak membuat proyek baru, melainkan menghitung ulang skor (§PRD-R10) |
| Alur Kelayakan Cuaca pada Activity Diagram | Hanya Ya/Tidak | Menangani `Data tidak tersedia` dan alur persetujuan risiko untuk penguncian hari `Tidak layak` | Mencerminkan status multi-nilai dan dialog konfirmasi risiko pada jadwal (§PRD-R10) |

---

## 17. Matriks Seluruh Acceptance Criteria dan Penelusuran Kebutuhan

Matriks berikut mencatat seluruh **31 Acceptance Criteria F1–F7**, **17 Ketentuan Lintas Fitur X01–X17**, dan **40 Skenario Pengujian T01–T40** yang mengikat pelaksanaan paket implementasi WP01 sampai WP08.

### 17.1 F1 — Profil Proyek

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F1-01** | Pengguna dapat membuat, melihat, mengubah, dan menghapus nama proyek, kota, dan koordinat. Koordinat dapat dipilih otomatis dari kota atau diisi manual. | WP02 | T01, T02 | Terpetakan |
| **F1-02** | Proyek memiliki satu atau lebih komponen dinding/atap, masing-masing dengan luas positif ($>0$). Penolakan terhadap 0 komponen atau luas invalid. | WP02 | T01, T02 | Terpetakan |
| **F1-03** | Perubahan koordinat lokasi proyek memicu pengambilan ulang profil iklim, menginvalidasi cache lama, dan menandai jadwal untuk ditinjau. | WP03 | T07, T08, T31 | Terpetakan |
| **F1-04** | Validasi koordinat pada rentang inklusif lintang $[-11, 6]$ dan bujur $[95, 141]$. Penanganan data legacy aman tanpa crash migrasi. | WP02 | T03, T37 | Terpetakan |

### 17.2 F2 — Basis Data Bahan

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F2-01** | Properti nama, jenis, termal, ketahanan lembap/korosi, usia nominal, dan faktor emisi ($\text{kgCO}_2\text{e/m}^2$) tersedia lengkap dan valid. | WP02 | T15, T39 | Terpetakan |
| **F2-02** | Filter katalog berdasarkan jenis komponen (`dinding` atau `atap`) tanpa percampuran kategori. | WP02 | T14, T39 | Terpetakan |
| **F2-03** | Sumber rujukan dan status verifikasi (perkiraan/terverifikasi) tercatat transparan per nilai; konversi satuan dicatat. | WP02 | T39 | Terpetakan |
| **F2-04** | Administrator/pengembang dapat menambah bahan baru melalui database tanpa kompilasi ulang kode. | WP02 | T39 | Terpetakan |

### 17.3 F3 — Analisis Iklim Lokasi

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F3-01** | Pengambilan data iklim historis 10 tahun (curah hujan, kelembapan rata-rata, suhu rata-rata) dari API berdasarkan koordinat. | WP03 | T04, T09 | Terpetakan |
| **F3-02** | Kedekatan pesisir dihitung secara geometris terhadap garis pantai sebagai proksi risiko korosi (batas efektif 50 km). | WP03 | T10, T13 | Terpetakan |
| **F3-03** | Profil iklim disimpan per proyek (multiplisitas 0..1) dan dipakai ulang luring tanpa memanggil API kembali. | WP01, WP03 | T04, T06, T36 | Terpetakan |
| **F3-04** | Ringkasan profil iklim dan status kualitas data ditampilkan pada antarmuka sebelum perbandingan bahan. | WP03 | T04, T05 | Terpetakan |

### 17.4 F4 — Perbandingan Berperingkat

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F4-01** | Daftar bahan terurut menurun berdasarkan skor kesesuaian iklim (skor turun, emisi naik, tie-break stabil). | WP04 | T11, T12, T13 | Terpetakan |
| **F4-02** | Skor kesesuaian (0–100) dan emisi awal ($\text{kgCO}_2\text{e}$) ditampilkan berdampingan di setiap baris perbandingan. | WP04 | T15 | Terpetakan |
| **F4-03** | Setiap bahan menyertakan kalimat alasan penilaian yang mudah dipahami persona non-teknis. | WP04 | T11, T12 | Terpetakan |
| **F4-04** | Rilis MVP mengunci empat bobot kriteria pada nilai tetap 0,25 masing-masing. | WP04 | T16 | Terpetakan |
| **F4-05** | Rilis V2 menyediakan pengaturan bobot kustom tervalidasi ($\sum = 100\%$) dan reset, langsung merangking tanpa request iklim ulang. | WP04 | T16 | Terpetakan |
| **F4-06** | Ambang label rekomendasi konsisten: $< 55$ "Tidak disarankan", $55..74,999$ "Perlu pertimbangan", $\ge 75$ "Cocok". | WP04 | T17 | Terpetakan |

### 17.5 F5 — Simulasi Emisi Jangka Panjang

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F5-01** | Pengguna dapat memilih horizon simulasi (20, 30, 50 tahun); relasi 0..* menyimpan hasil per horizon tanpa menimpa. | WP05 | T20 | Terpetakan |
| **F5-02** | Usia efektif $L$ dihitung dari usia nominal dan skor kesesuaian dengan parameter dasar penuaan $a = 0{,}5$. | WP05 | T18 | Terpetakan |
| **F5-03** | Jumlah penggantian dihitung strictly pada $0 < t < H$: $n = \max(0, \lceil H/L \rceil - 1)$ dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$. | WP05 | T18, T19 | Terpetakan |
| **F5-04** | Perbandingan visual disajikan dalam step chart interaktif yang menampilkan titik interior dan level akhir. | WP05 | T19 | Terpetakan |
| **F5-05** | Antarmuka menyatakan secara terbuka bahwa usia pakai efektif adalah estimasi model perhitungan. | WP05 | T18 | Terpetakan |

### 17.6 F6 — Penjadwal Pekerjaan Sensitif Cuaca

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F6-01** | Pendaftaran pekerjaan dengan jenis (pengecoran/pengecatan), rentang tanggal, dan relasi bahan/komponen opsional. | WP06 | T29 | Terpetakan |
| **F6-02** | Evaluasi prakiraan cuaca 14 hari per hari terhadap ambang teknis pekerjaan dengan kelembapan maksimum harian untuk cat. | WP06 | T24, T25, T26 | Terpetakan |
| **F6-03** | Status kelayakan multi-nilai: `Layak`, `Waspada`, `TidakLayak` (dengan alasan pelanggaran ambang), dan `DataTidakTersedia`. | WP06 | T24, T25, T27 | Terpetakan |
| **F6-04** | Pengguna dapat mengunci jadwal final; penguncian hari `TidakLayak` memerlukan konfirmasi persetujuan risiko eksplisit. | WP06 | T30 | Terpetakan |
| **F6-05** | Tanggal tanpa prakiraan (>14 hari) atau cuaca null/parsial diberi penjelasan eksplisit `Data tidak tersedia` (hanya draft). | WP06 | T27, T29 | Terpetakan |

### 17.7 F7 — Ekspor Laporan Rekomendasi

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F7-01** | Berkas PDF memuat profil proyek, profil iklim, bahan terpilih+skor+emisi, simulasi emisi horizon, dan jadwal terkunci. | WP07 | T32, T33 | Terpetakan |
| **F7-02** | Laporan memuat tanggal pembuatan (UTC dan waktu lokal proyek) serta atribusi sumber data transparansi. | WP07 | T33 | Terpetakan |
| **F7-03** | Berkas PDF disimpan lokal, dicatat ke dalam database/state, dan dapat dibuka kembali dari aplikasi. | WP07 | T34, T35 | Terpetakan |

### 17.8 Ketentuan Lintas Fitur (X01 — X17)

| ID | Ketentuan Lintas Fitur Normatif | Paket Terkait | Skenario Uji |
|---|---|---|---|
| **X01** | Scope terbatas pada dinding dan atap; desktop single-user tanpa login; tidak menambah modul di luar spesifikasi. | WP00, WP02 | T01, T02 |
| **X02** | Basis data minimal memuat $\ge 15$ bahan dinding dan $\ge 10$ bahan atap terverifikasi. | WP02 | T39 |
| **X03** | Profil iklim disimpan permanen per proyek dan dapat ditinjau saat luring. | WP01, WP03 | T04, T36 |
| **X04** | Cache prakiraan cuaca memiliki TTL default 6 jam UTC per lokasi, dengan penanda data kedaluwarsa. | WP06 | T21, T22 |
| **X05** | Kegagalan jaringan atau pembatalan asinkron tidak membekukan antarmuka aplikasi. | WP03, WP06, WP08 | T05, T08 |
| **X06** | Seluruh kontrak layanan terhubung ke alur eksekusi antarmuka pengguna nyata. | WP03–WP08 | T01–T40 |
| **X07** | Migrasi EF Core dan inisialisasi SQL konsisten; pruning cache operasional tidak merusak riwayat keputusan. | WP01, WP06 | T37, T40 |
| **X08** | Seluruh perhitungan skor memenuhi arah intuisi termal, lembap, korosi, dan usia dengan rumus lembap baru. | WP04 | T11, T12, T13 |
| **X09** | Emisi awal merupakan perkalian faktor ($\text{kgCO}_2\text{e/m}^2$) dengan luas ($m^2$) secara konsisten. | WP02, WP04 | T15 |
| **X10** | Asumsi model perhitungan (faktor $a=0{,}5$, acuan hujan, batas korosi) berversi dan dapat di-reset ke default. | WP05 | T16, T18 |
| **X11** | Ambang penjadwalan cuaca dapat dikonfigurasi melalui override spesifikasi produk atau nilai bawaan. | WP06 | T28 |
| **X12** | Pengguna diizinkan mengunci jadwal pada hari tidak layak setelah memahami dan menyetujui risiko. | WP06 | T30 |
| **X13** | Keterbatasan data internasional, proksi pesisir, dan bounding box diungkapkan terbuka di UI dan PDF. | WP02–WP07 | T03, T10, T33 |
| **X14** | Perubahan input, katalog, atau bobot baru tidak menimpa atau memodifikasi berkas laporan lama. | WP01, WP04–WP07 | T38 |
| **X15** | Arsitektur MVVM diuji melalui unit test ViewModel tanpa ketergantungan render grafis WPF. | WP08 | Uji ViewModel |
| **X16** | Target performa (<3 mnt, <2 dtk, API $\ge 95\%$) dibuktikan melalui instrumen ukur riil dan uji pengguna nyata. | WP08 | Uji Performa/Pengguna |
| **X17** | Seluruh penyelarasan PRD-R01 sampai PRD-R11 terdokumentasi lengkap dan selaras dengan UML. | WP00, WP08 | Review WP00 |

### 17.9 Daftar Skenario Pengujian Wajib (T01 — T40)

- **T01:** Buat proyek atap saja ($90\text{ m}^2$) $\rightarrow$ tersimpan dengan satu komponen atap; navigasi menyesuaikan.
- **T02:** Pembuatan proyek tanpa komponen, atau luas $\le 0$, NaN, Infinity $\rightarrow$ ditolak dengan pesan validasi jelas.
- **T03:** Koordinat tepat batas $[-11, 95]$ dan $[6, 141]$ diterima; titik di luar batas ditolak.
- **T04:** Buat proyek mode lokal saat online $\rightarrow$ mengambil data iklim otomatis dan menggantikan estimasi awal.
- **T05:** Panggilan API iklim pertama gagal $\rightarrow$ proyek tetap tersimpan, status iklim belum tersedia dengan tombol coba lagi.
- **T06:** Refresh iklim gagal saat profil lama ada $\rightarrow$ profil lama dipertahankan dengan pesan peringatan gagal refresh.
- **T07:** Ubah lokasi dari A ke B saat API B gagal $\rightarrow$ profil dan cuaca lokasi A tidak dipakai untuk lokasi B.
- **T08:** Permintaan iklim lokasi A berjalan lalu pengguna beralih ke lokasi B $\rightarrow$ respons A tidak menimpa lokasi B.
- **T09:** Agregasi iklim historis 10 tahun mencakup tahun kabisat dan data hilang ditangani secara benar.
- **T10:** Uji geometri jarak pesisir terhadap titik garis pantai dan titik daratan berjarak terukur.
- **T11:** Hujan 700 vs 3000 mm/thn pada RH 80% dan ketahanan 3 $\rightarrow$ skor lembap terbedakan secara signifikan (~97,83 vs ~65,22).
- **T12:** Konduktivitas $\lambda = 1{,}0$ pada suhu 26 vs 32 °C $\rightarrow$ skor termal 60 vs 30 (monoton tidak naik).
- **T13:** Ketahanan korosi 2 pada jarak pesisir 0 vs 50 km $\rightarrow$ skor korosi 40 vs 100.
- **T14:** Normalisasi usia tidak terpengaruh oleh penyaringan kata kunci atau perbandingan dua bahan.
- **T15:** Penggandaan luas komponen dari 120 ke 240 $\text{m}^2$ menggandakan emisi awal tanpa mengubah skor kesesuaian.
- **T16:** Pengujian bobot MVP tetap 0,25; pengujian bobot V2 valid, tolak bobot dengan jumlah $\ne 100\%$ atau NaN.
- **T17:** Konsistensi label rekomendasi pada skor 54,999 ("Tidak disarankan"), 55,000 ("Perlu pertimbangan"), 74,999 ("Perlu pertimbangan"), dan 75,000 ("Cocok").
- **T18:** Nominal 15 thn, skor 100, horizon 30 $\rightarrow L=15, n=1$, total emisi $\times 2$; Nominal 15 thn, skor 20, horizon 30 $\rightarrow L=9, n=3$, total emisi $\times 4$.
- **T19:** Grafik emisi kumulatif menunjukkan lonjakan tangga pada tahun kejadian interior (9, 18, 27) dan konstan hingga tahun 30.
- **T20:** Simulasi horizon 20, 30, dan 50 tahun disimpan berdampingan tanpa saling menghapus.
- **T21:** Cache prakiraan cuaca segar pada umur 5 jam 59 menit; kedaluwarsa tepat pada umur 6 jam 00 menit.
- **T22:** Navigasi antar proyek tidak menukar atau menghilangkan cache prakiraan cuaca masing-masing proyek.
- **T23:** Penentuan tanggal hari ini dan penguncian jadwal mengikuti zona waktu lokasi proyek dari provider.
- **T24:** Pengecoran dengan hujan $>5\text{ mm}$ atau probabilitas hujan $>60\%$ menghasilkan status `TidakLayak`; suhu $>35^\circ\text{C}$ saja menghasilkan `Waspada`; batas tepat 60% dan 5 mm tidak melanggar.
- **T25:** Pengecatan dengan kelembapan $>85\%$ atau probabilitas $>40\%$ atau suhu di luar $10-35^\circ\text{C}$ menghasilkan status `TidakLayak`; batas tepat diterima.
- **T26:** Kelembapan maksimum harian digunakan untuk evaluasi pengecatan meskipun nilai rata-rata berada di bawah 85%.
- **T27:** Data cuaca bernilai null atau parsial menghasilkan status `DataTidakTersedia`, bukan nilai 0 atau Layak.
- **T28:** Override ambang pengecatan ke batas probabilitas 30% menyebabkan kondisi cuaca 35% berstatus `TidakLayak`.
- **T29:** Rentang pekerjaan melampaui 14 hari menampilkan seluruh hari dengan keterangan jelas untuk hari tanpa prakiraan; hari tanpa cuaca hanya bisa disimpan sebagai draft.
- **T30:** Mengunci hari `TidakLayak` menampilkan dialog konfirmasi risiko; pembatalan tidak mengunci, persetujuan mengunci tanggal beserta alasan.
- **T31:** Mengubah lokasi setelah tanggal dikunci menandai jadwal lama dengan status "Perlu ditinjau".
- **T32:** Ekspor PDF menyertakan bahan yang dipilih pengguna meskipun bahan tersebut berperingkat rendah.
- **T33:** Berkas PDF laporan dua komponen dengan nama/alasan panjang menghasilkan dokumen multi-halaman yang rapi dan valid.
- **T34:** Berkas PDF yang telah diekspor dapat dibuka kembali dari riwayat laporan; pemindahan berkas ditangani dengan pesan pemulihan.
- **T35:** Pembatalan dialog simpan file atau kegagalan penulisan folder tidak menghasilkan klaim sukses palsu.
- **T36:** Startup tanpa koneksi PostgreSQL remote berhasil membuka snapshot lokal dalam mode read-only.
- **T37:** Migrasi skema database dan impor data legacy bersifat idempoten tanpa duplikasi atau penghapusan data.
- **T38:** Perubahan bobot atau evaluasi baru tidak mengubah berkas PDF atau snapshot evaluasi lama.
- **T39:** Penambahan bahan baru langsung ke database PostgreSQL tampil pada katalog setelah refresh tanpa rebuild aplikasi.
- **T40:** Pembersihan (*pruning*) cache cuaca lama tidak menghapus snapshot keputusan jadwal historis yang telah dikunci.
