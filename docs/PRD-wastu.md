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

Wastu adalah aplikasi desktop Windows yang membantu calon pemilik rumah memilih bahan dinding dan atap berdasarkan **kesesuaian dengan iklim lokasi** dan **jejak karbon tersemat**, kemudian membantu menjadwalkan pekerjaan konstruksi sensitif cuaca mengikuti prakiraan cuaca setempat.

Perbedaannya dengan kebiasaan yang berlaku hari ini terletak pada satu hal mendasar: pemilihan bahan bangunan rumah tinggal di Indonesia hampir selalu ditentukan oleh harga beli dan kebiasaan tukang, bukan oleh kecocokan bahan dengan curah hujan, kelembapan, suhu, dan risiko korosi di lokasi bangunan berdiri. Akibatnya bahan cepat rusak, penggantian dini terjadi lebih sering dari usia nominalnya, dan setiap penggantian mengulang emisi karbon produksi bahan dari awal.

**Segmen Pengguna:** calon pemilik rumah tinggal tapak di Indonesia yang sedang merencanakan pembangunan baru atau renovasi pada komponen **dinding dan atap**. Aplikasi mendukung fleksibilitas komponen: pemilik proyek dapat mengevaluasi dinding saja, atap saja, atau kedua komponen sekaligus.

**Prinsip Desain yang Membedakan Wastu:**

1. **Iklim dan karbon dinilai berdampingan, bukan terpisah.** Skor kesesuaian iklim (0–100) dan emisi awal ($\text{kgCO}_2\text{e}$) ditampilkan pada satu peringkat yang sama berdampingan di setiap baris, disertai alasan tekstual berbasis properti fisik material terhadap iklim lokasi (§11.1–11.2).
2. **Emisi dihitung sebagai konsekuensi jangka panjang dengan fungsi tangga penggantian.** Bahan yang kurang cocok dengan iklim diberi usia pakai efektif lebih pendek. Simulasi emisi jangka panjang (20, 30, 50 tahun) menghitung kejadian penggantian pada interval $0 < t < H$ dengan rumus $n = \max(0, \lceil H/L \rceil - 1)$ (§PRD-R02, §11.3).
3. **Penjadwalan kerja menyatu dengan data lokasi dan spesifikasi teknis.** Penjadwal mengevaluasi hari kerja berdasarkan ambang jenis pekerjaan dan lembar spesifikasi produk bahan terpilih, bukan mengarang ambang otomatis dari indeks ketahanan material (§PRD-R05, §12).
4. **Penyimpanan fleksibel dan peninjauan luring yang aman.** Mendukung PostgreSQL lokal maupun remote (Supabase). Mode remote memiliki snapshot lokal atomik read-only agar aplikasi tetap dapat dibuka saat luring tanpa risiko crash atau kehilangan data (§PRD-R06, §7).
5. **Transparansi sumber dan batasan terbuka.** Faktor emisi dinormalisasi ke $\text{kgCO}_2\text{e/m}^2$ dengan dokumentasi asumsi ketebalan/massa; asal data diklasifikasikan ke dalam kategori provenance eksplisit (`ApiHistoris`, `Contoh`, `Manual`, `LegacyUnknown`); asumsi model perhitungan berversi dan dapat di-reset (§PRD-R03, §PRD-R09, §PRD-R11).

Dokumen revisi ini menetapkan target spesifikasi terpadu untuk **seluruh scope produk hingga V2** tanpa mengurangi cakupan 31 Acceptance Criteria F1–F7.

---

## 2. Latar Belakang dan Masalah

### 2.1 Bagaimana bahan bangunan dipilih hari ini

Pada konstruksi rumah tinggal non-korporat di Indonesia, keputusan bahan dinding dan atap umumnya diambil berdasarkan harga beli per satuan dan kebiasaan tukang atau kontraktor kecil, bukan berdasarkan kecocokan bahan dengan kondisi iklim setempat. Curah hujan tinggi, kelembapan ekstrem, kedekatan dengan pesisir (risiko korosi udara bergaram), dan radiasi termal harian adalah faktor lingkungan yang secara fisik menentukan kecepatan degradasi bahan, namun faktor-faktor ini jarang dipertimbangkan secara kuantitatif sebelum pembelian.

### 2.2 Konsekuensi berganda dari pemilihan yang salah

Kesalahan pemilihan bahan menimbulkan tiga konsekuensi berantai yang saling memperburuk:

1. **Kerusakan dini.** Bahan yang memiliki ketahanan lembap atau ketahanan korosi rendah di lingkungan tropis basah atau pesisir mengalami penurunan fungsi jauh sebelum usia pakai nominal tercapai.
2. **Emisi berulang (*embodied carbon penalty*).** Setiap kali penggantian dini dilakukan, emisi produksi bahan terulang dari awal. Emisi tersemat (*embodied carbon*) bukan biaya sekali bayar: ia berlipat ganda setiap siklus penggantian terjadi.
3. **Pengerjaan ulang akibat cuaca buruk.** Pengecoran beton saat hujan atau pengecatan pada kelembapan udara tinggi menyebabkan kegagalan hidrasi semen atau daya lekat cat terkelupas, yang berujung pada pembongkaran, biaya ekstra, dan emisi material yang terbuang sia-sia.

### 2.3 Celah yang belum terisi

Calon pemilik rumah di Indonesia tidak memiliki alat bantu yang membandingkan jejak karbon bahan fungsional yang setara secara berdampingan dengan kecocokan iklim lokasi. Alat yang ada saat ini terpolarisasi pada dua ekstrem:

- Perangkat LCA komersial (One Click LCA, EC3) ditujukan bagi konsultan gedung berskala besar, memerlukan input spesifikasi yang sangat rumit, dan minim pustaka material lokal Indonesia.
- Simulasi energi bangunan profesional (EnergyPlus) memerlukan keahlian teknik tinggi dan hanya mensimulasikan performa energi operasional, bukan memberi rekomendasi pemilihan bahan.
- Aplikasi lokal yang beredar berfokus semata-mata pada Rencana Anggaran Biaya (RAB) dan penghitungan volume fisik, tanpa analisis iklim maupun emisi karbon.

### 2.4 Rumusan masalah

1. Bagaimana merancang aplikasi desktop yang menyajikan skor kesesuaian iklim lokasi dan emisi karbon awal secara berdampingan dalam satu peringkat terpadu dengan alasan yang mudah dipahami pemilik rumah awam?
2. Bagaimana memodelkan dampak penggantian dini material sepanjang horizon waktu pemakaian secara transparan melalui fungsi emisi kumulatif yang konsisten?
3. Bagaimana memadukan prakiraan cuaca harian lokasi proyek dengan ambang batas teknis pekerjaan konstruksi agar risiko kegagalan pengerjaan akibat cuaca dapat diminimalkan?

---

## 3. Target Pengguna

### Persona 1 — Bu Wulan, calon pemilik rumah (pengguna utama)
Merencanakan pembangunan rumah tinggal tapak pertamanya di area pesisir. Sudah memiliki denah arsitektur dasar namun bingung menentukan material dinding dan penutup atap yang awet terhadap udara pantai dan tidak gerah. Membutuhkan penjelasan yang tidak memaksanya memahami rumus teknik, dengan rekomendasi tertulis yang bisa dibawa saat berdiskusi dengan tukang/kontraktor.

### Persona 2 — Pak Herman, pemilik rumah yang merenovasi
Mengalami atap bocor dan dinding berjamur setelah 6 tahun renovasi terakhir. Curiga bahwa material yang dipilih sebelumnya tidak tahan terhadap curah hujan tinggi di kotanya. Membutuhkan simulasi perbandingan jangka panjang untuk memastikan renovasi kali ini tidak mengulang siklus kerusakan dini.

### Persona 3 — Kontraktor kecil / mandor (penerima laporan)
Menerima dokumen ringkasan rekomendasi bahan dan jadwal kerja dari pemilik rumah. Tidak mengoperasikan perangkat lunak Wastu secara langsung, namun membutuhkan laporan PDF yang ringkas, memuat spesifikasi bahan terpilih, alasan ketahanan iklim, dan tanggal penguncian jadwal kerja yang aman.

---

## 4. Tujuan dan Metrik Keberhasilan

### 4.1 Metrik produk

| Metrik | Target | Metode Pengukuran & Pembuktian |
|---|---|---|
| Waktu alur dari pembuatan proyek hingga peringkat tampil | < 3 menit | Uji pengguna kualitatif terhadap persona Bu Wulan dan Pak Herman dengan dataset proyek nyata. |
| Basis data bahan dinding siap dinilai | $\ge 15$ bahan | Verifikasi dataset basis data (`kind = 'dinding'`) dengan properti lengkap dan tervalidasi. |
| Basis data bahan atap siap dinilai | $\ge 10$ bahan | Verifikasi dataset basis data (`kind = 'atap'`) dengan properti lengkap dan tervalidasi. |
| Waktu hitung peringkat perbandingan satu komponen | < 2 detik | Benchmark komputasi domain dan render UI pada perangkat kelas menengah (Core i5 / Ryzen 5, 8GB RAM). |
| Keberhasilan pengambilan data cuaca online | $\ge 95\%$ | Pengujian otomatis berkala terhadap endpoint Open-Meteo; keberhasilan dihitung dari data yang **valid dan dapat digunakan** (bukan sekadar status HTTP 200). |
| Pemahaman pengguna terhadap alasan rekomendasi | $\ge 80\%$ responden memahami tanpa istilah teknik | Evaluasi wawancara kualitatif pasca-alur pada pengujian pengguna V2. |

### 4.2 Metrik kualitas rekomendasi dan konsistensi matematika

| Sifat Kualitas | Kontrak Perilaku | Bukti Pengujian |
|---|---|---|
| **Monotonisitas Skor Ketahanan** | Peningkatan indeks ketahanan bahan tidak boleh menurunkan skor kesesuaian terkait; peningkatan beban cuaca tidak boleh menaikkan skor. Daerah plateau pada saat kebutuhan terpenuhi dijelaskan secara terbuka (§PRD-R04). | Pengujian unit kasus sintetis numerik pada `PerhitunganSkor`. |
| **Monotonisitas Emisi Kumulatif** | Penurunan usia pakai efektif ($L$) bersifat monoton tidak turun terhadap emisi kumulatif ($n$ tidak pernah menurun jika $L$ menurun, *ceteris paribus*). Daerah plateau terjadi karena fungsi tangga integer (§PRD-R02, §PRD-R04). | Pengujian variasi horizon dan usia efektif pada `PerhitunganSimulasi`. |
| **Ketahanan Degradasi API** | Saat panggilan jaringan gagal atau API cuaca terputus, aplikasi tidak boleh membeku (*hang*) atau menutup paksa (*crash*), melainkan beralih ke cache lokal yang tersedia atau menampilkan status luring dengan opsi coba lagi (§PRD-R06). | Pengujian unit asinkron dan simulasi pemutusan koneksi jaringan. |

---

## 5. Scope Produk F1–F7 dan Rencana Rilis Bertahap

### 5.1 Scope Produk F1–F7 (Target Penuh Sampai V2) dan Pemisahan Tahapan

Rencana revisi Wastu mencakup implementasi fungsional penuh dari F1 sampai F7 hingga rilis **V2**. Pemisahan tahapan rilis diatur sebagai berikut:

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

## 6. Fitur dan User Stories Lengkap (F1 — F7)

### F1 — Profil Proyek dan Komponen

> Sebagai calon pemilik rumah, saya ingin mencatat lokasi dan komponen yang sedang saya kerjakan agar rekomendasi material dan cuaca sesuai dengan proyek saya.

**Acceptance Criteria:**
- **F1-01:** Pengguna dapat membuat, melihat, mengubah, dan menghapus proyek (nama proyek, kota, koordinat latitude dan longitude). Koordinat dapat terisi otomatis dari pilihan kota atau diisi manual.
- **F1-02:** Proyek memiliki minimal satu komponen aktif (dinding saja, atap saja, atau keduanya), masing-masing dengan nilai luas area positif ($> 0\text{ m}^2$). Pembuatan proyek tanpa komponen atau dengan luas tidak valid ($\le 0$, NaN, tak hingga) ditolak.
- **F1-03:** Mengubah koordinat lokasi proyek memicu pembaharuan profil iklim otomatis saat online, menginvalidasi cache cuaca lokasi lama, dan menandai jadwal kerja lama untuk ditinjau ulang (§PRD-R07).
- **F1-04:** Koordinat divalidasi pada rentang inklusif lintang $-11{,}0$ s/d $+6{,}0$ dan bujur $+95{,}0$ s/d $+141{,}0$. Data legacy yang berada pada rentang batas 94°–95° atau 141°–142° diaudit aman dengan penanda peninjauan tanpa penggagalan startup atau migrasi.

### F2 — Basis Data Bahan dan Provenance

> Sebagai calon pemilik rumah, saya ingin melihat katalog bahan dinding dan atap yang lengkap dengan karakteristik fisik, faktor emisi, dan sumber datanya agar saya dapat mengevaluasi pilihan bahan secara transparan.

**Acceptance Criteria:**
- **F2-01:** Setiap bahan memuat properti: nama, jenis komponen (`dinding` atau `atap`), konduktivitas termal ($W/m\cdot K$), indeks ketahanan lembap (1–5), indeks ketahanan korosi (1–5), usia pakai nominal (tahun), dan faktor emisi karbon tersemat yang dinormalisasi ke $\text{kgCO}_2\text{e/m}^2$ untuk susunan/ketebalan tertentu (§PRD-R03).
- **F2-02:** Pengguna dapat memfilter bahan berdasarkan kategori komponen (`dinding` atau `atap`) tanpa percampuran data antar kategori.
- **F2-03:** Setiap properti bahan mencatat metadata provenance (`ApiHistoris`, `Contoh`, `Manual`, `LegacyUnknown`) dan sumber literatur/basis data publik (ICE Database, EPD generik). Konversi nilai dari basis per $m^3$ atau per $kg$ wajib mencatat asumsi ketebalan atau massa jenis (§PRD-R03, §PRD-R09).
- **F2-04:** Administrator atau pengembang dapat menambah atau memperbarui data bahan melalui basis data relasional tanpa perlu melakukan *recompile* atau *rebuild* kode aplikasi.

### F3 — Analisis Iklim Lokasi Otomatis

> Sebagai calon pemilik rumah, saya ingin sistem menganalisis kondisi iklim lokasi proyek saya secara otomatis dari data historis agar saya tidak perlu mencari data meteorologi sendiri.

**Acceptance Criteria:**
- **F3-01:** Sistem mengambil data iklim historis 10 tahun dari Open-Meteo Historical Weather API berdasarkan koordinat proyek, mencakup agregasi curah hujan tahunan rata-rata ($\text{mm/tahun}$), kelembapan relatif rata-rata ($\%RH$), dan suhu udara rata-rata ($^\circ C$).
- **F3-02:** Jarak ke garis pantai terdekat ($\text{km}$) dihitung secara geometris dari koordinat lokasi terhadap data vektor pesisir sebagai proksi risiko korosi udara bergaram (dengan batas efektif kalkulasi $50\text{ km}$, §PRD-R10).
- **F3-03:** Profil iklim disimpan per proyek (multiplisitas relasi $0..1$ sebelum data berhasil diambil) dan dipakai ulang saat aplikasi dibuka kembali atau saat luring, tanpa memanggil API ulang jika koordinat tidak berubah (§PRD-R06, §PRD-R10).
- **F3-04:** Ringkasan profil iklim beserta status kualitas data ditampilkan secara ringkas pada antarmuka sebelum pengguna masuk ke modul perbandingan bahan.

### F4 — Perbandingan Berperingkat dan Pembobotan Kriteria

> Sebagai calon pemilik rumah, saya ingin melihat daftar bahan diurutkan dari yang paling cocok untuk iklim lokasi saya, lengkap dengan skor kesesuaian, emisi awal, dan alasan penilaian yang jelas.

**Acceptance Criteria:**
- **F4-01:** Sistem menampilkan daftar bahan terurut berdasarkan skor kesesuaian iklim (0–100) menurun. Jika skor identik, urutan kedua adalah emisi terendah, diikuti kestabilan nama/ID bahan.
- **F4-02:** Setiap baris kartu/tabel perbandingan menyajikan skor kesesuaian dan angka emisi awal ($\text{kgCO}_2\text{e}$) berdampingan dalam satu kesatuan tampilan.
- **F4-03:** Setiap baris bahan menyertakan kalimat alasan penilaian (`reasoning`) yang menjelaskan kesesuaian properti bahan terhadap profil iklim lokasi dalam bahasa yang mudah dipahami.
- **F4-04:** Pada mode rilis **MVP**, evaluasi menggunakan empat bobot kriteria tetap bernilai masing-masing $0{,}25$ (termal, lembap, korosi, usia), dan slider bobot kustom dinonaktifkan (§PRD-R01).
- **F4-05:** Pada mode rilis **V2**, pengguna dapat mengubah bobot empat kriteria (dengan validasi jumlah total tepat $100\%$, non-negatif, dan finite) serta tombol "Kembalikan ke Default" ($0{,}25$). Perubahan bobot langsung memperbarui peringkat secara reaktif tanpa memicu pengambilan ulang data iklim dari internet (§PRD-R01).
- **F4-06:** Ambang rekomendasi label terpusat diterapkan secara konsisten pada seluruh tampilan:
  - Skor $< 55{,}0$: Label visual **"Tidak disarankan"** (bahan tetap ditampilkan, tidak disembunyikan).
  - $55{,}0 \le \text{Skor} < 75{,}0$: Label visual **"Perlu pertimbangan"**.
  - Skor $\ge 75{,}0$: Label visual **"Cocok"** (§PRD-R10).

### F5 — Simulasi Emisi Jangka Panjang

> Sebagai calon pemilik rumah, saya ingin mengetahui proyeksi total emisi karbon dari bahan yang saya pilih sepanjang masa pakai rumah, termasuk dampak penggantian dini akibat kerusakan iklim.

**Acceptance Criteria:**
- **F5-01:** Pengguna dapat memilih horizon waktu simulasi (minimal 20, 30, dan 50 tahun). Hasil simulasi berbagai horizon disimpan persisten tanpa saling menimpa (§PRD-R10).
- **F5-02:** Usia pakai efektif bahan ($L$) dihitung dengan fungsi penurunan dari usia nominal berdasarkan skor kesesuaian: $L = \text{usia\_nominal} \times (a + (1 - a) \times \text{skor}/100)$, dengan nilai parameter default $a = 0{,}5$ (§PRD-R02, §PRD-R11).
- **F5-03:** Jumlah penggantian dihitung strictly pada rentang kejadian $0 < t < H$:
  $$n = \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right)$$
  dan emisi kumulatif dihitung dengan:
  $$\text{emisi\_kumulatif} = \text{emisi\_awal} \times (1 + n)$$
  Penggantian tepat pada batas akhir horizon ($t = H$) tidak dihitung (§PRD-R02).
  - Kasus acuan terverifikasi: Nominal 15 thn, Skor 100, $H=30 \rightarrow L=15, n=1$ (tahun 15), emisi $\times 2$.
  - Kasus acuan terverifikasi: Nominal 15 thn, Skor 20, $H=30 \rightarrow L=9, n=3$ (tahun 9, 18, 27), emisi $\times 4$.
- **F5-04:** Hasil disajikan dalam bentuk perbandingan visual grafik bertingkat (*step chart*) yang menampilkan kejadian penggantian interior dan tingkat kumulatif akhir, dengan total emisi kumulatif $\text{kgCO}_2\text{e}$ sebagai sorotan utama.
- **F5-05:** Antarmuka menyatakan secara terbuka bahwa usia pakai efektif adalah estimasi model perhitungan, bukan jaminan keawetan mutlak di lapangan (§PRD-R11).

### F6 — Penjadwal Pekerjaan Sensitif Cuaca

> Sebagai calon pemilik rumah, saya ingin mengetahui tanggal mana yang aman dari hujan dan cuaca buruk untuk pengecoran atau pengecatan agar terhindar dari pengerjaan ulang.

**Acceptance Criteria:**
- **F6-01:** Pengguna dapat membuat pekerjaan konstruksi terjadwal dengan jenis pekerjaan (`pengecoran` atau `pengecatan`) dan rentang tanggal rencana pelaksanaan. Pekerjaan dapat dikaitkan dengan bahan/komponen terkait (§PRD-R05).
- **F6-02:** Sistem mengambil data prakiraan cuaca 14 hari dari Open-Meteo Forecast API dan mengevaluasi kelayakan setiap hari berdasarkan ambang teknis pekerjaan. Evaluasi pengecatan menggunakan kelembapan maksimum harian dari data per jam (§PRD-R05, §PRD-R08).
- **F6-03:** Keputusan evaluasi harian mengembalikan status multi-nilai:
  - `Layak`: Memenuhi seluruh ambang keselamatan cuaca.
  - `Waspada`: Mendekati batas atau suhu tinggi pada pengecoran dengan catatan mitigasi.
  - `TidakLayak`: Melanggar satu atau lebih ambang cuaca (disertai alasan spesifik ambang yang dilanggar).
  - `DataTidakTersedia`: Tanggal di luar rentang prakiraan (>14 hari) atau data provider bernilai null/parsial (§PRD-R08).
- **F6-04:** Pengguna dapat mengunci satu tanggal sebagai jadwal final. Penguncian pada tanggal berstatus `TidakLayak` dapat dilakukan setelah pengguna memberikan konfirmasi persetujuan risiko eksplisit (§PRD-R10).
- **F6-05:** Prakiraan cuaca disimpan dalam cache lokal dengan **TTL default 6 jam** berbasis timestamp UTC pengambilan. Tanggal evaluasi mengikuti zona waktu lokasi proyek yang dilaporkan oleh penyedia API (§PRD-R08).

### F7 — Ekspor Laporan Rekomendasi PDF

> Sebagai calon pemilik rumah, saya ingin mengunduh laporan rekomendasi bahan dan jadwal kerja dalam format PDF agar dapat saya cetak atau kirimkan ke kontraktor.

**Acceptance Criteria:**
- **F7-01:** Menghasilkan berkas PDF yang rapi memuat: profil proyek, ringkasan iklim lokasi, daftar bahan terpilih beserta skor kesesuaian dan emisi awalnya, hasil simulasi emisi jangka panjang, dan jadwal pekerjaan yang telah dikunci.
- **F7-02:** Dokumen mencantumkan tanggal pembuatan (timestamp UTC dan zona waktu lokal proyek), versi algoritme/model perhitungan, serta atribusi sumber data bahan dan API cuaca sebagai catatan transparansi (§PRD-R09).
- **F7-03:** Berkas PDF disimpan ke media penyimpanan lokal pengguna, dicatat ke dalam riwayat laporan aplikasi, dan dapat dibuka kembali secara langsung dari antarmuka Wastu (§PRD-R06).

---

## 7. Arsitektur Sistem dan Komponen

```
┌─ ANTARMUKA PENGGUNA (WPF, .NET 8, MVVM) ───────────────────────────────────┐
│  Views & ViewModels: Project, Materials, Compare, Simulation, Schedule, Reports  │
│  Komunikasi asinkron non-blocking, debounced input, penanganan state luring      │
└─────────────────────────────────────┬───────────────────────────────────────┘
                                      │ Injeksi Layanan / Kontrak Domain
┌─────────────────────────────────────▼───────────────────────────────────────┐
│  LAPISAN LAYANAN APLIKASI (Application / Domain Services)                   │
│   ├─ ProjectService, MaterialService                                        │
│   ├─ ClimateAnalysisService (koordinasi profil iklim & cache)                │
│   ├─ SuitabilityScoringService (perhitungan skor 4 kriteria & label)         │
│   ├─ EmissionSimulationService (penuaan efektif & emisi kumulatif)          │
│   ├─ WorkSchedulingService (evaluasi ambang cuaca & penguncian jadwal)      │
│   └─ ReportExportService (pembentukan PDF & pencatatan riwayat laporan)     │
└───────────────┬───────────────────────────────────────────┬─────────────────┘
                │                                           │
┌───────────────▼───────────────────────────┐   ┌───────────▼─────────────────┐
│ LAPISAN PERSISTENSI DATA                   │   │ KLIEN CUACA & IKLIM          │
│ EF Core + Npgsql (PostgreSQL lokal/remote)│   │ Open-Meteo API Client        │
│  ├─ Repository & Unit of Work             │   │  ├─ IHistoricalClimateClient │
│  ├─ Snapshot Lokal Atomik (Read-Only)     │   │  ├─ IDailyForecastClient     │
│  └─ Kompatibilitas JSON (StateStore)      │   │  └─ Cache TTL 6 Jam UTC      │
└───────────────────────────────────────────┘   └─────────────────────────────┘
```

Aplikasi dirancang sebagai aplikasi desktop mandiri tanpa server backend perantara. Koneksi database ke PostgreSQL (lokal atau cloud Supabase) dikelola langsung oleh EF Core. Klien Open-Meteo berkomunikasi melalui protokol HTTPS dengan serializer JSON invariant culture.

---

## 8. Integrasi Layanan Cuaca Pihak Ketiga (Open-Meteo)

### 8.1 Dua Kebutuhan Data Cuaca

| Karakteristik | Analisis Iklim Lokasi (F3) | Prakiraan Cuaca Harian (F6) |
|---|---|---|
| **Kebutuhan** | Data rata-rata historis jangka panjang (10 tahun). | Prakiraan harian jangka pendek (14 hari ke depan). |
| **Variabel Utama** | Curah hujan tahunan ($\text{mm}$), suhu rata-rata ($^\circ C$), kelembapan relatif rata-rata ($\%RH$). | Probabilitas hujan ($\%$) & presipitasi ($\text{mm}$), suhu min/maks ($^\circ C$), kelembapan maksimum ($\%RH$). |
| **Endpoint API** | Open-Meteo Archive / Historical Weather API. | Open-Meteo Daily Forecast API. |
| **Masa Berlaku** | Permanen per lokasi; hanya diperbarui jika lokasi berubah atau atas permintaan manual. | Cache ber-TTL 6 jam; data kedaluwarsa diberi label "Data lama". |

### 8.2 Kontrak Layanan Cuaca

Lapisan domain tidak bergantung langsung pada kelas pustaka HTTP eksternal, melainkan melalui kontrak antarmuka abstraksi:

```csharp
public interface IHistoricalClimateClient
{
    Task<ClimateProfileResult> GetHistoricalProfileAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}

public interface IDailyForecastClient
{
    Task<ForecastResult> GetDailyForecastAsync(
        double latitude,
        double longitude,
        int days = 14,
        CancellationToken cancellationToken = default);
}
```

Hasil pembacaan API menyertakan metadata waktu pengambilan UTC (`fetched_at`), zona waktu resmi lokasi proyek (`timezone`), serta ringkasan kualitas data.

### 8.3 Pengelolaan Cache, TTL, dan Perilaku Luring

1. **Durasi Cache:** Cache prakiraan cuaca memiliki masa berlaku default **6 jam** dihitung dari timestamp UTC `fetched_at`. Permintaan pada rentang umur cache $< 6\text{ jam}$ dilayani langsung dari penyimpanan lokal.
2. **Zona Waktu:** Tanggal evaluasi harian ditafsirkan berdasarkan zona waktu lokasi proyek yang dikembalikan provider cuaca, bukan jam komputer klien Windows (§PRD-R08).
3. **Penanganan Kegagalan:** Jika koneksi API gagal saat pembaharuan, data cache yang ada tetap dipertahankan dengan indikator "Data lama (kedaluwarsa)". Jika cache tidak tersedia, status hari ditetapkan sebagai `DataTidakTersedia`. Aplikasi tidak boleh macet atau memblokir UI thread.

---

## 9. Skema Data Relasional dan Pemetaan Fisik

Basis data utama menggunakan PostgreSQL. Naming convention skema fisik tabel di database menggunakan Bahasa Indonesia, dipetakan secara terstruktur dengan entitas C# domain.

### 9.1 Definisi Skema Fisik Tabel (Bahasa Indonesia)

```sql
-- Tabel Proyek Bangunan
CREATE TABLE bangunan (
    id UUID PRIMARY KEY,
    nama VARCHAR(200) NOT NULL,
    kota VARCHAR(100) NOT NULL,
    latitude DOUBLE PRECISION NOT NULL CHECK (latitude BETWEEN -11.0 AND 6.0),
    longitude DOUBLE PRECISION NOT NULL CHECK (longitude BETWEEN 95.0 AND 141.0),
    dibuat_pada TIMESTAMPTZ NOT NULL DEFAULT now(),
    diperbarui_pada TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Tabel Profil Iklim Lokasi
CREATE TABLE profil_iklim (
    bangunan_id UUID PRIMARY KEY REFERENCES bangunan(id) ON DELETE CASCADE,
    curah_hujan_tahunan REAL NOT NULL CHECK (curah_hujan_tahunan >= 0),
    kelembapan_rata REAL NOT NULL CHECK (kelembapan_rata BETWEEN 0 AND 100),
    suhu_rata REAL NOT NULL,
    jarak_pesisir_km REAL NOT NULL CHECK (jarak_pesisir_km >= 0),
    tingkat_korosi VARCHAR(20) NOT NULL, -- 'rendah' | 'sedang' | 'tinggi'
    sumber VARCHAR(100) NOT NULL,
    provenance VARCHAR(50) NOT NULL DEFAULT 'ApiHistoris',
    diambil_pada_utc TIMESTAMPTZ NOT NULL
);

-- Tabel Komponen Proyek
CREATE TABLE komponen (
    id UUID PRIMARY KEY,
    bangunan_id UUID NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
    jenis VARCHAR(20) NOT NULL CHECK (jenis IN ('dinding', 'atap')),
    luas_m2 REAL NOT NULL CHECK (luas_m2 > 0),
    UNIQUE (bangunan_id, jenis)
);

-- Tabel Katalog Bahan
CREATE TABLE bahan (
    id UUID PRIMARY KEY,
    nama VARCHAR(200) NOT NULL,
    jenis VARCHAR(20) NOT NULL CHECK (jenis IN ('dinding', 'atap')),
    konduktivitas_termal REAL NOT NULL CHECK (konduktivitas_termal > 0),
    ketahanan_lembap SMALLINT NOT NULL CHECK (ketahanan_lembap BETWEEN 1 AND 5),
    ketahanan_korosi SMALLINT NOT NULL CHECK (ketahanan_korosi BETWEEN 1 AND 5),
    usia_nominal_tahun REAL NOT NULL CHECK (usia_nominal_tahun > 0),
    faktor_emisi_m2 REAL NOT NULL CHECK (faktor_emisi_m2 >= 0), -- kgCO2e per m2
    susunan_ketebalan VARCHAR(200),
    sumber VARCHAR(255) NOT NULL,
    provenance VARCHAR(50) NOT NULL DEFAULT 'Contoh'
);

-- Tabel Evaluasi Kesesuaian Bahan
CREATE TABLE evaluasi (
    id UUID PRIMARY KEY,
    komponen_id UUID NOT NULL REFERENCES komponen(id) ON DELETE CASCADE,
    bahan_id UUID NOT NULL REFERENCES bahan(id) ON DELETE RESTRICT,
    skor_kesesuaian REAL NOT NULL CHECK (skor_kesesuaian BETWEEN 0 AND 100),
    emisi_awal_kg REAL NOT NULL CHECK (emisi_awal_kg >= 0),
    alasan TEXT NOT NULL,
    bobot_termal REAL NOT NULL DEFAULT 0.25,
    bobot_lembap REAL NOT NULL DEFAULT 0.25,
    bobot_korosi REAL NOT NULL DEFAULT 0.25,
    bobot_usia REAL NOT NULL DEFAULT 0.25,
    dihitung_pada_utc TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Tabel Simulasi Emisi Jangka Panjang
CREATE TABLE simulasi_emisi (
    id UUID PRIMARY KEY,
    evaluasi_id UUID NOT NULL REFERENCES evaluasi(id) ON DELETE CASCADE,
    horizon_tahun SMALLINT NOT NULL CHECK (horizon_tahun IN (20, 30, 50)),
    usia_efektif_tahun REAL NOT NULL CHECK (usia_efektif_tahun > 0),
    jumlah_penggantian SMALLINT NOT NULL CHECK (jumlah_penggantian >= 0),
    emisi_kumulatif_kg REAL NOT NULL CHECK (emisi_kumulatif_kg >= 0),
    versi_model VARCHAR(50) NOT NULL DEFAULT 'v2.0',
    UNIQUE (evaluasi_id, horizon_tahun, versi_model)
);

-- Tabel Pekerjaan Konstruksi
CREATE TABLE pekerjaan (
    id UUID PRIMARY KEY,
    bangunan_id UUID NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
    nama VARCHAR(200) NOT NULL,
    jenis VARCHAR(50) NOT NULL CHECK (jenis IN ('pengecoran', 'pengecatan')),
    tanggal_mulai DATE NOT NULL,
    tanggal_selesai DATE NOT NULL CHECK (tanggal_selesai >= tanggal_mulai),
    tanggal_dikunci DATE,
    status VARCHAR(30) NOT NULL DEFAULT 'draft', -- 'draft' | 'terkunci' | 'selesai'
    persetujuan_risiko BOOLEAN NOT NULL DEFAULT FALSE,
    komponen_id UUID REFERENCES komponen(id) ON DELETE SET NULL,
    bahan_id UUID REFERENCES bahan(id) ON DELETE SET NULL
);

-- Tabel Cache Prakiraan Cuaca
CREATE TABLE prakiraan_cache (
    id UUID PRIMARY KEY,
    bangunan_id UUID NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
    tanggal_cuaca DATE NOT NULL,
    probabilitas_hujan REAL,
    curah_hujan_mm REAL,
    suhu_min_c REAL,
    suhu_maks_c REAL,
    kelembapan_maks_pct REAL,
    zona_waktu VARCHAR(50) NOT NULL,
    diambil_pada_utc TIMESTAMPTZ NOT NULL,
    UNIQUE (bangunan_id, tanggal_cuaca)
);

-- Tabel Riwayat Laporan Ekspor
CREATE TABLE laporan (
    id UUID PRIMARY KEY,
    bangunan_id UUID NOT NULL REFERENCES bangunan(id) ON DELETE CASCADE,
    path_berkas TEXT NOT NULL,
    format VARCHAR(10) NOT NULL DEFAULT 'PDF',
    dibuat_pada_utc TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

---

## 10. Spesifikasi Modul dan Kontrak Layanan

| Modul Layanan | Antarmuka Kontrak Utama | Tanggung Jawab Operasional |
|---|---|---|
| `ProjectService` | `CreateProjectAsync`, `UpdateLocationAsync`, `SetComponentsAsync`, `DeleteProjectAsync` | Validasi koordinat wilayah layanan, manajemen komponen (dinding/atap), penandaan invalidasi cache iklim saat lokasi berubah. |
| `ClimateAnalysisService` | `GetOrFetchProfileAsync`, `RefreshProfileAsync` | Pengambilan profil iklim historis via `IHistoricalClimateClient`, komputasi proksi jarak pesisir, penyimpanan relasi profil 0..1. |
| `MaterialService` | `GetMaterialsByKindAsync`, `AddMaterialAsync`, `GetMaterialDetailAsync` | Penyediaan katalog bahan dinding/atap, penapisan kategori, pembacaan metadata provenance dan rincian faktor emisi. |
| `SuitabilityScoringService` | `EvaluateComponentMaterialsAsync(componentId, weights, config)` | Komputasi empat sub-skor, penentuan alasan tekstual per baris, perankingan stabil, penerapan ambang label rekomendasi 55/75. |
| `EmissionSimulationService` | `RunSimulationAsync(evaluationId, horizon, config)` | Komputasi usia efektif $L$, komputasi penggantian interior $n = \max(0, \lceil H/L \rceil - 1)$, proyeksi emisi kumulatif multi-horizon. |
| `WorkSchedulingService` | `EvaluateWorkDaysAsync(workId)`, `LockWorkDateAsync(workId, date, confirmRisk)` | Evaluasi kelayakan cuaca harian 14 hari, manajemen cache TTL 6 jam UTC, penguncian tanggal dengan konfirmasi risiko. |
| `ReportExportService` | `ExportRecommendationPdfAsync(projectId, options)` | Pembuatan dokumen PDF dari snapshot hasil evaluasi, pencatatan metadata laporan, penyediaan akses buka berkas lokal. |

---

## 11. Metode Penilaian Kesesuaian Iklim dan Emisi Karbon

### 11.1 Perhitungan Skor Kesesuaian Iklim

Skor kesesuaian komposit dihitung sebagai kombinasi linier berbobot dari empat sub-skor (masing-masing berskala 0 s/d 100):

$$\text{skor\_kesesuaian} = w_{\text{termal}} \cdot s_{\text{termal}} + w_{\text{lembap}} \cdot s_{\text{lembap}} + w_{\text{korosi}} \cdot s_{\text{korosi}} + w_{\text{usia}} \cdot s_{\text{usia}}$$

dengan syarat $\sum w = 1{,}0$. Pada MVP, $w_{\text{termal}} = w_{\text{lembap}} = w_{\text{korosi}} = w_{\text{usia}} = 0{,}25$.

#### 1. Sub-Skor Lembap ($s_{\text{lembap}}$) — Model Terkoreksi (§PRD-R04)

Untuk mengatasi kejenuhan dini pada rumus lama, komposisi beban menggabungkan curah hujan tahunan dan kelembapan relatif dengan bobot seimbang $\alpha = 0{,}5$ dan acuan hujan $3000\text{ mm/tahun}$:

$$\begin{aligned}
r &= \operatorname{clamp}\left(\frac{\text{curah\_hujan\_tahunan}}{3000}, 0, 1\right) \\
h &= \operatorname{clamp}\left(\frac{\text{kelembapan\_rata}}{100}, 0, 1\right) \\
\text{beban} &= \alpha \cdot r + (1 - \alpha) \cdot h, \quad \text{default } \alpha = 0{,}5 \\
\text{kebutuhan} &= 1 + 4 \cdot \text{beban} \\
s_{\text{lembap}} &= \operatorname{clamp}\left(\frac{100 \cdot \text{ketahanan\_lembap}}{\text{kebutuhan}}, 0, 100\right)
\end{aligned}$$

*Kasus Uji Acuan (RH = 80%, Ketahanan Lembap = 3):*
- Hujan $700\text{ mm/thn}$: $\text{beban} \approx 0{,}5167 \rightarrow \text{kebutuhan} \approx 3{,}0667 \rightarrow s_{\text{lembap}} \approx 97{,}83$.
- Hujan $3000\text{ mm/thn}$: $\text{beban} = 0{,}9000 \rightarrow \text{kebutuhan} = 4{,}6000 \rightarrow s_{\text{lembap}} \approx 65{,}22$.
Terjadi diferensiasi skor yang jelas (~32,6 poin sub-skor, atau ~8,15 poin skor akhir pada bobot 0,25) tanpa saturasi dini.

#### 2. Sub-Skor Termal ($s_{\text{termal}}$)

Mengevaluasi konduktivitas termal bahan ($\lambda$ dalam $W/m\cdot K$) terhadap suhu rata-rata lokasi ($T$ dalam $^\circ C$):
- Pita bawah konduktivitas ideal: $\lambda_{\min} = 0{,}10\text{ W/m}\cdot\text{K}$.
- Batas atas ideal bergeser linier terhadap suhu:
  $$\lambda_{\max}(T) = \begin{cases}
  0{,}60, & T \le 26^\circ\text{C} \\
  0{,}60 - 0{,}05 \cdot (T - 26), & 26^\circ\text{C} < T < 32^\circ\text{C} \\
  0{,}30, & T \ge 32^\circ\text{C}
  \end{cases}$$
- Jika $\lambda \le \lambda_{\max}(T)$, maka $s_{\text{termal}} = 100$. Jika $\lambda > \lambda_{\max}(T)$, maka $s_{\text{termal}} = \operatorname{clamp}\left(100 \cdot \frac{\lambda_{\max}(T)}{\lambda}, 0, 100\right)$.

#### 3. Sub-Skor Korosi ($s_{\text{korosi}}$)

Mengevaluasi indeks ketahanan korosi bahan (1–5) terhadap jarak ke garis pantai ($d$ dalam $\text{km}$):
$$\begin{aligned}
d_{\text{efektif}} &= \min(d, 50) \\
\text{kebutuhan\_korosi} &= 1 + 4 \cdot \left(1 - \frac{d_{\text{efektif}}}{50}\right) \\
s_{\text{korosi}} &= \operatorname{clamp}\left(\frac{100 \cdot \text{ketahanan\_korosi}}{\text{kebutuhan\_korosi}}, 0, 100\right)
\end{aligned}$$
Lokasi pesisir ($d = 0$) membutuhkan ketahanan 5 untuk mencapai skor 100; lokasi daratan jauh ($d \ge 50\text{ km}$) memiliki kebutuhan 1 sehingga seluruh bahan memperoleh sub-skor 100.

#### 4. Sub-Skor Usia Pakai ($s_{\text{usia}}$)

Dinormalisasi secara min-maks terhadap sebaran usia pakai nominal seluruh bahan valid pada kategori yang sama:
$$s_{\text{usia}} = \begin{cases}
100 \cdot \frac{\text{usia} - \text{usia}_{\min}}{\text{usia}_{\max} - \text{usia}_{\min}}, & \text{usia}_{\max} > \text{usia}_{\min} \\
100, & \text{usia}_{\max} = \text{usia}_{\min}
\end{cases}$$
Rentang acuan $\text{usia}_{\min}$ dan $\text{usia}_{\max}$ ditentukan dari seluruh katalog aktif kategori tersebut, dan tidak berubah saat pengguna melakukan pencarian atau memilih dua bahan untuk dibandingkan.

### 11.2 Perhitungan Emisi Karbon Awal

Emisi karbon tersemat awal ($\text{kgCO}_2\text{e}$) dihitung secara langsung dari perkalian faktor emisi ternormalisasi dengan luas bidang komponen:

$$\text{emisi\_awal} = \text{faktor\_emisi\_m2} \times \text{luas\_area\_m2}$$

Skor kesesuaian dan angka emisi awal disajikan berdampingan pada setiap kartu dan baris tabel rekomendasi.

### 11.3 Simulasi Emisi Jangka Panjang dan Penggantian Dini (§PRD-R02)

Usia pakai efektif ($L$) diturunkan dari usia pakai nominal menggunakan fungsi monoton naik:

$$L = \text{usia\_nominal} \times \left(a + (1 - a) \cdot \frac{\text{skor\_kesesuaian}}{100}\right), \quad \text{default } a = 0{,}5$$

Jumlah penggantian ($n$) dihitung strictly untuk kejadian penggantian pada interval $0 < t < H$:

$$n = \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right)$$

dan total emisi kumulatif dihitung dengan:

$$\text{emisi\_kumulatif} = \text{emisi\_awal} \times (1 + n)$$

#### Tabel Acuan Simulasi (Horizon $H = 30$ tahun, $a = 0{,}5$):

| Bahan | Usia Nominal | Skor Kesesuaian | Usia Efektif ($L$) | Penggantian ($n$) & Waktu Kejadian | Total Emisi Relatif |
|---|---:|---:|---:|---|---:|
| Bahan A (Cocok) | 15 tahun | 100 | 15,0 tahun | 1 penggantian (pada tahun 15) | $\times 2$ (Awal + 1) |
| Bahan B (Kurang Cocok) | 15 tahun | 20 | 9,0 tahun | 3 penggantian (pada tahun 9, 18, 27) | $\times 4$ (Awal + 3) |
| Bahan C (Sangat Tahan) | 30 tahun | 100 | 30,0 tahun | 0 penggantian (tidak ada di $0 < t < 30$) | $\times 1$ (Awal saja) |
| Bahan D (Batas Horizon) | 15 tahun | 100 ($H=31$) | 15,0 tahun | 2 penggantian (pada tahun 15 dan 30) | $\times 3$ (Awal + 2) |

Penggantian tepat pada batas horizon ($t = H$) tidak dihitung sebagai beban dalam horizon bersangkutan. Pada grafik emisi kumulatif, kejadian penggantian interior ditampilkan sebagai lonjakan tangga, dan level kumulatif dipertahankan konstan hingga batas horizon $H$.

---

## 12. Penjadwal Pekerjaan Sensitif Cuaca — Logika Keputusan

### 12.1 Ambang Batas Keselamatan Cuaca Standar

| Jenis Pekerjaan | Parameter Cuaca | Ambang 'Tidak Layak' | Ambang 'Waspada' | Dasar Pertimbangan Teknis |
|---|---|---|---|---|
| **Pengecoran** | Probabilitas Hujan | $> 60\%$ | $40\% - 60\%$ | Hujan lebat merusak hidrasi semen dan mengikis permukaan beton segar. |
| **Pengecoran** | Curah Hujan Harian | $> 5{,}0\text{ mm}$ | $2{,}0 - 5{,}0\text{ mm}$ | Akumulasi air berlebih mengganggu rasio air-semen struktural. |
| **Pengecoran** | Suhu Maksimum | — | $> 35{,}0^\circ\text{C}$ | Suhu tinggi memicu penguapan kilat dan retak susut termal; perlu mitigasi curing basah. |
| **Pengecatan** | Kelembapan Maksimum | $> 85\%$ | $75\% - 85\%$ | Kelembapan tinggi menghalangi penguapan pelarut/air cat, lapisan menggelembung (*blistering*). |
| **Pengecatan** | Probabilitas Hujan | $> 40\%$ | $25\% - 40\%$ | Tetesan hujan merusak lapisan cat arsitektur yang belum kering sempurna (*washout*). |
| **Pengecatan** | Suhu Udara Harian | $< 10^\circ\text{C}$ atau $> 35^\circ\text{C}$ | — | Di luar rentang daya lekat dan waktu kering optimal cat dinding/atap. |

### 12.2 Prioritas Penentuan Ambang Batas

Evaluasi kelayakan menerapkan hierarki bertingkat:
1. **Override Spesifik Pekerjaan:** Jika pengguna memasukkan batas khusus pada form pekerjaan.
2. **Spesifikasi Produk Material:** Jika pekerjaan dikaitkan dengan material yang memiliki lembar data teknis spesifikasi pabrik.
3. **Ambang Asumsi Standar Konstruksi:** Nilai bawaan pada tabel §12.1 berlabel transparan sebagai asumsi generik.

### 12.3 Alur Evaluasi dan Penguncian Jadwal

1. Evaluasi harian mengembalikan status: `Layak` (hijau), `Waspada` (kuning), `TidakLayak` (merah), atau `DataTidakTersedia` (abu-abu).
2. Hari berstatus `TidakLayak` memuat alasan eksplisit pelanggaran parameter cuaca (contoh: *"Probabilitas hujan 70% melampaui batas aman pengecoran (60%)"*).
3. Pengguna dapat mengunci tanggal yang berstatus `Layak` atau `Waspada` secara langsung.
4. Jika pengguna memilih mengunci tanggal pada hari `TidakLayak`, sistem menampilkan dialog konfirmasi risiko: *"Kondisi cuaca berisiko tinggi terhadap mutu pekerjaan. Apakah Anda tetap ingin mengunci jadwal ini dengan menanggung risiko pengerjaan ulang?"*. Jika disetujui, jadwal terkunci dengan flag `persetujuan_risiko = true`.

---

## 13. Analisis Kompetitor

| Aspek Komparasi | One Click LCA / EC3 | EnergyPlus | Aplikasi RAB / Toko Bangunan | Wastu |
|---|---|---|---|---|
| **Target Utama** | Konsultan profesional & gedung komersial besar | Akademisi & insinyur fisika bangunan | Kontraktor & estimator anggaran | Pemilik rumah tinggal tapak & perencana awam |
| **Platform** | Berbasis web / SaaS berbayar | CLI / GUI desktop teknik | Aplikasi mobile / desktop lokal | Desktop Windows WPF mandiri |
| **Pertimbangan Iklim** | Tidak mengaitkan iklim dengan degradasi material | Pemodelan termal operasional mendalam | Tidak ada analisis iklim | Menghubungkan data iklim dengan ketahanan material |
| **Konsekuensi Jangka Panjang** | Analisis LCA cradle-to-grave standar | Beban energi pendingin ruangan | Perhitungan volume biaya saat ini | Simulasi penggantian dini dan emisi karbon berulang |
| **Penjadwalan Lapangan** | Tidak ada | Tidak ada | Jadwal kurva S berbasis biaya | Jadwal berbasis prakiraan cuaca harian 14 hari |

---

## 14. Matriks Lengkap Acceptance Criteria dan Penelusuran Kebutuhan

Matriks berikut memetakan seluruh **31 Acceptance Criteria F1–F7**, **17 Ketentuan Lintas Fitur X01–X17**, dan **40 Skenario Pengujian T01–T40** yang mengikat pelaksanaan paket implementasi WP01 sampai WP08.

### 14.1 F1 — Profil Proyek

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F1-01** | Pengguna dapat membuat, melihat, mengubah, dan menghapus nama proyek, kota, dan koordinat. Koordinat dapat dipilih otomatis dari kota atau diisi manual. | WP02 | T01, T02 | Terpetakan |
| **F1-02** | Proyek memiliki satu atau lebih komponen dinding/atap, masing-masing dengan luas positif ($>0$). Penolakan terhadap 0 komponen atau luas invalid. | WP02 | T01, T02 | Terpetakan |
| **F1-03** | Perubahan koordinat lokasi proyek memicu pengambilan ulang profil iklim, menginvalidasi cache lama, dan menandai jadwal untuk ditinjau. | WP03 | T07, T08, T31 | Terpetakan |
| **F1-04** | Validasi koordinat pada rentang inklusif lintang $[-11, 6]$ dan bujur $[95, 141]$. Penanganan data legacy aman tanpa crash migrasi. | WP02 | T03, T37 | Terpetakan |

### 14.2 F2 — Basis Data Bahan

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F2-01** | Properti nama, jenis, termal, ketahanan lembap/korosi, usia nominal, dan faktor emisi ($\text{kgCO}_2\text{e/m}^2$) tersedia lengkap dan valid. | WP02 | T15, T39 | Terpetakan |
| **F2-02** | Filter katalog berdasarkan jenis komponen (`dinding` atau `atap`) tanpa percampuran kategori. | WP02 | T14, T39 | Terpetakan |
| **F2-03** | Sumber rujukan dan kategori provenance (`ApiHistoris`, `Contoh`, `Manual`, `LegacyUnknown`) tercatat transparan per nilai. | WP02 | T39 | Terpetakan |
| **F2-04** | Administrator/pengembang dapat menambah bahan baru melalui database tanpa kompilasi ulang kode. | WP02 | T39 | Terpetakan |

### 14.3 F3 — Analisis Iklim Lokasi

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F3-01** | Pengambilan data iklim historis 10 tahun (curah hujan, kelembapan rata-rata, suhu rata-rata) dari API berdasarkan koordinat. | WP03 | T04, T09 | Terpetakan |
| **F3-02** | Kedekatan pesisir dihitung secara geometris terhadap garis pantai sebagai proksi risiko korosi (batas efektif 50 km). | WP03 | T10, T13 | Terpetakan |
| **F3-03** | Profil iklim disimpan per proyek (multiplisitas 0..1) dan dipakai ulang luring tanpa memanggil API kembali. | WP01, WP03 | T04, T06, T36 | Terpetakan |
| **F3-04** | Ringkasan profil iklim dan status kualitas data ditampilkan pada antarmuka sebelum perbandingan bahan. | WP03 | T04, T05 | Terpetakan |

### 14.4 F4 — Perbandingan Berperingkat

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F4-01** | Daftar bahan terurut menurun berdasarkan skor kesesuaian iklim (skor turun, emisi naik, tie-break stabil). | WP04 | T11, T12, T13 | Terpetakan |
| **F4-02** | Skor kesesuaian (0–100) dan emisi awal ($\text{kgCO}_2\text{e}$) ditampilkan berdampingan di setiap baris perbandingan. | WP04 | T15 | Terpetakan |
| **F4-03** | Setiap bahan menyertakan kalimat alasan penilaian yang mudah dipahami persona non-teknis. | WP04 | T11, T12 | Terpetakan |
| **F4-04** | Rilis MVP mengunci empat bobot kriteria pada nilai tetap 0,25 masing-masing. | WP04 | T16 | Terpetakan |
| **F4-05** | Rilis V2 menyediakan pengaturan bobot kustom tervalidasi ($\sum = 100\%$) dan reset, langsung merangking tanpa request iklim ulang. | WP04 | T16 | Terpetakan |
| **F4-06** | Ambang label rekomendasi konsisten: $< 55$ "Tidak disarankan", $55..74,999$ "Perlu pertimbangan", $\ge 75$ "Cocok". | WP04 | T17 | Terpetakan |

### 14.5 F5 — Simulasi Emisi Jangka Panjang

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F5-01** | Pengguna dapat memilih horizon simulasi (20, 30, 50 tahun); banyak simulasi disimpan persisten tanpa saling menimpa. | WP05 | T20 | Terpetakan |
| **F5-02** | Usia efektif $L$ dihitung dari usia nominal dan skor kesesuaian dengan parameter dasar penuaan $a = 0{,}5$. | WP05 | T18 | Terpetakan |
| **F5-03** | Jumlah penggantian dihitung strictly pada $0 < t < H$: $n = \max(0, \lceil H/L \rceil - 1)$ dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$. | WP05 | T18, T19 | Terpetakan |
| **F5-04** | Perbandingan visual disajikan dalam step chart interaktif yang menampilkan titik interior dan level akhir. | WP05 | T19 | Terpetakan |
| **F5-05** | Antarmuka menyatakan secara terbuka bahwa usia pakai efektif adalah estimasi model perhitungan. | WP05 | T18 | Terpetakan |

### 14.6 F6 — Penjadwal Pekerjaan Sensitif Cuaca

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F6-01** | Pendaftaran pekerjaan dengan jenis (pengecoran/pengecatan), rentang tanggal, dan relasi bahan/komponen opsional. | WP06 | T29 | Terpetakan |
| **F6-02** | Evaluasi prakiraan cuaca 14 hari per hari terhadap ambang teknis pekerjaan dengan kelembapan maksimum harian untuk cat. | WP06 | T24, T25, T26 | Terpetakan |
| **F6-03** | Status kelayakan multi-nilai: `Layak`, `Waspada`, `TidakLayak` (dengan alasan pelanggaran ambang), dan `DataTidakTersedia`. | WP06 | T24, T25, T27 | Terpetakan |
| **F6-04** | Pengguna dapat mengunci jadwal final; penguncian hari `TidakLayak` memerlukan konfirmasi persetujuan risiko eksplisit. | WP06 | T30 | Terpetakan |
| **F6-05** | Tanggal tanpa prakiraan (>14 hari) atau cuaca null/parsial diberi penjelasan eksplisit `Data tidak tersedia`. | WP06 | T27, T29 | Terpetakan |

### 14.7 F7 — Ekspor Laporan Rekomendasi

| ID | Acceptance Criteria Normatif | Paket | Skenario Uji | Status |
|---|---|---|---|---|
| **F7-01** | Berkas PDF memuat profil proyek, profil iklim, bahan terpilih+skor+emisi, simulasi emisi horizon, dan jadwal terkunci. | WP07 | T32, T33 | Terpetakan |
| **F7-02** | Laporan memuat tanggal pembuatan (UTC dan waktu lokal proyek) serta atribusi sumber data transparansi. | WP07 | T33 | Terpetakan |
| **F7-03** | Berkas PDF disimpan lokal, dicatat ke dalam database/state, dan dapat dibuka kembali dari aplikasi. | WP07 | T34, T35 | Terpetakan |

### 14.8 Ketentuan Lintas Fitur (X01 — X17)

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

### 14.9 Daftar Skenario Pengujian Wajib (T01 — T40)

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
- **T24:** Pengecoran dengan hujan $>5\text{ mm}$ atau probabilitas hujan $>60\%$ menghasilkan status `TidakLayak`; suhu $>35^\circ\text{C}$ menghasilkan `Waspada`.
- **T25:** Pengecatan dengan kelembapan $>85\%$ atau probabilitas $>40\%$ menghasilkan status `TidakLayak`.
- **T26:** Kelembapan maksimum harian digunakan untuk evaluasi pengecatan meskipun nilai rata-rata berada di bawah 85%.
- **T27:** Data cuaca bernilai null atau parsial menghasilkan status `DataTidakTersedia`, bukan nilai 0 atau Layak.
- **T28:** Override ambang pengecatan ke batas probabilitas 30% menyebabkan kondisi cuaca 35% berstatus `TidakLayak`.
- **T29:** Rentang pekerjaan melampaui 14 hari menampilkan seluruh hari dengan keterangan jelas untuk hari tanpa prakiraan.
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

---

## 15. Rencana Rilis Bertahap (MVP, V1, V2, dan Pasca-V2)

### MVP — Alur Inti Pemilihan Bahan Berbasis Iklim
- Skema database EF Core dan snapshot lokal luring (§9).
- Pustaka seed bahan $\ge 15$ dinding dan $\ge 10$ atap dengan faktor emisi $\text{kgCO}_2\text{e/m}^2$ (§F2).
- Klien Open-Meteo Historical Weather API dan kalkulasi jarak pesisir (§F3).
- Antarmuka manajemen proyek dan komponen (dinding saja, atap saja, atau keduanya) (§F1).
- Perbandingan berperingkat dengan skor kesesuaian iklim dan emisi awal berdampingan, **bobot kriteria tetap 0,25** (§F4).

### V1 — Konsekuensi Jangka Panjang dan Pelaksanaan Konstruksi
- Simulasi emisi jangka panjang (20, 30, 50 tahun) dengan fungsi penggantian terkoreksi $0 < t < H$ (§F5).
- Klien Open-Meteo Daily Forecast API dengan cache TTL 6 jam UTC dan penanganan zona waktu (§F6, §8).
- Penjadwal pekerjaan sensitif cuaca (pengecoran dan pengecatan) dengan evaluasi status multi-nilai dan dialog konfirmasi risiko penguncian (§F6, §12).

### V2 — Pelaporan Rekomendasi, Kustomisasi Bobot, dan Uji Pengguna
- Ekspor laporan rekomendasi PDF dan pembukaan riwayat laporan (§F7).
- Pembobotan kriteria kustom tervalidasi pada modul perbandingan bahan F4 (§F4, §11.1).
- Panel peninjauan asumsi perhitungan dan tombol reset ke default model (§11.4).
- Pelaksanaan pengujian pengguna kualitatif terhadap persona Bu Wulan dan Pak Herman untuk memvalidasi keterpahaman alasan rekomendasi (§4.1).

### Pasca-V2 — Rencana Pengembangan Lanjutan (Di Luar Scope Saat Ini)
- Kurasi faktor emisi dan basis data material spesifik produsen lokal Indonesia dengan sertifikasi EPD lokal.
- Integrasi data BMKG Open Data sebagai sumber pelengkap prakiraan cuaca lokal.
- Kalibrasi empiris fungsi penurunan usia pakai efektif material melalui riset penuaan material di iklim tropis.

---

## 16. Lampiran dan Keterkaitan dengan Diagram UML

### 16.1 Peran dan Pembagian Tugas Tim

| Anggota Tim | NIM | Peran Utama | Fokus Tanggung Jawab |
|---|---|---|---|
| **Dien Muhammad Scientivan Kurniapramono** | 24/533571/TK/59114 | Software Architect | Arsitektur sistem, integrasi layanan cuaca, persistensi data relasional, dan spesifikasi kontrak modul. |
| **Ramzi Alfito Rizky** | 24/540550/TK/60008 | Backend Developer | Layanan domain, logika perhitungan skor iklim, simulasi emisi jangka panjang, dan pengujian unit domain. |
| **Yohanes Anthony Saputra** | 24/536327/TK/59524 | Frontend Developer | Antarmuka pengguna WPF MVVM, modul penjadwal cuaca, pembuatan laporan PDF, dan pengalaman interaksi pengguna. |

### 16.2 Keterkaitan dengan Diagram UML

Seluruh spesifikasi pada dokumen ini selaras secara normatif dengan diagram UML pada [`docs/uml-wastu.md`](uml-wastu.md) dan berkas sumber visual [`docs/uml-wastu.drawio`](uml-wastu.drawio):

1. **Use Case Diagram:**
   - Menghapus use-case login pengguna (Wastu adalah aplikasi desktop single-user).
   - Menghubungkan aktor sistem Layanan Cuaca ke dua use case: `Analisis Iklim Lokasi` (data historis 10 tahun) dan `Ambil Prakiraan Cuaca` (prakiraan harian 14 hari).
2. **Activity Diagram:**
   - Keputusan "Ubah kriteria/bobot?" mengarahkan alur kembali langsung ke aktivitas "Hitung skor kesesuaian dan kgCO2e", bukan membuat proyek ulang dari awal.
   - Penanganan cuaca harian mencakup penanganan "Data tidak tersedia" serta jalur persetujuan risiko untuk penguncian tanggal berstatus "Tidak layak".
3. **Class Diagram & Domain Model:**
   - Menghilangkan metode `Pengguna.login()`.
   - Mengubah multiplisitas `Proyek` ke `ProfilIklim` menjadi `0..1` (profil iklim bernilai null sebelum berhasil diambil).
   - Mengubah multiplisitas `Proyek` ke `Komponen` menjadi `1..*` (proyek dapat memuat dinding saja, atap saja, atau keduanya; minimal 1 komponen aktif).
   - Mengubah multiplisitas `Evaluasi` ke `SimulasiEmisi` menjadi `1..*` (mendukung banyak horizon simulasi 20, 30, 50 tahun yang disimpan secara persisten).
   - Menyelaraskan identitas entitas menggunakan tipe `Guid` konsisten.
