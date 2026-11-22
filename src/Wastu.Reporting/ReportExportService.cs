using System.Security.Cryptography;
using System.Text.Json;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Catalog;
using Wastu.Domain.Climate;
using Wastu.Domain.Emissions;
using Wastu.Domain.Scoring;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;

namespace Wastu.Reporting;

public sealed class ReportExportService : IReportExportService
{
    private readonly ILaporanRepository _laporanRepository;
    private readonly IProyekRepository? _proyekRepository;
    private readonly IEvaluasiRepository? _evaluasiRepository;
    private readonly IMaterialRepository? _materialRepository;
    private readonly ISumberBahanRepository? _sumberBahanRepository;
    private readonly IPekerjaanRepository? _pekerjaanRepository;
    private readonly IKonfigurasiModelRepository? _konfigurasiModelRepository;
    private readonly string _pendingMetadataDir;
    private readonly PdfReportBuilder _pdfBuilder;

    public ReportExportService(
        ILaporanRepository? laporanRepository = null,
        IProyekRepository? proyekRepository = null,
        IEvaluasiRepository? evaluasiRepository = null,
        IMaterialRepository? materialRepository = null,
        ISumberBahanRepository? sumberBahanRepository = null,
        IPekerjaanRepository? pekerjaanRepository = null,
        IKonfigurasiModelRepository? konfigurasiModelRepository = null,
        string? pendingMetadataDirectory = null)
    {
        _laporanRepository = laporanRepository ?? new LocalMemoryLaporanRepository();
        _proyekRepository = proyekRepository;
        _evaluasiRepository = evaluasiRepository;
        _materialRepository = materialRepository;
        _sumberBahanRepository = sumberBahanRepository;
        _pekerjaanRepository = pekerjaanRepository;
        _konfigurasiModelRepository = konfigurasiModelRepository;

        _pendingMetadataDir = pendingMetadataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wastu",
            "pending_reports");

        _pdfBuilder = new PdfReportBuilder();
    }

    public async Task<Laporan> ExportAsync(Guid proyekId, OpsiEkspor opsi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(opsi);
        if (string.IsNullOrWhiteSpace(opsi.PathTujuan))
        {
            throw new ArgumentException("Path tujuan berkas PDF wajib diisi.", nameof(opsi));
        }

        if (_proyekRepository is null)
        {
            throw new InvalidOperationException("IProyekRepository diperlukan untuk mengekspor laporan berdasarkan ID proyek.");
        }

        var proyek = await _proyekRepository.GetByIdAsync(proyekId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Proyek dengan ID {proyekId} tidak ditemukan.");

        var komponenList = await _proyekRepository.ListKomponenAsync(proyekId, ct).ConfigureAwait(false);
        if (komponenList.Count == 0)
        {
            throw new InvalidOperationException("Proyek belum memiliki komponen bangunan.");
        }

        var profilIklim = await _proyekRepository.GetProfilIklimAsync(proyekId, ct).ConfigureAwait(false);
        var config = _konfigurasiModelRepository is not null
            ? await _konfigurasiModelRepository.GetActiveAsync(ct).ConfigureAwait(false)
            : KonfigurasiModel.Bawaan;

        var snapshot = await BuildSnapshotAsync(proyek, komponenList, profilIklim, config, opsi, ct).ConfigureAwait(false);
        return await ExportSnapshotAsync(snapshot, opsi.PathTujuan, ct).ConfigureAwait(false);
    }

    public async Task<Laporan> ExportSnapshotAsync(SnapshotLaporan snapshot, string pathTujuan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(pathTujuan))
        {
            throw new ArgumentException("Path tujuan berkas PDF wajib diisi.", nameof(pathTujuan));
        }

        // Validate selection: must not be empty
        if (snapshot.BahanTerpilih.Count == 0 && snapshot.Komponen.Count == 0)
        {
            throw new InvalidOperationException("Laporan tidak dapat diekspor dengan pilihan komponen dan material kosong.");
        }

        var fullPath = Path.GetFullPath(pathTujuan);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Finalize hash
        var hash = snapshot.HitungHash();
        var snapshotFinal = snapshot with { SnapshotHash = hash };

        // Generate PDF document
        var document = _pdfBuilder.Build(snapshotFinal);

        // Atomic write via temp file
        var tempFile = Path.Combine(
            string.IsNullOrEmpty(dir) ? Path.GetTempPath() : dir,
            $".wastu_export_{Guid.NewGuid():N}.tmp");

        string sha256;
        long fileSize;

        try
        {
            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                document.Save(stream, false);
            }

            fileSize = new FileInfo(tempFile).Length;
            sha256 = HitungChecksumSha256(tempFile);

            if (File.Exists(fullPath))
            {
                File.Replace(tempFile, fullPath, null);
            }
            else
            {
                File.Move(tempFile, fullPath);
            }
        }
        catch
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            }
            throw;
        }

        var laporan = new Laporan(
            id: snapshot.Id,
            proyekId: snapshot.ProyekId,
            pathBerkas: fullPath,
            dibuatPada: snapshot.DibuatPadaUtc,
            checksumSha256: sha256,
            snapshotHash: hash,
            ukuranByte: fileSize,
            status: "Tersimpan");

        // Try persisting to repository
        try
        {
            await _laporanRepository.SimpanAsync(laporan, ct).ConfigureAwait(false);
            return laporan;
        }
        catch (Exception ex)
        {
            // Offline / DB-failure resilience (T35):
            // The PDF file is preserved on disk.
            // Record local pending metadata file for idempotent reconciliation.
            await RecordPendingMetadataAsync(laporan, ex.Message).ConfigureAwait(false);

            return laporan with { Status = "MenungguPencatatan" };
        }
    }

    public async Task<IReadOnlyList<Laporan>> ListReportsAsync(Guid proyekId, CancellationToken ct = default)
    {
        return await _laporanRepository.ListByProyekAsync(proyekId, ct).ConfigureAwait(false);
    }

    public async Task<Laporan?> GetReportByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _laporanRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
    }

    public StatusBerkasLaporan PeriksaBerkasLaporan(Laporan laporan)
    {
        ArgumentNullException.ThrowIfNull(laporan);

        if (string.IsNullOrWhiteSpace(laporan.PathBerkas) || !File.Exists(laporan.PathBerkas))
        {
            return StatusBerkasLaporan.Hilang;
        }

        if (!string.IsNullOrWhiteSpace(laporan.ChecksumSha256))
        {
            try
            {
                var aktual = HitungChecksumSha256(laporan.PathBerkas);
                if (!string.Equals(aktual, laporan.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusBerkasLaporan.BerubahChecksum;
                }
            }
            catch
            {
                return StatusBerkasLaporan.Hilang;
            }
        }

        return StatusBerkasLaporan.Tersedia;
    }

    public async Task<int> RekonsiliasiPendingAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_pendingMetadataDir))
        {
            return 0;
        }

        var files = Directory.GetFiles(_pendingMetadataDir, "*.pending.json");
        int count = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
                var meta = JsonSerializer.Deserialize<PendingReportMetadata>(json);
                if (meta is null) continue;

                var existing = await _laporanRepository.GetByIdAsync(meta.Id, ct).ConfigureAwait(false);
                if (existing is null)
                {
                    var laporan = new Laporan(
                        id: meta.Id,
                        proyekId: meta.ProyekId,
                        pathBerkas: meta.PathBerkas,
                        dibuatPada: meta.DibuatPada,
                        checksumSha256: meta.ChecksumSha256,
                        snapshotHash: meta.SnapshotHash,
                        ukuranByte: meta.UkuranByte,
                        status: "Tersimpan");

                    await _laporanRepository.SimpanAsync(laporan, ct).ConfigureAwait(false);
                }

                // Remove pending file after successful persistence
                File.Delete(file);
                count++;
            }
            catch
            {
                // If DB is still unreachable, keep file for next attempt
            }
        }

        return count;
    }

    private async Task RecordPendingMetadataAsync(Laporan laporan, string errorMessage)
    {
        if (!Directory.Exists(_pendingMetadataDir))
        {
            Directory.CreateDirectory(_pendingMetadataDir);
        }

        var pending = new PendingReportMetadata
        {
            Id = laporan.Id,
            ProyekId = laporan.ProyekId,
            PathBerkas = laporan.PathBerkas,
            DibuatPada = laporan.DibuatPada,
            ChecksumSha256 = laporan.ChecksumSha256 ?? string.Empty,
            SnapshotHash = laporan.SnapshotHash,
            UkuranByte = laporan.UkuranByte ?? 0,
            Status = "MenungguPencatatan",
            DicatatLokalPadaUtc = DateTimeOffset.UtcNow,
            ErrorPesanTerakhir = errorMessage
        };

        var filePath = Path.Combine(_pendingMetadataDir, $"{laporan.Id}.pending.json");
        var tempFile = Path.Combine(_pendingMetadataDir, $".{laporan.Id}.pending.tmp");

        var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(tempFile, json).ConfigureAwait(false);

        if (File.Exists(filePath))
        {
            File.Replace(tempFile, filePath, null);
        }
        else
        {
            File.Move(tempFile, filePath);
        }
    }

    private static string HitungChecksumSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var bytes = SHA256.HashData(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<SnapshotLaporan> BuildSnapshotAsync(
        Proyek proyek,
        IReadOnlyList<Komponen> semuaKomponen,
        ProfilIklim? iklim,
        KonfigurasiModel config,
        OpsiEkspor opsi,
        CancellationToken ct)
    {
        var komponenAktif = opsi.KomponenDipilih is { Count: > 0 }
            ? semuaKomponen.Where(k => opsi.KomponenDipilih.Contains(k.Id)).ToList()
            : semuaKomponen.ToList();

        var snapshotKomponen = komponenAktif
            .Select(k => new SnapshotKomponen(k.Jenis, k.LuasM2))
            .ToList();

        var snapshotIklim = iklim is not null
            ? new SnapshotIklim
            {
                CurahHujanTahunanMm = iklim.CurahHujanTahunanMm,
                KelembapanRataPct = iklim.KelembapanRataPct,
                SuhuRataC = iklim.SuhuRataC,
                JarakPesisirKm = iklim.JarakPesisirKm,
                TingkatKorosi = iklim.TingkatKorosi.ToString(),
                Provider = iklim.Metadata?.Penyedia ?? "Open-Meteo Historical Climate API",
                Periode = iklim.Metadata?.PeriodeAwal.HasValue == true && iklim.Metadata?.PeriodeAkhir.HasValue == true
                    ? $"{iklim.Metadata.PeriodeAwal:yyyy}-{iklim.Metadata.PeriodeAkhir:yyyy}"
                    : "10 tahun historis (agregasi)",
                Provenance = iklim.Provenance.ToString(),
                DiambilPada = iklim.DiambilPada,
                IsOffline = iklim.Provenance == ProvenanceIklim.Contoh || iklim.Provenance == ProvenanceIklim.EstimasiBawaan,
                CatatanOffline = iklim.Provenance == ProvenanceIklim.Contoh ? "Diekspor dari data offline contoh." : null
            }
            : new SnapshotIklim();

        // Materials & Rankings
        var bahanList = new List<SnapshotBahan>();
        var simulasiList = new List<SnapshotSimulasi>();

        foreach (var komp in komponenAktif)
        {
            IReadOnlyList<Evaluasi> evaluasiKomp = _evaluasiRepository is not null
                ? await _evaluasiRepository.ListByKomponenAsync(komp.Id, ct).ConfigureAwait(false)
                : [];

            // Sort by score descending to get true ranking
            var sortedEvaluasi = evaluasiKomp.OrderByDescending(e => e.SkorKesesuaian).ToList();

            // Filter by user selection if specified, otherwise include all or top materials
            var targetEvaluasi = opsi.BahanDipilih is { Count: > 0 }
                ? sortedEvaluasi.Where(e => opsi.BahanDipilih.Contains(e.BahanId)).ToList()
                : sortedEvaluasi;

            for (int i = 0; i < targetEvaluasi.Count; i++)
            {
                var eval = targetEvaluasi[i];
                var rank = sortedEvaluasi.FindIndex(e => e.Id == eval.Id) + 1;

                Bahan? bahan = _materialRepository is not null
                    ? await _materialRepository.GetByIdAsync(eval.BahanId, ct).ConfigureAwait(false)
                    : null;

                var bahanNama = bahan?.Nama ?? $"Bahan {eval.BahanId:N}[..6]";

                var sumberList = new List<SnapshotSumberProperti>();
                if (_sumberBahanRepository is not null)
                {
                    var sumber = await _sumberBahanRepository.ListByBahanIdAsync(eval.BahanId, ct).ConfigureAwait(false);
                    sumberList.AddRange(sumber.Select(s => new SnapshotSumberProperti(s.Properti, s.Referensi, s.Status.ToString(), s.Versi)));
                }

                var label = config.LabelUntuk(eval.SkorKesesuaian).ToString();
                var labelFormatted = label switch
                {
                    "Cocok" => "Cocok",
                    "PerluPertimbangan" => "Perlu Pertimbangan",
                    _ => "Tidak Disarankan"
                };

                bahanList.Add(new SnapshotBahan
                {
                    BahanId = eval.BahanId,
                    KomponenId = komp.Id,
                    Jenis = komp.Jenis,
                    Peringkat = rank > 0 ? rank : (i + 1),
                    Nama = bahanNama,
                    SkorKesesuaian = eval.SkorKesesuaian,
                    LabelKesesuaian = labelFormatted,
                    EmisiAwalKg = eval.EmisiAwalKg,
                    SubSkorTermal = eval.SubSkor.Termal,
                    SubSkorLembap = eval.SubSkor.Lembap,
                    SubSkorKorosi = eval.SubSkor.Korosi,
                    SubSkorUsia = eval.SubSkor.Usia,
                    Alasan = eval.Alasan,
                    SumberProperti = sumberList
                });

                // Horizon simulations for this material
                var horizons = opsi.HorizonDipilih is { Count: > 0 }
                    ? opsi.HorizonDipilih
                    : [opsi.HorizonTahun];

                var usiaNominal = bahan?.UsiaPakaiNominalTahun ?? 20.0;

                foreach (var h in horizons)
                {
                    var sim = PerhitunganSimulasi.Simulasikan(eval.Id, eval, usiaNominal, h, config);

                    simulasiList.Add(new SnapshotSimulasi
                    {
                        EvaluasiId = eval.Id,
                        BahanNama = bahanNama,
                        Jenis = komp.Jenis,
                        HorizonTahun = h,
                        UsiaPakaiNominal = usiaNominal,
                        UsiaPakaiEfektif = sim.UsiaPakaiEfektifTahun,
                        JumlahPenggantian = sim.JumlahPenggantian,
                        EmisiAwalKg = eval.EmisiAwalKg,
                        EmisiKumulatifKg = sim.EmisiKumulatifKg,
                        FaktorPenuaanA = config.FaktorPenuaanA,
                        VersiModel = config.VersiModel
                    });
                }
            }
        }

        // Schedules
        var jadwalList = new List<SnapshotPekerjaan>();
        if (opsi.SertakanJadwal && _pekerjaanRepository is not null)
        {
            var pekerjaan = await _pekerjaanRepository.ListByProyekAsync(proyek.Id, ct).ConfigureAwait(false);
            var terkunci = pekerjaan.Where(p => p.TanggalDikunci.HasValue).ToList();

            foreach (var j in terkunci)
            {
                jadwalList.Add(new SnapshotPekerjaan
                {
                    PekerjaanId = j.Id,
                    Nama = j.Nama,
                    Jenis = j.Jenis,
                    TanggalMulai = j.TanggalMulai,
                    TanggalSelesai = j.TanggalSelesai,
                    TanggalDikunci = j.TanggalDikunci,
                    StatusCuaca = j.Status == StatusPekerjaan.Terkunci ? "Terkunci" : j.Status.ToString(),
                    KonfirmasiRisiko = j.Konfirmasi?.SetujuiRisiko ?? false,
                    KonfirmasiDataLama = j.Konfirmasi?.SetujuiDataLama ?? false,
                    Catatan = j.Konfirmasi?.Catatan
                });
            }
        }

        return new SnapshotLaporan
        {
            ProyekId = proyek.Id,
            NamaProyek = proyek.Nama,
            Kota = proyek.Kota,
            Latitude = proyek.Latitude,
            Longitude = proyek.Longitude,
            Komponen = snapshotKomponen,
            Iklim = snapshotIklim,
            BahanTerpilih = bahanList,
            SimulasiHorizon = simulasiList,
            SertakanJadwal = opsi.SertakanJadwal,
            JadwalTerkunci = jadwalList,
            DibuatPadaUtc = DateTimeOffset.UtcNow,
            DibuatPadaWib = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)),
            VersiModel = config.VersiModel
        };
    }

    private sealed class LocalMemoryLaporanRepository : ILaporanRepository
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Laporan> _dict = new();

        public Task<IReadOnlyList<Laporan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
        {
            IReadOnlyList<Laporan> list = _dict.Values.Where(l => l.ProyekId == proyekId).OrderByDescending(l => l.DibuatPada).ToList();
            return Task.FromResult(list);
        }

        public Task<Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_dict.GetValueOrDefault(id));

        public Task SimpanAsync(Laporan laporan, CancellationToken ct = default)
        {
            _dict[laporan.Id] = laporan;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            _dict.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}
