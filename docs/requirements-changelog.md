# Catatan Perubahan Kebutuhan Wastu (PRD-R01 — PRD-R11)

Dokumen ini mencatat rekonsiliasi spesifikasi kebutuhan produk Wastu antara naskah PRD awal, temuan implementasi baseline, dan keputusan rencana revisi normatif (PRD-R01 sampai PRD-R11). Seluruh keputusan pada dokumen ini mengikat implementasi paket kerja selanjutnya (WP01 sampai WP08) tanpa mengurangi cakupan acceptance criteria produk F1–F7.

---

## Ringkasan Matriks Koreksi PRD-R01 — PRD-R11

| ID | Topik Koreksi | Bagian Lama (Masalah) | Bagian Baru (Kontrak Normatif) | Area Kode & Dokumen Terdampak |
|---|---|---|---|---|
| **PRD-R01** | Tahapan Rilis & Bobot Kriteria | PRD §5.1 mencampur F5–F7 ke MVP; §11.1 menyebut bobot kustom tanpa tahapan rilis. | §5.1 menjadi scope produk penuh target V2. Tahapan rilis: MVP = F1–F4 (bobot tetap 0,25), V1 = F5–F6, V2 = F7, bobot kustom tervalidasi, dan uji pengguna. | PRD §5.1, §6 (F4), §11.1, §15; `KonfigurasiSkoring.cs`, `KriteriaBobot.cs`, `ComparePage.xaml.cs`. |
| **PRD-R02** | Rumus Penggantian & Batas Horizon | Rumus narasi PRD tertulis `floor(H/L)` dengan contoh kontradiktif (L=9/H=30 disebut 2 padahal 3 kejadian). Domain baseline sudah `ceil - 1`, tetapi UI menduplikasi dengan epsilon. | Pemasangan awal $t=0$. Penggantian dihitung strictly pada $0 < t < H$: $n = \max(0, \lceil H/L \rceil - 1)$. Contoh L=9, H=30: $n=3$, emisi total $\times 4$. Titik $t=H$ tidak memicu penggantian baru. Diseragamkan di semua layer. | PRD §11.3, §15; `PerhitunganSimulasi.cs`, `EmissionSimulationService.cs`, `EmissionChart.cs`, `SimulationPage.xaml`, `tests/Wastu.Domain.Tests/Emissions/PerhitunganSimulasiTests.cs`. |
| **PRD-R03** | Satuan Faktor Emisi Karbon | F2 menyebut faktor per luas atau volume, namun §9 dan §11.2 langsung mengalikan luas ($m^2$). Nilai per $m^3$/$kg$ tidak kompatibel fisik. | Nilai perhitungan wajib dinormalisasi ke $\text{kgCO}_2\text{e/m}^2$ untuk susunan/ketebalan spesifik. Konversi dari $m^3$ atau $kg$ wajib mencatat asumsi ketebalan/densitas dan sumber data. | PRD §6 (F2), §9, §11.2; `Bahan.cs`, `MaterialService.cs`, `seed_supabase.sql`, katalog bahan. |
| **PRD-R04** | Monotonisitas Skor & Simulasi (Plateau) | §4.2 menuntut setiap penurunan usia efektif menaikkan emisi. Padahal fungsi tangga memiliki plateau. | Penurunan usia efektif tidak boleh menurunkan emisi kumulatif ($n$ monoton tidak turun). Penurunan beban/iklim tidak menurunkan skor ketahanan; plateau dijelaskan pada batas saturasi model. | PRD §4.2, §11.1, §11.3; `PerhitunganSkor.cs`, `PerhitunganSimulasi.cs`, pengujian unit domain. |
| **PRD-R05** | Ambang Cuaca & Spesifikasi Bahan | §1 menyebut ambang diturunkan otomatis dari bahan; §12 hanya memuat nilai generik per pekerjaan. | Penjadwal menggunakan ambang jenis pekerjaan dengan urutan: Override Spesifikasi Produk $\rightarrow$ Default Asumsi Generik. Status Waspada bawaan hanya untuk suhu pengecoran $> 35^\circ\text{C}$ tanpa pelanggaran keras; nilai tepat pada batas tidak melanggar. Bukan standar tervalidasi. | PRD §1, §6 (F6), §12; `Pekerjaan.cs`, `PrakiraanCuaca.cs`, `WorkSchedulingService.cs`, UI Penjadwal. |
| **PRD-R06** | Penyimpanan Remote, Snapshot & Mode Luring | PRD hanya berasumsi PostgreSQL lokal; baseline memakai remote Supabase dan JSON fallback. Gagal koneksi mematikan aplikasi. | PostgreSQL adalah penyimpanan utama (lokal/remote). Mode remote wajib memiliki snapshot lokal atomik untuk peninjauan luring read-only. Penolakan konfigurasi parsial diperjelas. Mode JSON lama dipertahankan untuk kompatibilitas. | PRD §5.2, §7, §8.4, §9; `AppState.cs`, `StateStore.cs`, `PostgreSqlBackend.cs`, `App.xaml.cs`. |
| **PRD-R07** | Bounding Box Koordinat Indonesia | Bounding box inkonsisten: kode 94–142°BT vs PRD 95–141°BT. Validasi diklaim sebagai batas administratif presisi. | Standarisasi rentang inklusif lintang -11 s/d 6, bujur 95 s/d 141 sebagai filter kasar area layanan Wastu. Data legacy 94–95° / 141–142° diaudit aman tanpa crash migrasi. | PRD §6 (F1), §9; `Proyek.cs`, `Projects/ProjectService.cs`, dialog proyek, skema migrasi. |
| **PRD-R08** | TTL Cache Prakiraan & Zona Waktu | Cache prakiraan JSON tidak memiliki TTL terstruktur; tanggal cuaca mengikuti jam komputer klien Windows. | Cache ber-TTL default 6 jam (X04) berbasis timestamp UTC `fetched_at`. Tanggal harian diinterpretasikan menurut zona waktu lokasi proyek dari respons penyedia cuaca. Data di luar rentang / tidak tersedia hanya draft (F6-05). | PRD §8.4, §12; `OpenMeteoDailyForecastClient.cs`, `WorkSchedulingService.cs`, entitas cache cuaca. |
| **PRD-R09** | Transparansi Provenance Data | Seed contoh menggunakan `now()` tanpa membedakan data API historis riil, contoh, atau estimasi manual. | Provenance material: sumber properti + status perkiraan/terverifikasi tanpa wajib label ICE/EPD bila tak tertelusuri. Provenance iklim: `ApiHistoris`, `EstimasiBawaan`, `CacheHistoris`, `LegacyUnknown`. | PRD §6 (F2), §8.4, §9, §11.4; `ProfilIklim.cs`, `Bahan.cs`, `Laporan.cs`, schema migrasi. |
| **PRD-R10** | Ambang Label, GUID, Skema & UML | Ambang UI 55 vs domain 50; UML mewajibkan login; multiplisitas profil 1..1 dan simulasi 0..1 kaku; tabel fisik Indonesia belum dipetakan. | Label terpusat: `< 55` Tidak disarankan, `55..74,999` Perlu pertimbangan, `≥ 75` Cocok. GUID konsisten. Multiplisitas: Profil `0..1`, Komponen `1..*`, Evaluasi ke SimulasiEmisi `0..*`. Hapus login dari UML. | PRD §9, §11.1, §16.3; `docs/uml-wastu.md`, `docs/uml-wastu.drawio`, `docs/implementasi-class-diagram.md`. |
| **PRD-R11** | Model Asumsi Berversi & Metrik Pengujian | Parameter perhitungan tersembunyi; target performa (<3 mnt, <2 dtk, API ≥95%) belum berdasar instrumen ukur riil. | Seluruh parameter perhitungan (hujan, termal, korosi, penuaan $a=0,5$, ambang) berversi dan dapat di-reset. Metrik performa diuji melalui fixture dan uji pengguna kualitatif persona. | PRD §4.1, §4.2, §11.1, §11.3, §11.4; `KonfigurasiSkoring.cs`, panel asumsi, laporan pengujian. |

---

## Rincian Perubahan per Item

### PRD-R01 — Tahapan Rilis dan Pembobotan Kriteria

- **Bagian Lama:**
  - Dokumen PRD §5.1 mencantumkan simulasi emisi (F5), penjadwal cuaca (F6), dan ekspor laporan (F7) di dalam scope MVP.
  - Namun di §15 (Rencana Rilis Bertahap), F5 dan F6 dialokasikan pada V1, sedangkan F7 dan pembobotan kriteria dialokasikan pada V2.
  - Dokumen PRD §11.1 menyatakan bobot kriteria dapat diubah bebas oleh pengguna tanpa menegaskan tahapan rilisnya.
- **Bagian Baru (Kontrak Normatif):**
  - §5.1 diubah judulnya menjadi "Scope Produk F1–F7 (Target Penuh Sampai V2) dan Pemisahan Tahapan".
  - Pembagian rilis didefinisikan secara tegas:
    - **MVP:** F1 (Profil Proyek & Komponen), F2 (Basis Data Bahan), F3 (Analisis Iklim Lokasi), F4 (Perbandingan Berperingkat dengan bobot tetap 0,25 masing-masing).
    - **V1:** F5 (Simulasi Emisi Jangka Panjang) dan F6 (Penjadwal Pekerjaan Sensitif Cuaca).
    - **V2:** F7 (Ekspor Laporan Rekomendasi PDF), kontrol pembobotan kriteria kustom (dengan validasi jumlah 100%, non-negatif, finite, serta tombol reset ke default), dan uji pengguna nyata.
  - Seluruh rencana revisi Wastu menargetkan **cakupan penuh hingga V2**.
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/KriteriaBobot.cs`: Menjaga invariant bobot default MVP bernilai tepat 0,25 per kriteria.
  - `Views/Pages/ComparePage.xaml` dan `Views/Pages/ComparePage.xaml.cs`: Menonaktifkan slider bobot pada mode MVP, dan mengaktifkannya pada mode V2 dengan validasi input instan tanpa memanggil ulang API iklim.

---

### PRD-R02 — Rumus Penggantian Bahan dan Batas Horizon

- **Bagian Lama:**
  - Rumus jumlah penggantian pada naskah narasi PRD tertulis `jumlah_penggantian = floor(horizon_tahun / usia_pakai_efektif)`.
  - Terjadi kontradiksi internal pada teks PRD: untuk $H=30, L=15$, rumus menghasilkan 2, namun narasi menyatakan 1 penggantian. Untuk $H=30, L=9$, narasi menyebut 2 penggantian padahal penggantian terjadi pada tahun 9, 18, dan 27 (3 penggantian).
  - Pada implementasi baseline: domain (`src/Wastu.Domain/Emissions/PerhitunganSimulasi.cs`) sesungguhnya telah menerapkan rumus `(int)Math.Ceiling(h / l) - 1`, namun lapisan antarmuka UI memiliki duplikasi kode terpisah dengan penanganan floating-point epsilon.
- **Bagian Baru (Kontrak Normatif):**
  - Pemasangan awal terjadi saat bangunan dibangun ($t=0$).
  - Penggantian hanya dihitung pada waktu kejadian $t$ yang memenuhi $0 < t < H$. Penggantian tepat pada batas akhir horizon ($t=H$) tidak dihitung dalam horizon tersebut.
  - Rumus matematis normatif diseragamkan di seluruh layer (domain, antarmuka, dan dokumen):
    $$\begin{aligned}
    f(s) &= a + (1 - a) \times \frac{s}{100}, \quad \text{default } a = 0{,}5 \\
    L &= \text{usia\_nominal} \times f(s) \\
    n &= \max\left(0, \left\lceil \frac{H}{L} \right\rceil - 1\right) \\
    \text{emisi\_kumulatif} &= \text{emisi\_awal} \times (1 + n)
    \end{aligned}$$
  - Tabel kasus acuan terverifikasi:
    - Nominal 15 thn, Skor 100 $\rightarrow L=15$, Horizon 30 thn $\rightarrow n = \max(0, \lceil 30/15 \rceil - 1) = 1$ penggantian (thn 15), total emisi $\times 2$.
    - Nominal 15 thn, Skor 20 $\rightarrow L=9$, Horizon 30 thn $\rightarrow n = \max(0, \lceil 30/9 \rceil - 1) = 3$ penggantian (thn 9, 18, 27), total emisi $\times 4$.
    - Nominal 15 thn, Skor 100, Horizon 15 thn $\rightarrow n = 0$ penggantian, total emisi $\times 1$.
    - Nominal 15 thn, Skor 100, Horizon 31 thn $\rightarrow n = 2$ penggantian (thn 15 dan 30), total emisi $\times 3$.
  - Grafik emisi kumulatif menampilkan titik kejadian penggantian interior ($t < H$) dan mempertahankan level kumulatif hingga akhir horizon ($t=H$).
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/Emissions/PerhitunganSimulasi.cs`: Kontrak normatif $\max(0, \lceil H/L \rceil - 1)$ dengan presisi numerik konsisten.
  - `Controls/EmissionChart.cs`: Penyelarasan titik step chart pada grafik emisi kumulatif.
  - `tests/Wastu.Domain.Tests/Emissions/PerhitunganSimulasiTests.cs`: Jalur pengujian unit domain aktual yang memverifikasi kasus acuan baru.

---

### PRD-R03 — Standardisasi Satuan Faktor Emisi Karbon

- **Bagian Lama:**
  - Acceptance criteria F2 mengizinkan faktor emisi per satuan luas atau volume (misal $\text{kgCO}_2\text{e/m}^3$ atau $\text{kgCO}_2\text{e/kg}$).
  - Namun skema basis data (§9) dan rumus §11.2 langsung mengalikan `emission_factor` dengan luas komponen ($m^2$), yang menyebabkan kesalahan dimensi fisik apabila data bersumber per $m^3$ atau per $kg$.
- **Bagian Baru (Kontrak Normatif):**
  - Seluruh faktor emisi yang masuk ke kalkulasi domain **wajib bernilai $\text{kgCO}_2\text{e/m}^2$** untuk satu susunan/ketebalan spesifik yang dinyatakan.
  - Satuan $\text{kgCO}_2\text{e/m}^2$ ditampilkan secara konsisten pada katalog, pembanding, simulasi, dan laporan PDF.
  - Untuk data mentah:
    - Bersumber per $m^3$: $\text{faktor}_{\text{m}^2} = \text{faktor}_{\text{m}^3} \times \text{ketebalan (m)}$.
    - Bersumber per $kg$: $\text{faktor}_{\text{m}^2} = \text{faktor}_{\text{kg}} \times \text{massa}_{\text{m}^2}$ (atau $\text{faktor}_{\text{kg}} \times \text{densitas} \times \text{ketebalan}$).
  - Nilai asal, satuan sumber, asumsi ketebalan, dan metode konversi disimpan dalam metadata transparansi. Data tanpa dasar konversi yang valid tidak boleh diasumsikan sebagai per $m^2$.
- **Dampak Kode & Area Terdampak:**
  - `database/02_schema.sql`, `database/03_seed_supabase.sql`: Metadata susunan/ketebalan material dan normalisasi angka faktor emisi.
  - `src/Wastu.Domain/Bahan.cs`: Penambahan validasi kelayakan faktor emisi dan dokumentasi satuan.
  - `Views/Pages/MaterialsPage.xaml`: Penjelasan satuan dan susunan material pada panel detail katalog.

---

### PRD-R04 — Monotonisitas Skor dan Simulasi Emisi (Karakteristik Plateau)

- **Bagian Lama:**
  - PRD §4.2 menyatakan bahwa setiap penurunan usia pakai efektif harus menghasilkan peningkatan emisi kumulatif.
  - Pernyataan ini melupakan fakta bahwa fungsi jumlah penggantian adalah fungsi tangga (*step function* integer).
  - Skor ketahanan lembap lama juga cepat jenuh (saturasi) pada curah hujan sedang.
- **Bagian Baru (Kontrak Normatif):**
  - Penurunan usia pakai efektif bersifat **monoton tidak turun** terhadap emisi kumulatif: usia pakai yang lebih pendek tidak boleh menghasilkan emisi yang lebih rendah, *ceteris paribus*.
  - Skenario pengujian wajib menguji batas tangga penggantian.
  - Pada perhitungan skor: peningkatan ketahanan bahan tidak boleh menurunkan skor; peningkatan keparahan iklim tidak boleh menaikkan skor. Daerah plateau pada saat kondisi bahan sudah melampaui kebutuhan dinyatakan secara terbuka sebagai karakteristik saturasi model.
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/Scoring/PerhitunganSkor.cs`: Verifikasi kondisi batas dan sifat monoton non-increasing terhadap beban iklim.
  - `src/Wastu.Domain/Emissions/PerhitunganSimulasi.cs`: Penegasan batas plateau pada step function emisi.
  - Suite pengujian unit domain: Pengujian properti monotonisitas skor dan emisi.

---

### PRD-R05 — Hubungan Bahan dan Ambang Batas Penjadwalan Cuaca

- **Bagian Lama:**
  - PRD §1 menyatakan ambang penjadwalan cuaca diturunkan dari karakteristik bahan.
  - Namun pada §12, PRD hanya memuat ambang generik per jenis pekerjaan (pengecoran dan pengecatan). Properti konduktivitas termal dan ketahanan lembap tidak dapat dikonversi langsung menjadi syarat curing beton atau pengeringan cat.
- **Bagian Baru (Kontrak Normatif):**
  - Penjadwal cuaca menggunakan lokasi proyek dan profil ambang per jenis pekerjaan.
  - Pekerjaan dapat memiliki relasi opsional (`0..1`) ke `Komponen` dan `Bahan` proyek; pengguna dapat memasukkan nilai *override* ambang berdasarkan lembar data teknis spesifikasi produk material.
  - Jika spesifikasi produk tidak dimasukkan, aplikasi menggunakan ambang generik bawaan yang berlabel jelas sebagai "Asumsi Generik Perancangan" (bukan standar keselamatan atau konstruksi tervalidasi).
  - Batas default status:
    - **Pengecoran:** peluang hujan $\le 60\%$, curah hujan $\le 5\text{ mm}$, suhu kerja $\le 35^\circ\text{C}$. Pelanggaran hujan menyebabkan `TidakLayak`. Kondisi hujan aman namun suhu $> 35^\circ\text{C}$ menghasilkan status `Waspada`.
    - **Pengecatan:** peluang hujan $\le 40\%$, kelembapan relatif $\le 85\%$, suhu kerja antara $10^\circ\text{C}$ s/d $35^\circ\text{C}$. Pelanggaran salah satu ambang menyebabkan `TidakLayak`.
    - Nilai metrik tepat pada batas ambang (60, 5, 85, 40, 10, 35) tidak melanggar (berstatus `Layak`).
  - Evaluasi cuaca mengembalikan objek terstruktur (`HasilEvaluasiCuaca`) yang memuat status kelayakan **DAN seluruh daftar alasan/peringatan** yang terpicu (bukan sekadar enum tunggal tanpa konteks).
  - Penguncian tanggal hanya berlaku dalam rentang 14 hari prakiraan dan **wajib menolak tanggal yang sudah lewat** menurut zona waktu lokasi proyek. Penguncian data lama ($\ge 6$ jam) memerlukan konfirmasi eksplisit; data hilang/parsial hanya disimpan sebagai draf (F6-05).
  - Perubahan lokasi, ambang, bahan, atau prakiraan menandai jadwal lama sebagai "Perlu ditinjau" tanpa menghapus keputusan penguncian.
- **Dampak Kode & Area Terdampak:**
  - `Models/Pekerjaan.cs` dan `src/Wastu.Domain/Weather/AmbangLayakKerja.cs`: Evaluasi kelayakan dengan batas eksak, status multi-nilai, dan alasan terstruktur.
  - `Views/Pages/SchedulePage.xaml`: UI pengaturan ambang dengan indikator sumber ambang (Override Produk vs Asumsi Generik) dan penolakan tanggal lewat.

---

### PRD-R06 — Penyimpanan Utama PostgreSQL, Sinkronisasi Remote, dan Peninjauan Luring

- **Bagian Lama:**
  - Dokumen PRD mengasumsikan PostgreSQL berjalan sebagai instans lokal tunggal di komputer pengguna.
  - Baseline kode memperkenalkan koneksi PostgreSQL remote (Supabase) dan mode JSON kompatibilitas.
  - Bila koneksi database remote terputus saat aplikasi dibuka, aplikasi langsung mengalami crash atau tertutup.
- **Bagian Baru (Kontrak Normatif):**
  - PostgreSQL tetap menjadi penyimpanan relasional utama (baik lokal maupun remote).
  - Konfigurasi remote diverifikasi utuh; konfigurasi parsial ditolak secara eksplisit dengan mengarahkan ke panduan konfigurasi.
  - Pada mode remote, aplikasi wajib menyimpan **snapshot lokal atomik** per profil koneksi setiap kali transaksi berhasil.
  - Jika koneksi remote gagal saat startup, aplikasi **tidak boleh tertutup**, melainkan membuka snapshot lokal dalam mode **read-only** dengan banner penjelasan waktu sinkronisasi terakhir, atau menampilkan halaman koneksi/pemulihan dengan tombol coba lagi.
  - Mode luring remote bersifat read-only (tidak ada sinkronisasi multi-master yang rumit pada rilis ini).
  - Mode penyimpanan JSON lama (`state.json` / `state-postgres.json`) dipertahankan sebagai opsi kompatibilitas dengan indikator penyimpanan lokal yang jujur.
- **Dampak Kode & Area Terdampak:**
  - `Services/AppState.cs`, `Services/StateStore.cs`: Manajemen siklus hidup snapshot atomik dan fallback mode.
  - `Services/PostgreSqlBackend.cs`: Penanganan error koneksi graceful, isolasi transaksi, dan pembuatan snapshot lokal.
  - `App.xaml.cs`, `Views/MainWindow.xaml`: UI status koneksi, banner luring read-only, dan pencegahan force-exit aplikasi.

---

### PRD-R07 — Standardisasi Bounding Box Koordinat Wilayah Indonesia

- **Bagian Lama:**
  - Validasi koordinat di kode baseline menggunakan bujur 94° s/d 142° BT, sedangkan naskah lampiran PRD menetapkan 95° s/d 141° BT.
  - PRD mengklaim validasi koordinat memastikan lokasi berada di dalam wilayah hukum Indonesia, padahal kotak pembatas (*bounding box*) juga mencakup wilayah perairan internasional dan sebagian wilayah negara tetangga.
- **Bagian Baru (Kontrak Normatif):**
  - Standarisasi rentang inklusif wilayah layanan Wastu:
    - **Lintang:** $-11{,}0^\circ$ sampai $+6{,}0^\circ$
    - **Bujur:** $+95{,}0^\circ$ sampai $+141{,}0^\circ$
  - Ketentuan ini diseragamkan pada domain, validasi dialog proyek, skema migrasi database, dan pengujian unit.
  - Antarmuka menjelaskan bahwa batasan ini adalah batas koordinat kotak wilayah layanan Wastu, bukan verifikasi batas administratif batas negara presisi (*reverse geocoding* administratif di luar scope).
  - **Migrasi Data Legacy:** Koordinat legacy yang berada pada rentang 94°–95° atau 141°–142° diaudit secara aman dan ditandai "Perlu peninjauan", tanpa penghapusan diam-diam atau pelemparan exception yang menggagalkan migrasi.
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/Proyek.cs`, `src/Wastu.Domain/Projects/ProjectService.cs`: Validasi rentang $[-11, 6]$ dan $[95, 141]$.
  - `src/Wastu.Domain/Komponen.cs`: Mempertahankan validasi constructor luas $> 0$ (menolak non-finite dan $\le 0$) seraya mendukung fleksibilitas komponen opsional (dinding saja atau atap saja).
  - `database/02_schema.sql`: Constraint `CHECK (latitude BETWEEN -11.0 AND 6.0 AND longitude BETWEEN 95.0 AND 141.0)`.
  - Dialog proyek antarmuka: Pesan kesalahan validasi input koordinat yang informatif dan ramah pengguna.

---

### PRD-R08 — Durasi TTL Cache Prakiraan Cuaca dan Zona Waktu Proyek

- **Bagian Lama:**
  - Cache prakiraan JSON tidak memiliki durasi kedaluwarsa (TTL) yang terstruktur.
  - Evaluasi tanggal prakiraan menggunakan jam lokal komputer klien Windows (`DateTime.Today`), yang dapat menyebabkan pergeseran hari apabila komputer berada di zona waktu berbeda dari lokasi proyek.
- **Bagian Baru (Kontrak Normatif):**
  - Durasi TTL default cache prakiraan ditetapkan **6 jam** (X04), dihitung dari timestamp UTC `fetched_at`. Nilai TTL dapat dikonfigurasi.
  - Permintaan penjadwal dalam rentang masa berlaku 6 jam wajib memanfaatkan cache tanpa memanggil API cuaca kembali.
  - Data tanggal harian diinterpretasikan menurut **zona waktu lokasi proyek** dari respons penyedia API cuaca.
  - Penanganan data cuaca hilang/parsial atau di luar rentang 14 hari diatur pada **F6-05**: data tersebut hanya dapat disimpan sebagai draf, tidak dapat dikunci secara normal, dan diberi status `DataTidakTersedia`.
  - Data kedaluwarsa (> 6 jam) tetap dapat ditinjau dengan peringatan data lama, dan pengunciannya membutuhkan konfirmasi khusus. Penguncian tanggal lewat menurut zona waktu proyek wajib ditolak.
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/Abstractions/WeatherClients.cs`: Penyimpanan timestamp UTC dan metadata zona waktu proyek.
  - `src/Wastu.Domain/Services/ServiceContracts.cs` (`IWorkSchedulingService`) dan `Models/Pekerjaan.cs`: Evaluasi tanggal kerja berbasis zona waktu proyek dan validasi TTL cache.
  - `Views/Pages/SchedulePage.xaml`: Indikator umur data cuaca dan banner peringatan cache kedaluwarsa.

---

### PRD-R09 — Transparansi Sumber dan Provenance Data

- **Bagian Lama:**
  - Properti material dan profil iklim contoh diisi tanpa metadata sumber per properti.
  - Data seed contoh menggunakan timestamp `now()`, sehingga sistem dan pengguna tidak dapat membedakan mana data hasil pengambilan API riil dan mana data simulasi contoh.
- **Bagian Baru (Kontrak Normatif):**
  - **Pemisahan Provenance Material vs Iklim:**
    - **Material (F2-03):** Menyimpan sumber rujukan properti (misal: lembar data teknis produsen, SNI, publikasi literatur) dan status verifikasi (`Perkiraan` vs `Terverifikasi`). Tidak dipaksakan melabeli ICE/EPD apabila rantai asal-usulnya tidak dapat diverifikasi secara tuntas, dan tidak menggunakan enum API iklim.
    - **Iklim (F3-01):** Menyimpan kategori provenance iklim terpisah yang eksplisit:
      - `ApiHistoris`: Data yang benar-benar diambil dari layanan API iklim Open-Meteo.
      - `EstimasiBawaan`: Data aproksimasi atau estimasi default bawaan sistem.
      - `CacheHistoris`: Kualitas penggunaan profil iklim tersimpan dari API yang masih berlaku.
      - `LegacyUnknown`: Data historis lama yang asal-usulnya tidak dapat diverifikasi secara tuntas.
      - Sumber pendukung: `SumberContoh` dan `ManualOverride`.
    - **Pemisahan Provenance vs Status Operasional:** Provenance mencatat riwayat asal data, sedangkan status proses saat ini (`Mengambil`, `GagalMemperbarui`, `BelumTersedia`) adalah status operasional sementara. Ketika refresh API gagal, status menjadi `GagalMemperbarui` dan provenance profil tersimpan tetap terjaga (tidak diubah secara palsu).
  - Laporan PDF dan antarmuka mencantumkan keterbatasan data internasional dan asumsi proksi jarak pesisir secara transparan.
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/Bahan.cs`: Properti `SumberData` dan `StatusVerifikasi`.
  - `src/Wastu.Domain/ProfilIklim.cs`: Kategori provenance iklim (`ApiHistoris`, `EstimasiBawaan`, `CacheHistoris`, `LegacyUnknown`) terpisah dari status operasional pembaruan.
  - `src/Wastu.Data/Entities/`: Kolom persistensi provenance iklim dan bahan.

---

### PRD-R10 — Ambang Batas Label, Konsistensi GUID, Pemetaan Skema Fisik, dan Sinkronisasi UML

- **Bagian Lama:**
  - Terdapat perbedaan ambang rekomendasi: UI menggunakan angka 55, sedangkan sebagian konstanta domain menggunakan 50.
  - Terdapat inkonsistensi tipe ID (integer auto-increment pada UI lama vs GUID pada domain/database EF).
  - Skema database fisik menggunakan Bahasa Indonesia (`bangunan`, `bahan`, `komponen`), sementara PRD awal memuat contoh SQL Bahasa Inggris (`projects`, `materials`).
  - Diagram UML historis memuat use-case login pengguna (`Pengguna.login()`), multiplisitas profil iklim $1..1$ (padahal proyek baru belum tentu langsung memiliki profil iklim), dan multiplisitas evaluasi ke simulasi $0..1$ kaku (tidak mendukung riwayat simulasi berbagai horizon).
- **Bagian Baru (Kontrak Normatif):**
  - Standarisasi ambang label rekomendasi terpusat (berbasis floating-point sejati):
    - Skor $< 55{,}0$: **Tidak disarankan**
    - $55{,}0 \le \text{Skor} < 75{,}0$: **Perlu pertimbangan**
    - Skor $\ge 75{,}0$: **Cocok**
    - Aturan tie-break: jika skor sama, urutkan berdasarkan emisi terendah; jika emisi sama, urutkan alfabetis nama bahan.
  - Tipe entitas domain dan persistensi database diseragamkan menggunakan `Guid`. ID integer lama pada `Models/` dipetakan secara terpisah dalam masa transisi (`Models.Pekerjaan` baseline sama sekali tidak memiliki properti ID; `Models.StatusKelayakan` sudah 3 nilai: `Layak`, `Waspada`, `TidakLayak`).
  - Skema fisik tabel PostgreSQL Bahasa Indonesia ditetapkan sebagai kontrak resmi aplikasi: baseline 6 tabel fisik (`bangunan`, `bahan`, `komponen`, `profil_iklim`, `evaluasi`, `simulasi_emisi`) plus tabel target perluasan (`pekerjaan`, `prakiraan_cache`, `laporan`).
  - Penyelarasan UML normatif:
    - Menghapus kewajiban login dari UML use-case, activity, dan class diagram; entitas `Pengguna` dijelaskan sebagai representasi konteks aktor lokal historis pada aplikasi desktop Wastu tanpa tabel akun/kredensial database.
    - Menghubungkan aktor `Pemilik Rumah` ke use case `Simulasi Emisi Karbon` (UC5).
    - Multiplisitas `Proyek` ke `ProfilIklim` menjadi `0..1`.
    - Multiplisitas `Proyek` ke `Komponen` menjadi `1..*` (proyek dapat berisi dinding saja, atap saja, atau keduanya; minimal 1 komponen aktif). Constructor `Domain.Komponen` sudah memvalidasi luas $> 0$ (menolak non-finite/$\le 0$).
    - Multiplisitas `Evaluasi` ke `SimulasiEmisi` menjadi `0..*` (evaluasi baru/draft boleh belum memiliki hasil simulasi emisi, dan setelah dieksekusi dapat menyimpan simulasi horizon 20, 30, 50 tahun secara simultan).
    - Menambahkan relasi opsional `Pekerjaan` ke `Komponen` (`0..1`) dan `Bahan` (`0..1`).
    - Evaluasi cuaca mengembalikan `HasilEvaluasiCuaca` (status kelayakan DAN seluruh daftar alasan/peringatan).
    - Kontrak service aktual (§10) dipertahankan inventaris metodenya: `GetAnnualProfileAsync`, `GetForecastAsync`, `RankAsync`, `EvaluateDaysAsync`, `LockDateAsync`, `ExportAsync`; penambahan tanggung jawab seperti metadata, persistensi setting, list report, dan pruning cache dijelaskan sebagai evolusi target tanpa mengganti nama metode aktual secara sembarang.
- **Dampak Kode & Area Terdampak:**
  - `docs/uml-wastu.md`, `docs/uml-wastu.drawio`: Selaras penuh dengan naskah kontrak normatif, aktor lokal, relasi opsional pekerjaan, dan multiplisitas `0..*`.
  - `docs/implementasi-class-diagram.md`: Penjelasan pemisahan tegas baseline `Models/` vs target WP01–WP08.
  - `src/Wastu.Domain/Scoring/KonfigurasiSkoring.cs`: Ambang batas 55/75 terpusat.

---

### PRD-R11 — Parameter Model Asumsi Berversi dan Metrik Keberhasilan Pengujian

- **Bagian Lama:**
  - Nilai-nilai asumsi perhitungan (seperti pita konduktivitas termal, faktor penuaan $a=0{,}5$, acuan curah hujan lembap, batas proksi korosi 50 km) tertanam secara hardcoded tanpa dokumentasi parameter.
  - PRD §4 menetapkan target kuantitatif (waktu alur < 3 menit, respon antarmuka < 2 detik, ketersediaan API cuaca $\ge 95\%$) tanpa metodologi pengukuran dan instrumen uji yang nyata.
- **Bagian Baru (Kontrak Normatif):**
  - **Formulasi Perhitungan Lengkap & Transparan:**
    - **Termal:** Interpolasi linier kontinu pada pita 26–32°C. Bahan isolator unggul dengan konduktivitas $\lambda < 0{,}10\text{ W/m}\cdot\text{K}$ dijamin memperoleh skor termal 100 pada iklim panas.
    - **Usia Pakai:** Jika $\text{usia}_{\min} == \text{usia}_{\max}$ pada katalog, skor usia pakai bernilai 100.
    - **Penuaan:** Parameter $a$ ($0 < a \le 1$, default 0,5) dapat dikonfigurasi melalui panel asumsi perancangan, berversi, dan dapat di-reset.
    - **Bobot Kustom V2:** Validasi ketat finite, non-negatif, dan total tepat 100% tanpa normalisasi diam-diam; kalkulasi lokal tanpa memicu request HTTP ulang.
  - Seluruh parameter model perhitungan dikelompokkan ke dalam konfigurasi berversi (`KonfigurasiModel`) yang dapat dibaca pada panel antarmuka "Asumsi Perhitungan" dan dapat dikembalikan ke nilai default (*reset*).
  - Setiap perubahan parameter asumsi menghasilkan revisi hasil evaluasi baru tanpa menimpa riwayat evaluasi lama.
  - Metodologi pengujian performa dan keandalan didefinisikan:
    - Ketersediaan API cuaca $\ge 95\%$ diukur dari keberhasilan pengambilan data yang valid dan lengkap (bukan sekadar status HTTP 200).
    - Waktu respon < 2 detik diuji melalui benchmark operasi domain dan render UI.
    - Alur < 3 menit dan keterbacaan alasan rekomendasi divalidasi melalui skenario uji pengguna nyata (*user testing*) dengan persona yang relevan (§3).
- **Dampak Kode & Area Terdampak:**
  - `src/Wastu.Domain/KonfigurasiModel.cs`: Model konfigurasi parameter asumsi berversi.
  - `Views/Pages/SettingsPage.xaml` / Dialog Asumsi: Panel peninjauan parameter perhitungan dan tombol reset.
  - Dokumentasi rencana pengujian dan verifikasi acceptance criteria WP08.

---

## Matriks Hubungan dengan Acceptance Criteria F1–F7 dan X01–X17

Seluruh 11 koreksi di atas terhubung langsung dengan pemenuhan 31 Acceptance Criteria F1–F7 dan 17 Ketentuan Lintas Fitur X01–X17, sebagaimana dipetakan pada matriks PRD §14:

| Koreksi | Keterkaitan Langsung Acceptance Criteria | Ketentuan Lintas Fitur |
|---|---|---|
| **PRD-R01** | F4-04, F4-05, F7-01 | X01, X17 |
| **PRD-R02** | F5-02, F5-03, F5-04 | X08, X10, X14 |
| **PRD-R03** | F2-01, F2-03, F4-02 | X02, X09, X13 |
| **PRD-R04** | F4-01, F5-03, F5-04 | X08, X10 |
| **PRD-R05** | F6-02, F6-03, F6-04 | X11, X12 |
| **PRD-R06** | F1-01, F3-03, F7-03 | X03, X05, X07 |
| **PRD-R07** | F1-01, F1-04 | X01, X07, X13 |
| **PRD-R08** | F6-02, F6-05 | X04, X06, X14 |
| **PRD-R09** | F2-03, F3-01, F7-02 | X02, X13, X14 |
| **PRD-R10** | F1-02, F4-06, F5-01 | X01, X07, X17 |
| **PRD-R11** | F4-04, F5-05, F6-03 | X10, X15, X16, X17 |
