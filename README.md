# Wastu

**Pembanding bahan bangunan berbasis iklim dan jejak karbon untuk pemilik rumah.**

Wastu adalah aplikasi desktop yang membantu calon pemilik rumah memilih bahan dinding dan atap yang
cocok dengan iklim lokasi bangunan sekaligus memperhitungkan jejak karbon tersemat tiap bahan, lalu
menjadwalkan pekerjaan konstruksi sesuai prakiraan cuaca.

### Kelompok Manut

- **Ketua Kelompok:** Dien Muhammad Scientivan Kurniapramono — 24/533571/TK/59114
- **Anggota 1:** Yohanes Anthony Saputra — 24/536327/TK/59524
- **Anggota 2:** Ramzi Alfito Rizky — 24/540550/TK/60008

## Diagram Kelas

> [!NOTE]
> Diagram di bawah adalah **diagram kelas historis tugas awal Modul 3** dan dipertahankan sebagai arsip historis. Spesifikasi UML normatif sistem saat ini didefinisikan secara resmi pada [docs/uml-wastu.md](docs/uml-wastu.md) dan berkas sumber visual [docs/uml-wastu.drawio](docs/uml-wastu.drawio), dengan penjelasan pemetaan C# pada [docs/implementasi-class-diagram.md](docs/implementasi-class-diagram.md).

![Class Diagram Historis](docs/class-diagram.png)

## Latar Belakang

Pemilihan bahan bangunan rumah tinggal di Indonesia umumnya ditentukan oleh harga beli dan kebiasaan tukang, bukan oleh kecocokan bahan dengan curah hujan, kelembapan, suhu, dan risiko korosi di lokasi bangunan. Akibatnya bahan cepat rusak, penggantian dini lebih sering terjadi, dan setiap penggantian mengulang emisi karbon produksi bahan. Pengecoran atau pengecatan pada cuaca yang salah juga berujung pada pengerjaan ulang yang membuang biaya dan material.

Wastu menjawab masalah itu dengan tiga prinsip:

1. **Iklim dan karbon dinilai berdampingan.** Skor kesesuaian iklim (0–100) dan emisi awal ($\text{kgCO}_2\text{e}$) tampil pada satu peringkat yang sama berdampingan di setiap baris, lengkap dengan alasan tertulis yang mudah dipahami.
2. **Emisi dihitung sebagai konsekuensi jangka panjang.** Bahan yang kurang cocok dengan iklim diberi usia pakai efektif lebih pendek. Simulasi menangkap emisi berulang akibat penggantian dini sepanjang horizon 20, 30, atau 50 tahun.
3. **Penjadwalan kerja menyatu dengan rekomendasi bahan.** Hari layak kerja untuk pengecoran dan pengecatan dinilai dari prakiraan cuaca lokasi proyek dan ambang spesifikasi teknis pekerjaan.

## Status Sistem: Baseline Saat Ini vs Target Revisi Spesifikasi

Untuk menjaga integritas dokumentasi proyek, tabel berikut membedakan kapabilitas yang telah tersedia pada kode *baseline* saat ini dari target spesifikasi normatif revisi (WP00–WP08):

| Modul / Aspek | Kapabilitas Baseline Saat Ini | Target Revisi Spesifikasi (PRD-R01 — PRD-R11) |
|---|---|---|
| **Profil Proyek (F1)** | Membuat proyek dengan nama, kota/koordinat, dan mewajibkan luas dinding serta atap sekaligus. Bounding box bujur 94°–142° BT. | Mendukung pemilihan komponen fleksibel: dinding saja, atap saja, atau keduanya (luas $> 0\text{ m}^2$). Bounding box inklusif diseragamkan ke lintang $[-11, 6]$ dan bujur $[95, 141]$ dengan migrasi aman untuk data legacy (§PRD-R07). |
| **Katalog Bahan (F2)** | Seed 15 bahan dinding dan 10 bahan atap tersimpan di database/JSON; nilai properti belum menampilkan seluruh rincian sumber. | Normalisasi faktor emisi wajib $\text{kgCO}_2\text{e/m}^2$ dengan catatan asumsi ketebalan/massa konversi; pencatatan provenance per properti (`ApiHistoris`, `Contoh`, `Manual`, `LegacyUnknown`) (§PRD-R03, §PRD-R09). |
| **Analisis Iklim (F3)** | Klien Open-Meteo historis 10 tahun dan estimasi jarak pesisir garis pantai Natural Earth 1:10m. Profil tersimpan per proyek. | Pengambilan iklim otomatis saat online; penanganan kegagalan graceful dengan penanda profil iklim 0..1; pemisahan label data contoh dari data API riil (§PRD-R09, §PRD-R10). |
| **Perbandingan & Bobot (F4)** | Peringkat bahan dengan 4 kriteria berbobot default 0,25. Rumus lembap lama cepat jenuh pada RH 80%. Ambang UI memakai 55 sedangkan domain 50. | Rumus lembap baru dengan $\alpha=0{,}5$ dan acuan 3000 mm/thn yang sensitif terhadap variasi hujan (§PRD-R04). Ambang label terpusat: $<55$ *Tidak disarankan*, $55..74,999$ *Perlu pertimbangan*, $\ge 75$ *Cocok*. Rilis MVP mengunci bobot pada 0,25; rilis V2 menyediakan bobot kustom tervalidasi ($\sum=100\%$) dan reset (§PRD-R01, §PRD-R10). |
| **Simulasi Emisi (F5)** | Simulasi horizon 20, 30, 50 tahun memakai rumus lama `floor(H/L)` yang menyimpan hasil per `EvaluasiId` saja. | Menggunakan rumus penggantian terkoreksi pada interval kejadian $0 < t < H$: $n = \max(0, \lceil H/L \rceil - 1)$ dan emisi kumulatif $= \text{emisi\_awal} \times (1 + n)$ (§PRD-R02). Kasus acuan: nominal 15 thn, skor 20, $H=30 \rightarrow L=9$ thn, 3 penggantian (thn 9, 18, 27), total emisi $\times 4$. Riwayat multi-horizon disimpan simultan (§PRD-R10). |
| **Penjadwal Cuaca (F6)** | Mengambil prakiraan 14 hari Open-Meteo; evaluasi ambang pengecoran dan pengecatan; jadwal disimpan di JSON lokal; penguncian hari berisiko didukung. | Ambang teknis mendukung override spesifikasi produk bahan terpilih (§PRD-R05). Status evaluasi multi-nilai: `Layak`, `Waspada`, `TidakLayak`, dan `DataTidakTersedia`. Cache ber-TTL default 6 jam UTC dengan penafsiran tanggal berbasis zona waktu lokasi proyek (§PRD-R08). |
| **Ekspor Laporan (F7)** | Tombol PDF memanggil `PrintDialog` Windows; `Laporan.Ekspor` menulis format teks sederhana. | Generator PDF mandiri berlisensi sesuai, memuat profil proyek, iklim, bahan terpilih, simulasi horizon, dan jadwal terkunci lengkap dengan catatan transparansi sumber dan peninjauan riwayat lokal luring (§PRD-R06). |
| **Persistensi & Luring** | Mendukung PostgreSQL (lokal/remote Supabase) dan mode JSON lokal. Gagal koneksi remote saat startup menutup aplikasi. | PostgreSQL sebagai penyimpanan utama relasional; snapshot lokal atomik read-only untuk koneksi remote saat offline sehingga aplikasi tetap dapat dibuka untuk ditinjau tanpa crash (§PRD-R06). |

## Fitur Aplikasi

- **Profil Proyek:** Kelola nama, kota, koordinat wilayah layanan Indonesia, serta konfigurasi komponen (dinding saja, atap saja, atau keduanya).
- **Analisis Iklim Lokasi:** Agregasi historis 10 tahun curah hujan tahunan, kelembapan, suhu, dan jarak pesisir dari Open-Meteo.
- **Eksplor Katalog Bahan:** Telusuri bahan dinding dan atap lengkap dengan properti konduktivitas termal, ketahanan lembap/korosi, usia nominal, dan faktor emisi $\text{kgCO}_2\text{e/m}^2$.
- **Bandingkan Material:** Peringkat kesesuaian iklim (0–100) dan emisi awal berdampingan dengan alasan tekstual per baris.
- **Simulasi Emisi Jangka Panjang:** Proyeksi emisi kumulatif 20, 30, atau 50 tahun memperhitungkan frekuensi penggantian dini.
- **Penjadwal Pekerjaan Sensitif Cuaca:** Evaluasi harian 14 hari untuk pengecoran dan pengecatan, indikator cache 6 jam, dan penguncian jadwal kerja.
- **Laporan Rekomendasi PDF:** Dokumen rekomendasi terpadu untuk dibawa berdiskusi dengan tukang atau kontraktor.

## Alur Penggunaan

1. **Buat Proyek:** Masukkan nama proyek, pilih kota atau koordinat, dan tentukan luas bidang dinding dan/atau atap.
2. **Tinjau Profil Iklim:** Sistem mengambil data historis lokasi dan menyajikan ringkasan iklim di Beranda.
3. **Bandingkan Bahan:** Buka halaman pembanding untuk melihat peringkat bahan terurut berdasarkan skor kesesuaian dan jejak karbon.
4. **Simulasi Emisi:** Pilih bahan dan lihat proyeksi penggantian dini serta emisi kumulatif sepanjang horizon masa pakai rumah.
5. **Jadwalkan Pekerjaan:** Buka penjadwal cuaca, tinjau hari yang layak untuk pengecoran/pengecatan, dan kunci tanggal aman.
6. **Ekspor Laporan:** Unduh berkas PDF rekomendasi lengkap untuk arsip dan pedoman kerja di lapangan.

## Teknologi

- **Framework:** Desktop Windows berbasis WPF (.NET 8 LTS, C#).
- **Basis Data:** PostgreSQL (didukung lokal maupun cloud Supabase) melalui Entity Framework Core (Npgsql).
- **Layanan Cuaca:** Open-Meteo Historical Weather API & Daily Forecast API.
- **Vektor Pesisir:** Garis pantai regional Natural Earth 1:10m.

## Menjalankan Aplikasi dengan PostgreSQL

1. Siapkan database PostgreSQL lokal atau instans remote. Jalankan skrip inisialisasi:
   - `database/01_create_database.sql` (bila menggunakan PostgreSQL lokal).
   - `database/02_schema.sql` (struktur tabel relasional).
   - `database/03_seed_supabase.sql` (data katalog awal bahan dan profil contoh).
2. Konfigurasikan variabel koneksi melalui environment variable atau salin `.env.example` menjadi `.env` di direktori proyek:
   - Variabel yang didukung: `host`, `port`, `database`, `user`, `password` (atau `WASTU_CONNECTION_STRING`).
   - Berkas `.env` bersifat lokal dan tidak boleh di-commit ke repositori.
3. Jalankan aplikasi:
   ```bash
   dotnet run --project Wastu.csproj
   ```
4. Jalankan pengujian unit backend domain:
   ```bash
   dotnet test Wastu.sln
   ```

Jika seluruh variabel database dikosongkan, aplikasi beroperasi dalam mode penyimpanan lokal terisolasi (`state.json`). Saat mode PostgreSQL aktif, aplikasi membaca katalog dan data proyek dari database relasional.

## Batasan dan Transparansi

Properti fisik bahan, faktor emisi karbon, dan profil iklim awal bersumber dari rujukan literatur publik internasional (ICE Database, EPD generik) yang menjadi estimasi awal untuk pasar Indonesia. Fungsi penyesuaian usia pakai efektif dan simulasi emisi adalah model perhitungan estimasi matematis untuk membantu pengambilan keputusan, bukan jaminan keawetan mutlak di lapangan.
