# Finalisasi Wastu Implementation Plan

> **Untuk pelaksana:** kerjakan satu paket terukur melalui worker Orca yang diawasi koordinator; gunakan skill `executing-plans` untuk eksekusi bertahap. Setiap paket wajib menyertakan pengujian perilaku, review, commit, push, pull request, dan verifikasi merge sebelum dependensinya dimulai.

**Goal:** menyelesaikan F1–F7 sampai V2, 17 ketentuan lintas fitur, dan skenario minimum T01–T40 sesuai rencana revisi 22 November 2026.

**Architecture:** pertahankan desktop WPF, tema yang ada, domain berbahasa Indonesia, dan PostgreSQL/EF Core. Pindahkan aturan serta I/O dari jalur UI ke service dan ViewModel secara bertahap; hasil, cache, keputusan jadwal, dan laporan memakai snapshot input berversi. Mode JSON kompatibilitas tetap tersedia; fallback remote memakai snapshot lokal read-only.

**Tech Stack:** C#, WPF, .NET 8, EF Core/Npgsql, PostgreSQL, xUnit, Open-Meteo, aset garis pantai Natural Earth; pustaka PDF dipilih setelah pemeriksaan lisensi.

## Dasar rencana dan status awal

- Baseline lokal dan `origin/master`: `c197d678474c04cd7feaafac3fc934ac1c297325`.
- Semua berkas teks dalam `docs/`, sumber draw.io dan gambar class diagram telah ditinjau bersama rencana revisi lengkap. PRD lampiran dan arsip percakapan yang disebut rencana tidak tersedia dalam repository; gunakan matriks §14 rencana revisi untuk kebutuhan yang tidak ada di PRD repository.
- Panduan revisi pengguna `Wastu_Rencana_Revisi_Lengkap.md` masih merupakan berkas lokal untracked ketika audit dimulai; pertahankan tanpa perubahan dan jangan ikut men-stage secara tidak sengaja.
- Tidak ditemukan `AGENTS.md` pada jalur induk workspace maupun daftar berkas repository.
- Perubahan produk belum dikerjakan pada saat rencana ini ditulis. Checklist yang belum mempunyai bukti tetap terbuka.
- Bukti build/test awal: [baseline](../../verification/2026-11-22-baseline.md).

## Peta repository dan batas tanggung jawab

| Area | Lokasi aktual | Tanggung jawab dan arah revisi |
|---|---|---|
| Bootstrap desktop | `App.xaml.cs`, `Views/MainWindow.xaml.cs` | Komposisi dependency, startup, status koneksi dan pemulihan; jangan menutup aplikasi saat remote gagal. |
| Halaman | `Views/CreateProjectView.*`, `Views/Pages/*`, `Views/ShellView.*` | Interaksi, binding dan dialog; migrasikan satu halaman dengan ViewModel yang dapat diuji. |
| Visual | `Themes/*`, `Controls/*`, `Assets/*` | Pertahankan token, font, aset dan tema; grafik memakai kejadian domain yang sama dengan total. |
| State/adaptor UI | `Services/AppState.cs`, `Services/StateStore.cs`, `Models/*` | Jembatan legacy; jangan membuat aturan perhitungan alternatif atau identitas sementara untuk hasil tersimpan. |
| Domain | `src/Wastu.Domain/` | Entitas, invariant, kontrak repository, skor, simulasi, evaluator dan service. |
| Persistensi | `src/Wastu.Data/`, `database/` | EF, repository, transaksi, migrasi, sumber properti dan snapshot; SQL harus setara EF. |
| Kompatibilitas | `src/Wastu.InMemory/` | Jalur layanan yang setara dan katalog kanonik, bukan aturan produk tersendiri. |
| Cuaca | `Services/OpenMeteo.cs`, `Services/JarakPesisir.cs` | Pisahkan parsing/HTTP dari keputusan domain; metadata periode, zona, sumber dan kualitas terstruktur. |
| Tes | `tests/Wastu.Domain.Tests/`, `tests/Wastu.Data.Tests/` | Tambahkan tes service/ViewModel/parser/snapshot yang berjalan tanpa render WPF; gunakan PostgreSQL uji terisolasi. |
| Dokumentasi | `docs/PRD-wastu.md`, `docs/uml-wastu.*`, `docs/implementasi-class-diagram.md`, `README.md` | Satu kontrak produk normatif, diagram selaras, panduan menjalankan dan bukti penerimaan. |

## Temuan yang sudah dicocokkan dengan source

| Masalah | Bukti lokasi | Penanganan |
|---|---|---|
| Startup menutup jendela jika load DB gagal | `Views/MainWindow.xaml.cs:40` | WP01: snapshot profil koneksi atau halaman recovery. |
| State PostgreSQL berbagi satu nama file tanpa namespace koneksi | `Services/StateStore.cs:77` | WP01: schema version, identitas profil nonrahasia, atomic replace dan backup. |
| UI menghitung ranking dengan komponen `Guid.Empty` | `Services/AppState.cs:64` | WP01/WP04: identitas persisten dan batch input berversi. |
| Jalur create lokal tidak otomatis mengambil historis | `Services/AppState.cs:114` | WP02/WP03: create tersimpan dahulu, pengambilan async dan retry yang jelas. |
| Komponen create selalu dinding serta atap | `Services/AppState.cs:140` | WP02: minimal satu komponen, tanpa komponen palsu berluas nol. |
| Bounding box lama memakai bujur 94–142 | `src/Wastu.Data/WastuDbContext.cs:88` | WP02: 95–141, audit legacy sebelum constraint ketat. |
| Beban lembap jenuh terlalu cepat | `src/Wastu.Domain/Scoring/PerhitunganSkor.cs:77` | WP04: gabungan berbobot normalisasi hujan/RH sesuai revisi. |
| Cache hanya satu proyek dan dihapus saat pindah | `Services/AppState.cs:91` | WP06: per proyek/revisi koordinat, TTL UTC dan zona proyek. |
| Evaluator UI memakai ambang sendiri | `Models/PrakiraanCuaca.cs:27` | WP06: satu evaluator domain dengan override yang persisten. |
| RH prakiraan dirata-rata per tanggal | `Services/OpenMeteo.cs:94` | WP06: RH maksimum, pemeriksaan kelengkapan dan kualitas per hari. |
| Upsert simulasi hanya berdasarkan evaluasi | `src/Wastu.Data/Repositories/EvaluasiRepositoryEf.cs:84` | WP05: evaluasi+horizon+versi, idempoten dan tidak menimpa horizon lain. |
| Rumus model UI memakai epsilon tersendiri | `Models/SimulasiEmisi.cs:30` | WP05: satu kebijakan presisi, bedakan kejadian interior dan ujung horizon. |
| Ekspor membuka printer dan membatasi lima bahan | `Views/Pages/ReportPage.xaml.cs:20`, `:84` | WP07: writer PDF langsung, pilihan pengguna dan riwayat. |

## Kontrak yang dibekukan sebelum implementasi paralel

1. Identitas `Guid` persisten dan mapping ID JSON legacy yang idempoten; revisi koordinat terpisah dari revisi nama/luas.
2. Komponen berupa koleksi terpilih dengan luas finite positif; minimal satu. Koordinat inklusif -11/6/95/141, dengan penjelasan bounding box kasar.
3. Sumber iklim/API/contoh/legacy terpisah dari status loading/error; periode, provider, UTC dan zona disimpan terstruktur.
4. Emisi selalu memakai faktor yang dapat ditelusuri dalam kgCO₂e/m²; sumber per properti dan asumsi konversi eksplisit.
5. Snapshot evaluasi membawa luas, iklim, katalog, bobot, konfigurasi, hash input dan versi algoritme. Label default berdasarkan angka asli: <55 / 55–<75 / ≥75.
6. MVP berbobot 0,25; V2 menyediakan validasi empat persentase, jumlah 100, reset dan persistensi per proyek.
7. Simulasi menghitung kejadian penggantian pada 0<t<H. Nominal15/skor20/H30 menghasilkan usia9, tiga penggantian dan total ×4.
8. Prakiraan segar jika 0≤umur<6 jam; tanggal mengikuti zona proyek; null/parsial bukan angka nol. RH prakiraan maksimum, RH historis rata-rata.
9. Hasil evaluator: Layak/Waspada/TidakLayak/DataTidakTersedia. Override pekerjaan → spesifikasi produk → default; snapshot efektif disimpan saat lock.
10. PDF mengambil snapshot yang sama, mempunyai pilihan bahan/horizon dan riwayat; ekspor luring boleh dengan sumber serta umur data jelas.

## Urutan paket dan PR

Setiap baris di bawah adalah bidang perubahan tersendiri. Jika terlalu besar, pecah menjadi PR kontrak/domain, data/service, lalu wiring UI dengan kriteria selesai masing-masing. WP08 berlangsung di setiap paket.

| Urutan | Paket/branch yang disarankan | Hasil konkret | Gate sebelum lanjut |
|---|---|---|---|
| 0 | `docs/requirements-alignment` | Selaraskan PRD-R01–R11, UML termasuk sumber draw.io, catatan model legacy, README dan matriks penerimaan. Bedakan target dari kemampuan yang benar-benar sudah tersedia. | Review seluruh 31 F +17 X; jangan menandai implementasi selesai. |
| 1a | `refactor/versioned-result-contracts` | Kontrak komponen, identitas, metadata iklim/cuaca, batch hasil, jadwal dan laporan; adaptor lama tetap build. | Kontrak disepakati sebelum worker UI/data mengubah interface yang sama. |
| 1b | `feat/persistence-history` | Repository pekerjaan, ambang, cache, batch evaluasi, multi-horizon dan laporan; transaksi serta migrasi aditif. | Fresh install, upgrade baseline, rollback kegagalan dan impor ulang tidak kehilangan/duplikasi data. |
| 1c | `fix/offline-startup-recovery` | Snapshot atomik per koneksi, backup, versi skema dan banner read-only; recovery tanpa snapshot. | Remote mati/snapshot rusak/koneksi parsial tidak menutup aplikasi atau mengklaim penyimpanan berhasil. |
| 2a | `feat/project-components` | Form create/edit dinding saja/atap saja/keduanya; validator bersama; navigasi halaman sesuai komponen. | F1, T01–T03, seluruh halaman proyek satu komponen. |
| 2b | `feat/material-provenance` | Dataset kanonik ≥15+10, sumber tiap nilai, satuan normal, detail dan refresh katalog. | F2, T15/T39, validasi sumber dan kesetaraan semua jalur katalog. |
| 2c | `fix/climate-profile-lifecycle` | Histori 10 tahun lengkap, auto fetch, cancellation, metadata, pesisir bersama dan respons sesuai revisi koordinat. | F3, T04–T10; data contoh tidak berstatus API. |
| 3a | `fix/climate-suitability-scoring` | Rumus lembap, validasi finite, tie-break stabil, normalisasi kategori penuh, label dan alasan terpusat. | T11–T17 dan snapshot evaluasi tidak duplikat saat rerender. |
| 3b | `feat/comparison-weights` | Home/katalog/pembanding memakai service yang sama; alasan setiap baris; bobot V2/reset dan panel asumsi. | F4 pada mode MVP dan V2; perubahan bobot tanpa HTTP iklim. |
| 4 | `fix/emission-simulation-history` | Satu kalkulator presisi dan kejadian grafik; asumsi a/reset, horizon 20/30/50 dan riwayat persisten. | F5, T18–T20/T38, batas 29,999/30/30,001. |
| 5a | `feat/forecast-cache-quality` | Klien forecast, cache proyek/lokasi, TTL, zona, RH maksimum dan fixture parser. | T21–T23/T26–T27/T40. |
| 5b | `feat/weather-work-scheduling` | CRUD pekerjaan, pengaturan ambang dan override, seluruh tanggal rentang, lock/konfirmasi dan riwayat review. | F6, T24–T31, restart dan repository UI nyata. |
| 6 | `feat/pdf-report-history` | Writer PDF berlisensi sesuai, pilihan bahan/horizon, tabel multipage, sumber, riwayat/buka/recovery dan metadata pending idempoten. | F7, T32–T35, PDF valid serta teks/visual dibanding snapshot. |
| 7 | `test/windows-release-acceptance` | Pengujian lintas fitur dan publish Windows bersih; pengukuran/API, panduan DB dan uji pengguna nyata. | Seluruh bukti penerimaan aktual; hasil yang belum diuji tetap terbuka. |

### WP00 — langkah pertama yang dapat dikerjakan langsung

- [x] Catat struktur, head, status awal, identitas GitHub dan baseline build/test.
- [ ] Buat branch `docs/requirements-alignment` dari `origin/master` terbaru sebelum menyunting dokumen kontrak.
- [ ] Terapkan PRD-R01–R11 pada PRD, dengan changelog kebutuhan lama → baru → area kode.
- [ ] Selaraskan diagram: hapus kewajiban login, profil 0..1, komponen minimal satu, banyak simulasi, jalur bobot kembali ke skor, konfirmasi risiko dan data tidak tersedia.
- [ ] Catat pemetaan nama tabel fisik Indonesia, mode remote/JSON/luring, sumber dan versi hasil; README harus membedakan perilaku sekarang dari target.
- [ ] Buat matriks 31 F dan 17 X dengan kolom status, penanggung jawab paket, bukti otomatis/manual serta keterbatasan.
- [ ] Review diff, jalankan pemeriksaan dokumentasi, commit per bidang, push dan PR; merge hanya setelah semua konflik kebutuhan terselesaikan.

## Pembagian worker berdasarkan kompleksitas

Ini kebijakan penugasan awal, bukan klaim benchmark kemampuan model. Uji tugas kecil dahulu; pindahkan tugas bila hasil review tidak memadai. Nama model hanya dipakai jika tersedia pada host saat dispatch.

| Tingkat | Worker/model calon | Penugasan | Kebijakan effort/review |
|---|---|---|---|
| Ringan | OpenCode, `opencode/mimo-v2.6-flash-free` | Inventaris, matriks AC, audit dokumentasi, teks UI yang kontraknya sudah pasti. | OpenCode tidak mendukung flag effort melalui worker-start; tugas pendek dan checklist eksplisit. |
| Menengah | Antigravity, `gemini-3.8-flash` | Wiring ViewModel/WPF, detail katalog, state loading/error/empty dan audit alur UI. | `high` sesuai konfigurasi model yang terdeteksi; kontrak dan tes wajib. |
| Menengah–tinggi | OpenCode, model non-Claude yang terdaftar seperti `opencode/deepseek-v4-pro` | Parser fixture, service cuaca, repository dan tes yang spesifikasinya sudah dibekukan. | Jalankan hanya jika akses model terverifikasi; jangan menganggap daftar model sama dengan kuota tersedia. |
| Tinggi | Codex, `gpt-6.1-sol` | Review identitas/migrasi/transaksi, race condition, batas simulasi dan integrasi berisiko tinggi. | `high`; naikkan `xhigh` hanya untuk kegagalan/keputusan yang sulit. Koordinator memegang keputusan kontrak dan merge. |
| Tertunda autentikasi | MiMo Code langsung, `xiaomi/mimo-v2.6-flash`/`xiaomi/mimo-v2.6-pro` | Alternatif dokumentasi/tes/helper setelah akses tersedia. | CLI terpasang dan model terdaftar, tetapi inventaris kredensial langsung masih kosong; gunakan jalur OpenCode yang berhasil dahulu. |

Claude tidak digunakan melalui provider mana pun. Jangan memindahkan pekerjaan secara diam-diam ke Claude saat model lain gagal.

### Mekanisme Orca

- Satu **Run** menyimpan objective dan mailbox; **Task DAG** menyimpan dependensi, status dan hasil setiap paket.
- Gunakan **supervised worker** dengan spesifikasi self-contained: sumber, file yang boleh disunting, larangan, AC, perintah tes, format bukti dan titik berhenti.
- Gunakan **child worktree** terpisah ketika writer dapat berbenturan. Branch harus nama eksak tanpa prefix akun. Konfigurasi Orca saat audit memakai prefix git-username; override melalui `worktree create --branch <nama>` yang didukung CLI sebelum meluncurkan worker pada workspace tersebut. Ini diperlukan untuk memenuhi aturan nama branch pengguna.
- Audit read-only dapat berbagi checkout. Writer pada checkout bersama harus tunggal; jangan berganti branch ketika writer sedang bekerja.
- Konfigurasi Orca yang terdeteksi: `agentPermissionDefault=full-access`, native chat `full-access`, tanpa override per-agent. Verifikasi receipt peluncuran; full access tidak mengizinkan worker melampaui scope file atau menyentuh DB utama untuk tes.
- Mulai dengan maksimum dua worker aktif; gunakan worker ketiga hanya untuk tugas independen yang jelas. Mayoritas baca/edit rutin dibawa provider selain Codex.
- Koordinator membaca ringkasan, diff dan bukti yang relevan. Worker melaporkan path/commit/AC/hasil tes/blokir, tanpa menyalin seluruh source atau dokumen kembali.
- Worker bertanya melalui `ask`, koordinator menjawab dari kontrak; hasil melalui `worker_done`. Ikuti wake dari Orca, tanpa polling/watcher.
- Gunakan gate integrasi untuk perubahan interface, migrasi dan merge; gate internal review tidak berarti meminta izin pengguna lagi untuk pekerjaan yang telah diotorisasi.
- Saat kuota provider habis, checkpoint task, file, commit dan bukti; pindahkan hanya pekerjaan yang belum selesai ke provider tersedia. Tidak ada klaim jaminan token tidak akan habis.

## Alur GitHub yang wajib

1. `git fetch origin master`, pastikan dasar paket terbaru dan perubahan pengguna tetap utuh.
2. Buat branch deskriptif sebelum perubahan. Contoh: `fix/forecast-cache-quality`; tanpa prefix nama akun.
3. Satu commit untuk satu bidang logis; setiap commit disertai push. Contoh pesan satu kalimat: `fix(weather): preserve forecasts for each project`.
4. Stage path secara eksplisit; hindari `git add .`. Jangan stage `.env`, PDF pengguna, log rahasia atau berkas untracked pengguna.
5. Pastikan author/committer milik pengguna dan tidak ada trailer co-author, nama provider/model, atau atribusi otomatis dalam branch, commit, judul/body PR maupun komentar.
6. Buat PR melalui `gh` aktif sebagai `AnthonyS051105`; deskripsi menjelaskan perilaku, validasi dan keterbatasan. Gunakan body file untuk multiline.
7. Koordinator meninjau diff dan hasil tes, memeriksa status PR/checks serta perubahan terbaru `master`. Jangan auto-merge worker sebelum review.
8. Merge melalui sesi akun yang sama; pilih merge commit untuk mempertahankan commit bertahap, kemudian verifikasi `mergedBy` dan SHA. PR/merge tidak dilakukan oleh bot.
9. Sinkronkan `master` lokal hanya saat aman dan gunakan sebagai dasar paket berikutnya. Jangan force-push, menghapus perubahan pengguna, atau mengubah DB utama demi membuat tes lulus.

## Gerbang verifikasi

| Bukti | Pelaksanaan |
|---|---|
| Numerik domain | Angka acuan independen untuk lembap 97,826087 vs65,217391, termal60/30, korosi40/100, label55/75, luas×2, simulasi×2/×4 dan batas horizon. |
| Service/ViewModel | Fake repository/clock/HTTP; loading/error/empty, retry, stale response, cancel, command ganda, identitas dan invalidasi per revisi. |
| Parser | Fixture lengkap/null/array timpang/unit salah/duplikat tanggal/timeout; kalender kabisat dan zona WIB/WITA/WIT. |
| Data | PostgreSQL terisolasi; fresh schema, upgrade baseline, impor JSON dua kali, transaksi gagal dan hasil lintas restart. |
| Windows | Jalankan WPF pada runtime target .NET 8 dan publish bersih; form, semua halaman proyek satu komponen, dialog, resize, offline, printer independen dan buka PDF. |
| PDF | Header `%PDF`, parser/ekstraksi teks, inspeksi multi-halaman, simbol °C/m²/CO₂, bahan peringkat10, snapshot lama, cancel dan path hilang. |
| Metrik | Profil→peringkat nyata <3 menit; hitung kategori <2 detik dengan spesifikasi perangkat; API valid ≥19/20 sampel terkontrol tanpa cache-hit. |
| Uji pengguna | Minimum lima peserta sesuai protokol rencana; mayoritas mampu menjelaskan iklim dan batasan karbon. Tidak digantikan output tes otomatis. |

Perintah baseline yang diulang pada runtime target saat tersedia:

```bash
dotnet restore Wastu.sln
dotnet build Wastu.sln -c Release --nologo
dotnet test Wastu.sln -c Release --no-build --nologo
```

Tes standar pada host audit memerlukan runtime .NET 8. Roll-forward ke .NET 10 hanya bukti tambahan; target aplikasi tidak diubah ke .NET 10.

## Batas akses dan status yang harus tetap jujur

- Kredensial Supabase pengguna hanya tersedia di `.env` lokal terabaikan Git dengan permission 0600. Tidak dibawa ke prompt worker, log, dokumen atau artefak publik.
- Koneksi pengguna hanya untuk inspeksi read-only pada tahap audit. Migrasi/integration test yang menulis memakai database uji terpisah.
- Tidak ada Windows/VM atau environment Orca remote terhubung yang terdeteksi saat audit. Build lintas target bukan bukti WPF telah dijalankan.
- Pemilihan library PDF harus memverifikasi versi/lisensi dari sumber primer ketika dilaksanakan; rencana ini belum menetapkan paketnya.
- Belum ada bukti uji pengguna, API reliability atau performa Windows. Bagian tersebut tetap belum selesai sampai pengukuran nyata tersedia.
- Tahap selesai dilaporkan dengan commit/PR, F/X/T yang ditutup, perintah dan hasil aktual, cara upgrade, artefak serta sisa blokir spesifik.
