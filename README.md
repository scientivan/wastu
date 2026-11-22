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

Verifikasi otomatis tersedia untuk domain, repository PostgreSQL terisolasi, snapshot/pemulihan, alur state aplikasi tanpa WPF, dan isi serta tata letak PDF. Uji interaktif WPF pada Windows, pengukuran performa Windows, reliabilitas API nyata, uji pemahaman pengguna, dan penyelesaian pemisahan ViewModel masih menjadi bagian penerimaan yang belum selesai.

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
2. Jalankan `database/02_schema.sql` dan `database/03_seed_supabase.sql` pada database tersebut.
3. Isi `.env` sesuai `.env.example` dengan `host`, `port`, `database`, `user`, dan `password` dari Supabase. Aplikasi menyusun connection string Npgsql otomatis dengan SSL. Gunakan session pooler port 5432. Alternatifnya, isi `WASTU_CONNECTION_STRING` lengkap untuk mengesampingkan variabel terpisah.

4. Jalankan `dotnet run --project Wastu.csproj` di Windows dengan .NET 8 SDK. Jalankan tes dengan perintah pada bagian Pengujian di bawah.

File `.env` dicari dari direktori kerja, lalu direktori executable, termasuk folder induknya. Environment variable yang terisi lebih diutamakan daripada nilai dalam file. `.env` tidak disertakan dalam build atau publish dan tidak boleh di-commit. Untuk menjalankan hasil publish, sediakan `.env` sendiri di direktori executable atau atur environment variable. Konfigurasi database yang hanya terisi sebagian akan menghasilkan kesalahan agar aplikasi tidak diam-diam beralih ke mode lokal.

Dengan koneksi PostgreSQL, proyek, luas bidang, profil iklim, dan katalog dibaca dari database. Katalog dimuat saat aplikasi dibuka. Jadwal dan preferensi tampilan disimpan di `state-postgres.json`. Saat seluruh konfigurasi koneksi kosong, aplikasi membuka proyek dan katalog lokal seperti biasa. Proyek lama tetap dapat dibuka di mode lokal melalui `state.json`.

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
