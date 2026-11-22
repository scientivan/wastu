# Matriks penerimaan finalisasi Wastu

Sumber: matriks §14 rencana revisi lengkap 22 November 2026. Semua status awal **Belum diverifikasi**; keberadaan fitur di baseline tidak membuktikan pemenuhan. Perbarui status hanya bersama bukti aktual dan commit/PR yang relevan. Skenario T01–T40 serta acceptance WP00–WP08 tetap wajib.

| ID | Kebutuhan | Paket | Bukti yang diperlukan | Status | Bukti aktual / PR |
|---|---|---|---|---|---|
| F1-01 | Buat, ubah, hapus nama/kota/koordinat; koordinat otomatis dari pilihan kota atau manual. | WP02 | Tes CRUD dan alur WPF. | Belum diverifikasi | — |
| F1-02 | Proyek mempunyai satu atau lebih komponen dinding/atap, masing-masing luasnya. | WP02 | Dinding saja, atap saja, keduanya; penolakan nol komponen. | Belum diverifikasi | — |
| F1-03 | Ubah lokasi memicu pengambilan ulang profil iklim. | WP03 | Mock request koordinat baru, invalidasi profil lama, respons terlambat. | Belum diverifikasi | — |
| F1-04 | Koordinat sesuai bounding box lampiran; keterbatasan proksi dijelaskan. | WP02 | Tes batas UI/domain/DB dan pemeriksaan teks UI. | Belum diverifikasi | — |
| F2-01 | Nama, jenis, termal, ketahanan lembap/korosi, usia nominal, faktor emisi tersedia dan dapat ditinjau. | WP02 | Detail bahan, tes satuan/validasi, pemeriksaan UI. | Belum diverifikasi | — |
| F2-02 | Filter berdasarkan jenis komponen. | WP02 | Filter dinding/atap tidak mencampur kategori. | Belum diverifikasi | — |
| F2-03 | Sumber setiap nilai ditampilkan; sifat perkiraan transparan. | WP02 | Metadata per properti dan tampilan sumber. | Belum diverifikasi | — |
| F2-04 | Pengembang/admin menambah bahan lewat DB tanpa perubahan kode. | WP02 | Insert bahan uji lalu refresh; tampil tanpa rebuild. | Belum diverifikasi | — |
| F3-01 | Historis hujan tahunan, RH rata-rata dan suhu rata-rata diperoleh dari API berdasarkan koordinat. | WP03 | Fixture agregasi, metadata periode dan smoke API. | Belum diverifikasi | — |
| F3-02 | Kedekatan pesisir dihitung dari koordinat sebagai proksi korosi. | WP03 | Tes geometri dan kesetaraan antar mode. | Belum diverifikasi | — |
| F3-03 | Profil disimpan per proyek dan digunakan ulang sampai lokasi berubah/refresh manual. | WP01, WP03 | Restart, luring dan cache reuse tanpa request ekstra. | Belum diverifikasi | — |
| F3-04 | Ringkasan profil ditampilkan sebelum perbandingan. | WP03 | Alur WPF dengan sumber/status yang benar. | Belum diverifikasi | — |
| F4-01 | Daftar jenis komponen terurut berdasarkan skor kesesuaian iklim. | WP04 | Tes urutan/nilai, tie-break dan kategori. | Belum diverifikasi | — |
| F4-02 | Setiap baris menampilkan skor 0–100 dan kgCO₂e berdampingan. | WP04 | Pemeriksaan seluruh baris UI, termasuk skor rendah. | Belum diverifikasi | — |
| F4-03 | Setiap bahan mempunyai alasan kalimat yang dapat dipahami. | WP04 | Tes alasan, akses setiap baris, uji pengguna. | Belum diverifikasi | — |
| F4-04 | MVP memakai empat bobot 0,25 dan tidak dapat diubah pengguna. | WP04 | Tes konfigurasi MVP dan state kontrol. | Belum diverifikasi | — |
| F4-05 | V2 dapat mengubah bobot termal/lembap/korosi/usia dan langsung melihat perubahan peringkat. | WP04 | Tes input valid/invalid, reset, tidak memanggil ulang API. | Belum diverifikasi | — |
| F4-06 | Bahan di bawah ambang tetap tampil dengan label Tidak disarankan. | WP04 | Tes nilai sekitar 55 dan seluruh konteks label. | Belum diverifikasi | — |
| F5-01 | Horizon dapat dipilih, setidaknya 20/30/50 tahun. | WP05 | UI dan tes persistensi horizon. | Belum diverifikasi | — |
| F5-02 | Usia efektif disesuaikan dari nominal berdasarkan skor iklim. | WP05 | Kasus a=0,5 dan skor 0/20/100 serta konfigurasi. | Belum diverifikasi | — |
| F5-03 | Jumlah penggantian dan emisi kumulatif dihitung sesuai kontrak horizon yang sudah dikoreksi. | WP05 | Angka acuan PRD-R02 dan batas presisi. | Belum diverifikasi | — |
| F5-04 | Perbandingan visual sederhana antar bahan; kgCO₂e kumulatif menjadi sorotan. | WP05 | Grafik/angka konsisten, komponen/luas setara. | Belum diverifikasi | — |
| F5-05 | UI menyatakan usia efektif adalah estimasi, bukan jaminan. | WP05 | Pemeriksaan teks dekat hasil dan laporan. | Belum diverifikasi | — |
| F6-01 | Pengguna membuat pekerjaan dengan jenis dan rentang tanggal rencana. | WP06 | CRUD dan validasi rentang. | Belum diverifikasi | — |
| F6-02 | Prakiraan harian lokasi proyek dievaluasi per hari menurut ambang jenis pekerjaan. | WP06 | Fixture, cache, statistik RH, jenis/override berbeda. | Belum diverifikasi | — |
| F6-03 | Hari tidak layak jelas dan mempunyai alasan ambang yang dilanggar. | WP06 | Tes beberapa pelanggaran dan tampilan UI. | Belum diverifikasi | — |
| F6-04 | Pengguna mengunci tanggal final setelah melihat evaluasi. | WP06 | Layak, Waspada, Tidak layak dengan persetujuan, batal dan restart. | Belum diverifikasi | — |
| F6-05 | Tanggal tanpa prakiraan/di luar cakupan diberi penjelasan eksplisit. | WP06 | Rentang melewati 14 hari, data null/parsial, kondisi kosong. | Belum diverifikasi | — |
| F7-01 | PDF memuat profil proyek, ringkasan iklim, bahan dipilih+skor+emisi, simulasi horizon, jadwal terkunci. | WP07 | PDF valid dan pencocokan isi dengan snapshot. | Belum diverifikasi | — |
| F7-02 | Tanggal pembuatan dan sumber API/material tercantum sebagai catatan transparansi. | WP07 | Ekstraksi teks dan inspeksi PDF. | Belum diverifikasi | — |
| F7-03 | Berkas disimpan lokal dan dapat dibuka ulang dari aplikasi. | WP07 | Save, restart, buka riwayat, file hilang dan cancel. | Belum diverifikasi | — |
| X01 | Scope dinding/atap, single-user desktop; tidak menambah RAB, marketplace atau komponen lain. | WP00, WP02 | Review PRD/UML/UI. | Belum diverifikasi | — |
| X02 | ≥15 dinding dan ≥10 atap siap dinilai. | WP02 | Query DB dan validasi dataset. | Belum diverifikasi | — |
| X03 | Profil iklim permanen dan dapat ditinjau luring. | WP01, WP03 | Putus internet/restart. | Belum diverifikasi | — |
| X04 | Prakiraan TTL enam jam, per proyek/lokasi, fallback dengan label. | WP06 | Clock test dan integrasi cache. | Belum diverifikasi | — |
| X05 | Timeout/error/cancel tidak membekukan aplikasi. | WP03, WP06, WP08 | Tes async dan manual Windows. | Belum diverifikasi | — |
| X06 | Semua kontrak cuaca/service terhubung ke jalur UI nyata. | WP03–WP08 | Tes service/ViewModel dan review wiring. | Belum diverifikasi | — |
| X07 | EF migrations, seed/SQL konsisten, pruning cache tidak merusak riwayat. | WP01, WP06 | Migrasi baseline dan cleanup test. | Belum diverifikasi | — |
| X08 | Skor sesuai arah termal/lembap/korosi/usia; rumus lembap direvisi. | WP04 | Kasus numerik/monoton/batas. | Belum diverifikasi | — |
| X09 | Emisi faktor×luas memakai satuan yang kompatibel. | WP02, WP04 | Kasus konversi dan luas dua kali lipat. | Belum diverifikasi | — |
| X10 | Asumsi model usia efektif dapat dibaca/dikonfigurasi dan berversi. | WP05 | Panel asumsi, validasi dan snapshot hasil. | Belum diverifikasi | — |
| X11 | Ambang kerja dapat diatur; koneksi ke spesifikasi bahan jujur. | WP06 | Override mengubah keputusan, sumber terekam. | Belum diverifikasi | — |
| X12 | Pengguna boleh mengunci hari tidak layak setelah memahami alasan. | WP06 | Konfirmasi dan jejak keputusan. | Belum diverifikasi | — |
| X13 | Sumber internasional, estimasi, proksi pesisir/bounding box dinyatakan terbuka. | WP02–WP07 | UI dan PDF. | Belum diverifikasi | — |
| X14 | Riwayat/hasil tidak ditulis ulang ketika input/rumus berubah. | WP01, WP04–WP07 | Revisi input dan PDF lama. | Belum diverifikasi | — |
| X15 | MVVM dan pengujian ViewModel sesuai strategi PRD. | WP08 | Tes command/state tanpa WPF render. | Belum diverifikasi | — |
| X16 | Metrik <3 menit, <2 detik, API≥95%, dan pemahaman pengguna mempunyai bukti. | WP08 | Laporan pengukuran dan uji pengguna. | Belum diverifikasi | — |
| X17 | Penyelarasan PRD-R01–R11 dan UML selesai. | WP00, WP08 | Changelog, review dokumentasi. | Belum diverifikasi | — |
