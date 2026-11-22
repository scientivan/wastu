using System.Security.Cryptography;
using UglyToad.PdfPig;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;
using Wastu.InMemory;
using Xunit;

namespace Wastu.Reporting.Tests;

public sealed class PdfReportingTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _pendingDir;

    public PdfReportingTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "Wastu_PdfReportingTests_" + Guid.NewGuid().ToString("N"));
        _pendingDir = Path.Combine(_testDir, "pending");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_pendingDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch
        {
            // ignore cleanup errors
        }
    }

    [Fact]
    public async Task DokumenPdf_MemilikiHeaderValid_DapatDiparsing_DanNolKebocoranKredensial()
    {
        // Arrange
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);
        var snapshot = BuatContohSnapshot();
        var pdfPath = Path.Combine(_testDir, "valid_test.pdf");

        // Act
        var hasil = await service.ExportSnapshotAsync(snapshot, pdfPath);

        // Assert
        Assert.NotNull(hasil);
        Assert.True(File.Exists(pdfPath));
        var bytes = await File.ReadAllBytesAsync(pdfPath);

        // 1. Header PDF valid
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);

        // 2. Parsable oleh PdfPig
        using var pdf = PdfDocument.Open(bytes);
        Assert.True(pdf.NumberOfPages >= 1);
        var fullText = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        // 3. Nol kebocoran kredensial atau connection string
        Assert.DoesNotContain("postgres", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Id=", fullText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T32_EksporBahanPeringkatKe10_TercantumLengkapBukanDigantiLimaTeratas()
    {
        // Skenario T32: Pengguna secara spesifik memilih bahan peringkat ke-10.
        // PDF harus mencantumkan bahan peringkat ke-10 tersebut lengkap dengan nama, skor, emisi, dan alasannya.
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);

        var rank10Nama = "Bata Ringan AAC Peringkat 10";
        var rank10Alasan = "Alasan evaluasi teknis lengkap untuk bahan peringkat ke-10 yang dipilih langsung oleh pengguna.";
        var rank10Skor = 58.4;
        var rank10Emisi = 34.2;

        var snapshot = BuatContohSnapshot(b =>
        {
            // Tambahkan 10 bahan, dan pilih spesifik yang peringkat 10
            var list = new List<SnapshotBahan>();
            for (int i = 1; i <= 9; i++)
            {
                list.Add(new SnapshotBahan
                {
                    BahanId = Guid.NewGuid(),
                    KomponenId = Guid.NewGuid(),
                    Jenis = JenisKomponen.Dinding,
                    Peringkat = i,
                    Nama = $"Bahan Dinding Top {i}",
                    SkorKesesuaian = 90 - i * 2,
                    LabelKesesuaian = "Cocok",
                    EmisiAwalKg = 20 + i * 2,
                    Alasan = $"Alasan top {i}"
                });
            }

            list.Add(new SnapshotBahan
            {
                BahanId = Guid.NewGuid(),
                KomponenId = Guid.NewGuid(),
                Jenis = JenisKomponen.Dinding,
                Peringkat = 10,
                Nama = rank10Nama,
                SkorKesesuaian = rank10Skor,
                LabelKesesuaian = "Perlu Pertimbangan",
                EmisiAwalKg = rank10Emisi,
                Alasan = rank10Alasan
            });

            return b with { BahanTerpilih = list };
        });

        var pdfPath = Path.Combine(_testDir, "t32_rank10.pdf");

        // Act
        var laporan = await service.ExportSnapshotAsync(snapshot, pdfPath);

        // Assert
        Assert.True(File.Exists(pdfPath));
        using var pdf = PdfDocument.Open(pdfPath);
        var fullText = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));

        // Verifikasi bahan peringkat 10 hadir dalam teks PDF
        Assert.Contains("Bata Ringan AAC", fullText);
        Assert.Contains("Peringkat 10", fullText);
        Assert.Contains("58.4/100", fullText);
        Assert.Contains("34.2", fullText);
        Assert.Contains("#10", fullText);
        Assert.Contains("Alasan evaluasi teknis lengkap", fullText);
    }

    [Fact]
    public async Task T33_LaporanDuaKomponen_AlasanPanjang_MenghasilkanMultiHalamanDanSimbolLengkap()
    {
        // Skenario T33: PDF laporan dua komponen dengan 20+ material dan alasan Indonesia panjang.
        // Halaman harus >= 2, teks tidak terpotong, simbol °C, m², CO₂ hadir, serta catatan transparansi lengkap.
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);

        var listBahan = new List<SnapshotBahan>();
        for (int i = 1; i <= 12; i++)
        {
            listBahan.Add(new SnapshotBahan
            {
                BahanId = Guid.NewGuid(),
                KomponenId = Guid.NewGuid(),
                Jenis = JenisKomponen.Dinding,
                Peringkat = i,
                Nama = $"Material Dinding Variasi {i} Berkinerja Tinggi",
                SkorKesesuaian = 85.0 - i,
                LabelKesesuaian = i <= 5 ? "Cocok" : "Perlu Pertimbangan",
                EmisiAwalKg = 40.0 + i * 3,
                SubSkorTermal = 80,
                SubSkorLembap = 75,
                SubSkorKorosi = 70,
                SubSkorUsia = 80,
                Alasan = $"Analisis menyeluruh komponen dinding nomor {i}: Ketahanan terhadap curah hujan tropis 2800 mm/tahun dan kelembapan relatif tinggi 82% sangat stabil, namun membutuhkan perlindungan plesteran eksterior tambahan agar terhindar dari rembesan kapiler.",
                SumberProperti = [new SnapshotSumberProperti("Konduktivitas Termal", "0.18 W/m·K", "SNI 03-6386")]
            });
        }
        for (int i = 1; i <= 10; i++)
        {
            listBahan.Add(new SnapshotBahan
            {
                BahanId = Guid.NewGuid(),
                KomponenId = Guid.NewGuid(),
                Jenis = JenisKomponen.Atap,
                Peringkat = i,
                Nama = $"Material Penutup Atap {i} Tahan Korosi Pesisir",
                SkorKesesuaian = 88.0 - i * 1.5,
                LabelKesesuaian = "Cocok",
                EmisiAwalKg = 30.0 + i * 2,
                SubSkorTermal = 85,
                SubSkorLembap = 80,
                SubSkorKorosi = 85,
                SubSkorUsia = 85,
                Alasan = $"Evaluasi atap {i}: Lapisan protektif aluminium-seng memberikan daya tahan prima pada zona korosi sedang dengan jarak pantai 12 km dari pesisir.",
                SumberProperti = [new SnapshotSumberProperti("Faktor Emisi", "2.1 kgCO₂e/kg", "ICE Database v3.0")]
            });
        }

        var snapshot = BuatContohSnapshot(b => b with
        {
            Komponen =
            [
                new SnapshotKomponen(JenisKomponen.Dinding, 120.0),
                new SnapshotKomponen(JenisKomponen.Atap, 85.0)
            ],
            BahanTerpilih = listBahan,
            SertakanJadwal = true,
            JadwalTerkunci =
            [
                new SnapshotPekerjaan
                {
                    PekerjaanId = Guid.NewGuid(),
                    Nama = "Pengecoran Dinding Lantai 1",
                    Jenis = JenisPekerjaan.Pengecoran,
                    TanggalMulai = new DateOnly(2026, 12, 1),
                    TanggalSelesai = new DateOnly(2026, 12, 10),
                    TanggalDikunci = new DateOnly(2026, 12, 5),
                    StatusCuaca = "Layak",
                    AlasanCuaca = ["Probabilitas hujan 15% (di bawah ambang batas)"]
                }
            ]
        });

        var pdfPath = Path.Combine(_testDir, "t33_multipage.pdf");

        // Act
        var hasil = await service.ExportSnapshotAsync(snapshot, pdfPath);

        // Assert
        Assert.True(File.Exists(pdfPath));
        using var pdf = PdfDocument.Open(pdfPath);

        // Memenuhi syarat multi-halaman
        Assert.True(pdf.NumberOfPages >= 2, $"Diharapkan >= 2 halaman, aktual: {pdf.NumberOfPages}");

        var fullText = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));

        // Verifikasi simbol °C, m², CO₂
        Assert.Contains("°C", fullText);
        Assert.Contains("m²", fullText);
        Assert.Contains("CO₂", fullText);

        // Verifikasi kedua komponen tercantum
        Assert.Contains("Dinding: 120.0 m²", fullText);
        Assert.Contains("Atap: 85.0 m²", fullText);

        // Verifikasi disclaimer estimasi dan sumber transparansi
        Assert.Contains("Hasil simulasi emisi adalah proyeksi estimasi model matematis", fullText);
        Assert.Contains("Open-Meteo", fullText);
        Assert.Contains("SNI 03-6386", fullText);
        Assert.Contains("Halaman 1 dari", fullText);
    }

    [Fact]
    public async Task T34_SimpanLaporan_PemeriksaanBerkas_DanMissingFileRecovery()
    {
        // Skenario T34: Ekspor laporan -> metadata tersimpan -> file ada dan checksum cocok ->
        // berkas dipindah/dihapus -> PeriksaBerkasLaporan mendeteksi StatusBerkasLaporan.Hilang tanpa crash.
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);
        var snapshot = BuatContohSnapshot();
        var pdfPath = Path.Combine(_testDir, "t34_report.pdf");

        // Act 1: Export
        var laporan = await service.ExportSnapshotAsync(snapshot, pdfPath);
        Assert.Equal("Tersimpan", laporan.Status);

        // Verifikasi di repository
        var tersimpan = await laporanRepo.GetByIdAsync(laporan.Id);
        Assert.NotNull(tersimpan);
        Assert.Equal(pdfPath, tersimpan.PathBerkas);
        Assert.NotNull(tersimpan.ChecksumSha256);

        // Act 2: Periksa berkas saat ada
        var statusSaatAda = service.PeriksaBerkasLaporan(tersimpan);
        Assert.Equal(StatusBerkasLaporan.Tersedia, statusSaatAda);

        // Act 3: Hapus/pindahkan berkas
        File.Delete(pdfPath);
        Assert.False(File.Exists(pdfPath));

        // Act 4: Periksa berkas saat hilang
        var statusSaatHilang = service.PeriksaBerkasLaporan(tersimpan);
        Assert.Equal(StatusBerkasLaporan.Hilang, statusSaatHilang);

        // Act 5: Ubah isi berkas (checksum berubah)
        await File.WriteAllTextAsync(pdfPath, "isi rusak sembarang bukan pdf asli");
        var statusBerubah = service.PeriksaBerkasLaporan(tersimpan);
        Assert.Equal(StatusBerkasLaporan.BerubahChecksum, statusBerubah);
    }

    [Fact]
    public async Task T35_FolderDitolak_GagalTanpaKlaimSukses_DanDbGagal_PreserveFile_SertaRekonsiliasiIdempoten()
    {
        // Skenario T35 Bagian 1: Path tidak valid / direktori tidak dapat ditulis
        // Harus melempar exception, tidak membuat file parsial/temp, dan tidak mencatat sukses palsu.
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);
        var snapshot = BuatContohSnapshot();

        var invalidPath = "/root/unwritable_wastu_path_test/sub/report.pdf";
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExportSnapshotAsync(snapshot, invalidPath));

        var riwayatKosong = await laporanRepo.ListByProyekAsync(snapshot.ProyekId);
        Assert.Empty(riwayatKosong);

        // Skenario T35 Bagian 2: File berhasil ditulis ke disk, tetapi repository DB gagal (e.g. offline/exception)
        // PDF tetap ada di disk, status laporan "MenungguPencatatan", tercatat di pending metadata lokal.
        var throwingRepo = new ThrowingLaporanRepository();
        var offlineService = new ReportExportService(throwingRepo, pendingMetadataDirectory: _pendingDir);
        var validPath = Path.Combine(_testDir, "t35_offline_file.pdf");

        var laporanPending = await offlineService.ExportSnapshotAsync(snapshot, validPath);

        // File PDF UTUH dan TERSEDIA di disk
        Assert.True(File.Exists(validPath));
        Assert.Equal("MenungguPencatatan", laporanPending.Status);

        // Memeriksa pending metadata di disk
        var pendingFiles = Directory.GetFiles(_pendingDir, "*.pending.json");
        Assert.Single(pendingFiles);

        // Skenario T35 Bagian 3: Rekonsiliasi idempoten saat DB pulih
        // Panggil RekonsiliasiPendingAsync dua kali; hanya tepat 1 catatan yang tersimpan di DB.
        var targetRepo = new LaporanRepositoryInMemory();
        var reconcileService = new ReportExportService(targetRepo, pendingMetadataDirectory: _pendingDir);

        int count1 = await reconcileService.RekonsiliasiPendingAsync();
        Assert.Equal(1, count1);

        var listDb = await targetRepo.ListByProyekAsync(snapshot.ProyekId);
        Assert.Single(listDb);
        Assert.Equal(laporanPending.Id, listDb[0].Id);

        // Panggilan kedua tidak menduplikasi dan menghasilkan 0 rekonsiliasi baru
        int count2 = await reconcileService.RekonsiliasiPendingAsync();
        Assert.Equal(0, count2);

        var listDb2 = await targetRepo.ListByProyekAsync(snapshot.ProyekId);
        Assert.Single(listDb2);
    }

    [Fact]
    public async Task EksporOffline_MencantumkanLabelLuringDanWaktuSnapshot()
    {
        // Verifikasi ekspor snapshot luring memuat catatan umur/status luring secara terbuka
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);

        var waktuDiambil = new DateTimeOffset(2026, 10, 15, 8, 30, 0, TimeSpan.FromHours(7));
        var snapshot = BuatContohSnapshot(b => b with
        {
            Iklim = b.Iklim with
            {
                IsOffline = true,
                Provenance = "Contoh",
                DiambilPada = waktuDiambil,
                CatatanOffline = "Diekspor dari data offline contoh."
            }
        });

        var pdfPath = Path.Combine(_testDir, "offline_test.pdf");

        // Act
        await service.ExportSnapshotAsync(snapshot, pdfPath);

        // Assert
        using var pdf = PdfDocument.Open(pdfPath);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        Assert.Contains("MODE LURING", text);
        Assert.Contains("15/10/2026", text);
    }

    [Fact]
    public async Task T38_PerubahanSetelahPdfDibuat_TidakMengubahPdfMaupunSnapshotLama()
    {
        // Skenario T38: Snapshot laporan bersifat immutable.
        // Pembuatan snapshot baru atau perubahan data proyek di kemudian hari tidak mengubah file PDF lama.
        var laporanRepo = new LaporanRepositoryInMemory();
        var service = new ReportExportService(laporanRepo, pendingMetadataDirectory: _pendingDir);

        var snapshotLama = BuatContohSnapshot(b => b with
        {
            Komponen = [new SnapshotKomponen(JenisKomponen.Dinding, 100.0)]
        });

        var pathLama = Path.Combine(_testDir, "t38_lama.pdf");
        var laporanLama = await service.ExportSnapshotAsync(snapshotLama, pathLama);
        var checksumLama = laporanLama.ChecksumSha256;

        // Simulasi perubahan luas dan rumus di kemudian hari
        var snapshotBaru = snapshotLama with
        {
            Id = Guid.NewGuid(),
            Komponen = [new SnapshotKomponen(JenisKomponen.Dinding, 250.0)],
            VersiModel = "v2.1"
        };

        var pathBaru = Path.Combine(_testDir, "t38_baru.pdf");
        var laporanBaru = await service.ExportSnapshotAsync(snapshotBaru, pathBaru);

        // File lama tidak termutasi
        Assert.Equal(checksumLama, HitungSha256(pathLama));
        Assert.NotEqual(laporanLama.ChecksumSha256, laporanBaru.ChecksumSha256);
        Assert.NotEqual(laporanLama.SnapshotHash, laporanBaru.SnapshotHash);
    }

    private static string HitungSha256(string file)
    {
        using var stream = File.OpenRead(file);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static SnapshotLaporan BuatContohSnapshot(Func<SnapshotLaporan, SnapshotLaporan>? mutator = null)
    {
        var proyekId = Guid.NewGuid();
        var kompDindingId = Guid.NewGuid();
        var kompAtapId = Guid.NewGuid();

        var snapshot = new SnapshotLaporan
        {
            Id = Guid.NewGuid(),
            ProyekId = proyekId,
            NamaProyek = "Rumah Tropis Asri",
            Kota = "Surabaya",
            Latitude = -7.2575,
            Longitude = 112.7521,
            Komponen =
            [
                new SnapshotKomponen(JenisKomponen.Dinding, 100.0),
                new SnapshotKomponen(JenisKomponen.Atap, 70.0)
            ],
            Iklim = new SnapshotIklim
            {
                CurahHujanTahunanMm = 1850.0,
                KelembapanRataPct = 76.5,
                SuhuRataC = 28.2,
                JarakPesisirKm = 8.5,
                TingkatKorosi = "Sedang",
                Provider = "Open-Meteo Historical Climate API",
                Periode = "2014-2023 (10 tahun)",
                Provenance = "ApiHistoris",
                DiambilPada = DateTimeOffset.UtcNow
            },
            BahanTerpilih =
            [
                new SnapshotBahan
                {
                    BahanId = Guid.NewGuid(),
                    KomponenId = kompDindingId,
                    Jenis = JenisKomponen.Dinding,
                    Peringkat = 1,
                    Nama = "Bata Merah Standar",
                    SkorKesesuaian = 82.5,
                    LabelKesesuaian = "Cocok",
                    EmisiAwalKg = 45.0,
                    SubSkorTermal = 85,
                    SubSkorLembap = 80,
                    SubSkorKorosi = 85,
                    SubSkorUsia = 80,
                    Alasan = "Daya tahan tinggi terhadap iklim tropis lembap dan isolasi termal memadai.",
                    SumberProperti = [new SnapshotSumberProperti("Faktor Emisi", "0.24 kgCO₂e/kg", "ICE DB v3.0")]
                }
            ],
            SimulasiHorizon =
            [
                new SnapshotSimulasi
                {
                    EvaluasiId = Guid.NewGuid(),
                    BahanNama = "Bata Merah Standar",
                    Jenis = JenisKomponen.Dinding,
                    HorizonTahun = 30,
                    UsiaPakaiNominal = 25.0,
                    UsiaPakaiEfektif = 22.8,
                    JumlahPenggantian = 1,
                    EmisiAwalKg = 45.0,
                    EmisiKumulatifKg = 90.0,
                    FaktorPenuaanA = 0.5,
                    VersiModel = "v2.0"
                }
            ],
            SertakanJadwal = false,
            JadwalTerkunci = []
        };

        return mutator is not null ? mutator(snapshot) : snapshot;
    }

    private sealed class ThrowingLaporanRepository : ILaporanRepository
    {
        public Task<IReadOnlyList<Laporan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Laporan>>([]);

        public Task<Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Laporan?>(null);

        public Task SimpanAsync(Laporan laporan, CancellationToken ct = default)
            => throw new InvalidOperationException("Koneksi database terputus (simulasi offline/DB error).");

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
