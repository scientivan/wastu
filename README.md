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
> Diagram di bawah adalah **diagram kelas historis tugas awal Modul 3** dan dipertahankan sebagai arsip historis. Spesifikasi UML normatif sistem didefinisikan secara resmi pada [docs/uml-wastu.md](docs/uml-wastu.md) dan sumber visual [docs/uml-wastu.drawio](docs/uml-wastu.drawio), dengan penjelasan pemetaan C# pada [docs/implementasi-class-diagram.md](docs/implementasi-class-diagram.md).

![Class Diagram Historis](docs/class-diagram.png)

## Latar Belakang

Pemilihan bahan bangunan rumah tinggal di Indonesia umumnya ditentukan oleh harga dan kebiasaan tukang,
bukan oleh kecocokan bahan dengan curah hujan, kelembapan, suhu, dan risiko korosi di lokasi bangunan.
Akibatnya bahan cepat rusak, penggantian dini lebih sering terjadi, dan setiap penggantian mengulang emisi
karbon produksi bahan. Pengecoran atau pengecatan pada cuaca yang salah juga berujung pada pengerjaan ulang.

Wastu menjawab masalah itu dengan tiga prinsip:

1. **Iklim dan karbon dinilai berdampingan.** Skor kesesuaian iklim dan emisi (kgCO₂e) tampil pada satu
   peringkat yang sama, lengkap dengan alasan tertulis yang mudah dipahami.
2. **Emisi dihitung sebagai konsekuensi jangka panjang.** Bahan yang kurang cocok dengan iklim diberi usia
   pakai efektif lebih pendek, sehingga simulasi menangkap emisi berulang akibat penggantian dini.
3. **Penjadwalan kerja menyatu dengan rekomendasi bahan.** Hari layak kerja untuk pengecoran dan
   pengecatan dinilai dari prakiraan cuaca lokasi proyek.

## Status Implementasi

Implementasi saat ini mencakup perubahan V2 berikut. Ketersediaan kode dan kelulusan tes otomatis belum menggantikan pengujian aplikasi WPF pada Windows.

| Modul | Perilaku yang tersedia |
|---|---|
| Profil proyek | Dinding saja, atap saja, atau keduanya; luas positif dan koordinat dalam rentang lintang [-11, 6], bujur [95, 141]. |
| Katalog | Dataset kanonik 15 bahan dinding dan 10 bahan atap untuk mode lokal/PostgreSQL, sumber per properti, status perkiraan, dan refresh katalog. |
| Iklim | Pengambilan historis berdasarkan lokasi, perlindungan respons terlambat ketika lokasi/proyek berubah, profil tersimpan, serta metadata sumber, periode, cache, dan proksi pesisir. |
| Peringkat | Satu layanan skoring untuk halaman aplikasi, normalisasi usia terhadap kategori penuh, alasan setiap bahan, ambang 55/75, dan emisi awal berdampingan dengan skor. |
| Bobot | Mode MVP memakai 25% per kriteria; mode V2 menyimpan bobot per proyek, menolak total selain 100%, mendukung reset, dan menghitung ulang tanpa mengambil iklim lagi. |
| Riwayat evaluasi | Identitas proyek/komponen stabil dan batch berdasarkan hash input; render ulang memakai cache, penulisan berulang tidak menimpa batch lama, dan kegagalan penyimpanan dapat dicoba lagi. |
| Simulasi | Penggantian hanya pada waktu 0 < t < H, faktor penuaan dapat diatur, grafik step mengikuti kalkulator domain, dan repository mendukung beberapa horizon. |
| Jadwal | Prakiraan, cache enam jam, zona waktu proyek, status data tidak tersedia, pengaturan ambang, dan konfirmasi untuk mengunci hari berisiko. |
| Laporan | PDF langsung tanpa printer, pilihan bahan/horizon/jadwal, bobot serta metadata iklim aktual, riwayat lokal, dan pemulihan pencatatan yang tertunda. |
| Luring | Snapshot per profil koneksi untuk peninjauan baca-saja saat database tidak tersedia; mode JSON lokal tetap tersedia. |

Verifikasi mencakup domain, repository PostgreSQL terisolasi, snapshot/pemulihan, state/ViewModel tanpa WPF, serta PDF. Pada Windows telah dijalankan alur kontrol WPF untuk ketiga kombinasi komponen, restart, dialog PDF dan risiko melalui UI Automation/keyboard, serta startup online/luring/pemulihan. Pengukuran dan smoke API merupakan sampel satu perangkat/sesi; belum menggantikan uji pengguna, pemeriksaan aksesibilitas menyeluruh, atau pengujian mesin Windows bersih.

## Fitur Aplikasi

- **Profil Proyek:** Nama, kota atau koordinat lokasi, jarak ke pesisir, serta luas dinding, atap, atau keduanya. Pengguna dapat mengelola lebih dari satu proyek.
- **Analisis Iklim Lokasi:** Curah hujan tahunan, kelembapan, dan suhu rata-rata dari data historis 10 tahun Open-Meteo.
- **Eksplor Material:** Katalog bahan dinding dan atap dengan skor kesesuaian iklim, status kecocokan, dan emisi awal untuk luas bidang proyek.
- **Bandingkan Material:** Dua bahan berdampingan beserta peringkat kesesuaian seluruh bahan sejenis.
- **Simulasi Emisi:** Proyeksi emisi kumulatif 20, 30, atau 50 tahun termasuk jumlah penggantian bahan.
- **Jadwal Pekerjaan:** Prakiraan cuaca 14 hari, penilaian hari layak atau tidak layak untuk pengecoran dan pengecatan, serta penguncian tanggal pekerjaan.
- **Laporan Proyek:** Ringkasan proyek, profil iklim, peringkat material, simulasi emisi, dan jadwal terkunci untuk dibawa ke tukang atau kontraktor. PDF dibuat langsung dan dapat dibuka kembali dari riwayat lokal.

## Alur Penggunaan

1. **Buat Proyek Baru:** Beri nama, tentukan lokasi (pilih kota atau koordinat), lalu pilih satu atau kedua komponen dan isi luas bidangnya.
2. **Perbarui Profil Iklim:** Tinjau ringkasan iklim lokasi di Beranda.
3. **Bandingkan Bahan:** Telusuri katalog bahan, pilih dua bahan, lalu bandingkan kesesuaian dan emisinya.
4. **Simulasikan Emisi:** Lihat dampak penggantian dini material sepanjang masa pakai rumah.
5. **Jadwalkan Pekerjaan:** Ambil prakiraan cuaca, pilih hari yang layak, dan kunci jadwal pekerjaan.
6. **Simpan Laporan Rekomendasi:** Ekspor laporan sebagai PDF.

## Teknologi

Aplikasi desktop Windows berbasis WPF (.NET 8, C#) dengan data cuaca dan iklim dari Open-Meteo.

## Menjalankan dengan PostgreSQL

1. Buat database lokal dengan `database/01_create_database.sql` atau gunakan database PostgreSQL yang sudah tersedia.
2. Siapkan skema dengan migrasi EF pada database kosong atau database dengan riwayat migrasi EF yang sudah cocok. SQL instalasi/seed di `database/` dipakai untuk pemeriksaan kesetaraan; jangan menjalankan SQL lalu langsung mengulang seluruh migrasi EF pada tabel yang sama.
3. Isi `.env` sesuai `.env.example` dengan `host`, `port`, `database`, `user`, dan `password` dari Supabase. Aplikasi menyusun connection string Npgsql otomatis dengan SSL. Gunakan session pooler port 5432. Alternatifnya, isi `WASTU_CONNECTION_STRING` lengkap untuk mengesampingkan variabel terpisah.

4. Jalankan `dotnet run --project Wastu.csproj` di Windows dengan .NET 8 SDK. Jalankan tes dengan perintah pada bagian Pengujian di bawah.

Pemilik database menjalankan migrasi sekali sebelum aplikasi baru dipakai, sesudah backup diverifikasi. Dengan konfigurasi lokal/environment di atas tersedia, gunakan `dotnet ef database update --project src/Wastu.Data` dari root repository (tool `dotnet-ef` 8.0.11 sesuai dependency proyek). Untuk deployment remote, pemilik database dapat menyiapkan migration bundle/script; klien aplikasi biasa hanya memeriksa kecocokan skema. Database lama yang dibuat lewat SQL tanpa riwayat EF memerlukan pemeriksaan baseline terlebih dahulu; jangan menandai semua migrasi telah diterapkan tanpa memeriksa skemanya. Migrasi otomatis lokal beserta backup/recovery masih merupakan pekerjaan terbuka.

File `.env` dicari dari direktori kerja, lalu direktori executable, termasuk folder induknya. Environment variable yang terisi lebih diutamakan daripada nilai dalam file. `.env` tidak disertakan dalam build atau publish dan tidak boleh di-commit. Untuk menjalankan hasil publish, sediakan `.env` sendiri di direktori executable atau atur environment variable. Konfigurasi database yang hanya terisi sebagian akan menghasilkan kesalahan agar aplikasi tidak diam-diam beralih ke mode lokal.

Dengan koneksi PostgreSQL, proyek, luas, profil iklim, katalog, jadwal/keputusan cuaca, simulasi, dan riwayat laporan dibaca/ditulis melalui repository. UI memakai service simulasi, penjadwalan, dan ekspor yang sama dengan pengujian. Snapshot lokal dipisahkan menurut profil koneksi dan mencakup katalog, cache prakiraan, riwayat, bobot serta faktor penuaan per proyek. Jadwal snapshot lama diimpor sekali dengan identitas stabil; penghapusan setelah impor tidak membuat jadwal muncul kembali. Perubahan area/komponen memakai ID komponen database yang tersimpan.

Pengambilan historis mempunyai batas waktu 60 detik, sedangkan prakiraan 12 detik. Pembaruan manual iklim/prakiraan dapat dibatalkan dengan menekan tombol pembaruan lagi. Cache memakai timestamp UTC terstruktur dan tanggal pada zona provider. Laporan menyimpan snapshot cuaca/ambang dan persetujuan yang sebenarnya; keputusan tanpa bukti legacy tidak dianggap otomatis layak. Saat ekspor luring, PDF tetap tersedia dan metadata menunggu rekonsiliasi per profil koneksi.

Saat seluruh konfigurasi koneksi kosong, aplikasi membuka mode JSON lokal. Berkas legacy `state.json` dan `state-postgres.json` dapat diimpor ke snapshot berversi yang sesuai. Jika database tidak tersedia, snapshot profil koneksi yang valid dapat dibuka dalam mode baca-saja; bila snapshot tidak tersedia atau rusak, aplikasi menampilkan alur pemulihan.

Dalam mode PostgreSQL, profil iklim memakai 10 tahun data historis Open-Meteo. Jarak pesisir diestimasi dari segmen garis pantai regional [Natural Earth 1:10m](https://www.naturalearthdata.com/downloads/10m-physical-vectors/10m-coastline/). Resolusi garis pantai membatasi akurasi jarak pada pulau kecil.

## Batasan dan Transparansi

Properti bahan, faktor emisi, dan profil iklim bawaan masih berupa estimasi awal dari referensi literatur publik, belum dikalibrasi untuk produk spesifik di pasar lokal Indonesia. Katalog lokal dan seed PostgreSQL memakai dataset kanonik yang sama; sumber dan status perkiraan per properti tetap harus diperiksa sebelum digunakan untuk produk tertentu. Usia pakai efektif dan simulasi emisi adalah model perhitungan estimasi matematis untuk membantu keputusan, bukan jaminan keawetan mutlak di lapangan.

## Pengujian dan Publish

```sh
dotnet build Wastu.sln -c Release
dotnet test tests/Wastu.Domain.Tests -c Release --no-build
dotnet test tests/Wastu.Application.Tests -c Release --no-build
dotnet test tests/Wastu.Reporting.Tests -c Release --no-build
```

Tes aplikasi mengompilasi sumber state dan adapter yang sama tanpa lapisan visual Windows. Tes tersebut tidak menjalankan jendela WPF atau dialog berkas.

Tes data memakai `WASTU_TEST_CONNECTION_STRING` menuju PostgreSQL uji terisolasi. Jalankan `dotnet test tests/Wastu.Data.Tests -c Release --no-build --filter "FullyQualifiedName!~AlurSupabaseTests"`. Tes integrasi yang memerlukan database akan melaporkan skip jika koneksi uji tidak tersedia. `AlurSupabaseTests` menggunakan konfigurasi aplikasi dan menulis data, sehingga harus dijalankan hanya dengan konfigurasi database uji yang disengaja, terpisah dari database pengguna.

```sh
dotnet publish Wastu.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Salin seluruh folder hasil publish ke Windows dan jalankan `Wastu.exe`. Build/publish lintas platform memverifikasi kompilasi dan paket; jalankan pengujian interaktif, luring, resize, dialog ekspor, serta buka ulang PDF pada Windows sebelum menyatakan rilis siap digunakan. Aset garis pantai dan font PDF disertakan sebagai embedded resource. Berkas `.env` harus disediakan secara lokal dan tidak disertakan dalam paket.


Untuk mengulang pemeriksaan WPF, bangun dengan `dotnet build Wastu.csproj -c Release -p:Snapshot=true`. Jalankan salinan output dari folder uji di luar pohon `.env`, atur `WASTU_DATA_DIR` ke direktori uji kosong, dan set `WASTU_SNAPSHOT=acceptance;<folder-hasil>;1440;900`. Mode `acceptance-restart` memakai data hasil proses pertama. Harness merender kontrol WPF dan menulis hasil/binding trace; ini otomasi Windows, bukan studi pemahaman manusia. Mode diagnostik tidak disertakan dalam build Release biasa.

`tests/Wastu.WindowsVerification` menyediakan mode `benchmark` (komputasi kategori tanpa HTTP/cache/UI), `api` (satu historis dan 20 prakiraan nyata), serta `database` (alur service dan restart). Jalankan memakai `dotnet run --project tests/Wastu.WindowsVerification -c Release -- <mode> <folder-hasil>`. Mode database hanya menerima `WASTU_TEST_CONNECTION_STRING` untuk database khusus `wastu_windows_ui` pada `127.0.0.1`; tidak memakai database aplikasi pengguna. Jalankan tes Data dengan database uji terpisah sebagaimana dijelaskan di atas.

Evaluasi baru menyimpan sifat/sumber bahan, profil iklim, komponen, bobot, konfigurasi, dan populasi normalisasi aslinya. Perubahan katalog atau faktor penuaan tidak menimpa hasil sebelumnya. Evaluasi legacy tanpa input asli tetap tersimpan dan memerlukan evaluasi baru sebelum simulasi; aplikasi tidak menebak usia historis dari katalog terkini.

Laporan baru menyimpan snapshot lengkap bersama metadata PostgreSQL/JSON maupun antrean pencatatan luring. Tindakan **Buat salinan** pada riwayat mempertahankan identitas, waktu, angka, sumber, bobot, dan asumsi laporan asli, termasuk ketika PDF lama hilang. Snapshot harus lolos pemeriksaan identitas/hash; laporan legacy tanpa snapshot tidak dapat dibuat ulang dengan angka tebakan. Backup database mencakup snapshot laporan; file PDF fisik tetap harus dicadangkan terpisah.

Migrasi `20261122110423_PreserveEvaluationInputs` menambahkan kolom nullable input evaluasi dan memperluas identitas simulasi dengan faktor penuaan. Terapkan melalui prosedur migrasi database uji/deployment yang dijelaskan di atas; aplikasi tidak memigrasi database pengguna diam-diam. Data legacy tidak diisi ulang dengan input katalog terbaru. Proyek JSON lama tanpa komponen/luas tetap dapat diperbaiki pada profil dan tidak diberi komponen 100 m² otomatis.

Penerimaan keseluruhan WP08 tetap terbuka: studi minimal lima pengguna belum tersedia, pemisahan ViewModel halaman lainnya belum lengkap, dan alur edit/buka kunci pekerjaan serta spesifikasi produk opsional masih perlu dituntaskan. Audit cache cuaca, migrasi legacy, dan konfigurasi ambang umum juga masih mempunyai pekerjaan terbuka. Folder hasil pengujian dan dokumen verifikasi lokal bukan bagian distribusi atau repository.
