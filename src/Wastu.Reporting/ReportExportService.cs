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

public sealed partial class ReportExportService : IReportExportService
{
    private readonly ILaporanRepository _laporanRepository;
    private readonly IProyekRepository? _proyekRepository;
    private readonly IEvaluasiRepository? _evaluasiRepository;
    private readonly IPekerjaanRepository? _pekerjaanRepository;
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
        _pekerjaanRepository = pekerjaanRepository;
        // Legacy constructor arguments remain source-compatible. Reports read captured
        // evaluation inputs, never current material/source/model repositories.

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

        var snapshot = await BuildSnapshotAsync(proyek, komponenList, opsi, ct).ConfigureAwait(false);
        return await ExportSnapshotAsync(snapshot, opsi.PathTujuan, ct).ConfigureAwait(false);
    }

    public async Task<Laporan> ExportSnapshotAsync(SnapshotLaporan snapshot, string pathTujuan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(pathTujuan))
        {
            throw new ArgumentException("Path tujuan berkas PDF wajib diisi.", nameof(pathTujuan));
        }

        // Validate selection: must not be empty
        if (snapshot.BahanTerpilih.Count == 0 || snapshot.Komponen.Count == 0)
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
        using var document = await Task.Run(() => _pdfBuilder.Build(snapshotFinal), ct).ConfigureAwait(false);

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

            ct.ThrowIfCancellationRequested();
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
            status: "Tersimpan", snapshotJson: JsonSerializer.Serialize(snapshotFinal));

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
            await RecordPendingMetadataAsync(laporan, ex.GetType().Name).ConfigureAwait(false);

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

    public async Task<IReadOnlyList<Laporan>> ReadPendingReportsAsync(CancellationToken ct = default)
    {
        var reports = new List<Laporan>();
        if (!Directory.Exists(_pendingMetadataDir)) return reports;
        foreach (var file in Directory.GetFiles(_pendingMetadataDir, "*.pending.json"))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var meta = JsonSerializer.Deserialize<PendingReportMetadata>(await File.ReadAllTextAsync(file, ct));
                if (meta is not null) reports.Add(new Laporan(meta.Id, meta.ProyekId, meta.PathBerkas,
                    meta.DibuatPada, meta.ChecksumSha256, meta.SnapshotHash, meta.UkuranByte, "MenungguPencatatan", meta.SnapshotJson));
            }
            catch (Exception ex) when (ex is JsonException or IOException) { }
        }
        return reports;
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
                if (existing is null || existing.SnapshotHash == meta.SnapshotHash)
                {
                    var laporan = new Laporan(
                        id: meta.Id,
                        proyekId: meta.ProyekId,
                        pathBerkas: meta.PathBerkas,
                        dibuatPada: meta.DibuatPada,
                        checksumSha256: meta.ChecksumSha256,
                        snapshotHash: meta.SnapshotHash,
                        ukuranByte: meta.UkuranByte,
                        status: "Tersimpan", snapshotJson: meta.SnapshotJson);

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
            SnapshotJson = laporan.SnapshotJson,
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
