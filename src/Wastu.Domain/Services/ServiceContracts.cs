using Wastu.Domain.Weather;

namespace Wastu.Domain.Services;

/// <summary>Katalog bahan (PRD §F2, §10).</summary>
public interface IMaterialService
{
    Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default);

    Task<Bahan> GetByIdAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Manajemen profil proyek dan komponennya (PRD §F1, §10).</summary>
public interface IProjectService
{
    Task<IReadOnlyList<Proyek>> ListAsync(CancellationToken ct = default);

    Task<Proyek> CreateProjectAsync(string nama, string kota, double latitude, double longitude, CancellationToken ct = default);

    Task<Proyek> CreateProjectAsync(string nama, string kota, double latitude, double longitude,
        double luasDinding, double luasAtap, CancellationToken ct = default);

    Task<Proyek> CreateProjectAsync(string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default);

    Task<ProfilIklim?> UpdateProjectAsync(Guid proyekId, string nama, string kota, double latitude, double longitude,
        double luasDinding, double luasAtap, CancellationToken ct = default);

    Task<ProfilIklim?> UpdateProjectAsync(Guid proyekId, string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default);

    Task DeleteAsync(Guid proyekId, CancellationToken ct = default);

    /// <summary>Perubahan lokasi memicu pengambilan ulang profil iklim (PRD §F1).</summary>
    Task UpdateLocationAsync(Guid proyekId, double latitude, double longitude, CancellationToken ct = default);

    Task AddComponentAsync(Guid proyekId, JenisKomponen jenis, double luasM2, CancellationToken ct = default);

    Task<(Proyek Proyek, ProfilIklim? Profil, string? Peringatan)> CreateWithClimateAsync(
        string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default)
        => Task.FromResult<(Proyek, ProfilIklim?, string?)>((null!, null, null));
}

/// <summary>Analisis iklim lokasi (PRD §F3, §10).</summary>
public interface IClimateAnalysisService
{
    /// <summary>
    /// Mengembalikan profil iklim yang tersimpan, atau mengambil baru dari
    /// <see cref="Abstractions.IHistoricalClimateClient"/> bila belum ada (PRD §8.4).
    /// </summary>
    Task<ProfilIklim> GetOrFetchProfileAsync(Guid proyekId, CancellationToken ct = default);

    /// <summary>Memaksa pengambilan ulang profil iklim atas permintaan pengguna.</summary>
    Task<ProfilIklim> RefreshProfileAsync(Guid proyekId, CancellationToken ct = default);
}

/// <summary>Perbandingan berperingkat dengan skor kesesuaian dan kgCO2e berdampingan (PRD §F4, §10).</summary>
public interface ISuitabilityScoringService
{
    /// <summary>
    /// Mengurutkan bahan yang sesuai untuk satu komponen, dari skor tertinggi ke terendah.
    /// </summary>
    /// <param name="komponenId">Komponen yang dinilai.</param>
    /// <param name="bobot">
    /// Bobot kriteria termal/lembap/korosi/usia. Null memakai bobot equiponderal bawaan (PRD §11.1).
    /// </param>
    Task<IReadOnlyList<Evaluasi>> RankAsync(Guid komponenId, KriteriaBobot? bobot = null, CancellationToken ct = default);

    /// <summary>
    /// Alias perankingan bahan untuk komponen dengan bobot kriteria tertentu (PRD §F4, WP04b).
    /// </summary>
    Task<IReadOnlyList<Evaluasi>> RankMaterialsAsync(Guid komponenId, KriteriaBobot? bobot = null, CancellationToken ct = default);
}

/// <summary>Simulasi emisi jangka panjang dengan pengganda penggantian dini (PRD §F5, §10).</summary>
public interface IEmissionSimulationService
{
    /// <summary>
    /// Memproyeksikan emisi kumulatif satu evaluasi sepanjang <paramref name="horizonTahun"/> tahun.
    /// </summary>
    /// <remarks>
    /// Hasil adalah estimasi model, bukan hasil pengukuran (PRD §11.4, §14 R2).
    /// </remarks>
    Task<SimulasiEmisi> SimulateAsync(Guid evaluasiId, int horizonTahun, CancellationToken ct = default);
}

/// <summary>
/// Penjadwalan pekerjaan sensitif cuaca (PRD §F6, §10).
/// </summary>
public interface IWorkSchedulingService
{
    /// <summary>Menilai kelayakan tiap hari dalam rentang rencana sebuah pekerjaan.</summary>
    Task<IReadOnlyList<PenilaianKesiapanHari>> EvaluateDaysAsync(Guid pekerjaanId, CancellationToken ct = default);

    /// <summary>
    /// Mengunci satu tanggal sebagai jadwal final. Pengguna tetap boleh mengunci
    /// hari yang ditandai tidak layak karena Wastu memberi informasi, bukan memaksakan
    /// keputusan (PRD §14 R7).
    /// </summary>
    Task LockDateAsync(Guid pekerjaanId, DateOnly tanggal, CancellationToken ct = default);

    /// <summary>
    /// Menilai kelayakan hari kerja secara detail dengan status bertingkat (Layak/Waspada/TidakLayak/DataTidakTersedia).
    /// </summary>
    Task<IReadOnlyList<HasilEvaluasiCuaca>> EvaluateDaysDetailedAsync(Guid pekerjaanId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<HasilEvaluasiCuaca>>([]);

    /// <summary>
    /// Mengunci satu tanggal dengan membawa konfirmasi persetujuan risiko dan catatan pengguna (PRD §12.2).
    /// </summary>
    Task LockDateAsync(Guid pekerjaanId, DateOnly tanggal, KonfirmasiPenguncian konfirmasi, CancellationToken ct = default)
        => LockDateAsync(pekerjaanId, tanggal, ct);
}

/// <summary>Ekspor laporan rekomendasi ke PDF (PRD §F7, §10).</summary>
public interface IReportExportService
{
    /// <summary>Menghasilkan berkas PDF ringkasan rekomendasi dan mencatatnya.</summary>
    Task<Laporan> ExportAsync(Guid proyekId, OpsiEkspor opsi, CancellationToken ct = default);

    /// <summary>Mendapatkan riwayat laporan PDF yang pernah diekspor untuk satu proyek.</summary>
    Task<IReadOnlyList<Laporan>> ListReportsAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Laporan>>([]);

    /// <summary>Mendapatkan data laporan berdasarkan identitas dokumen.</summary>
    Task<Laporan?> GetReportByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Laporan?>(null);
}

/// <summary>Hasil penilaian kelayakan satu hari kerja (PRD §F6).</summary>
public sealed record PenilaianKesiapanHari(DateOnly Tanggal, bool Layak, string? Alasan)
{
    public static PenilaianKesiapanHari HariTidakLayak(DateOnly tanggal, string alasan) => new(tanggal, false, alasan);

    public static PenilaianKesiapanHari HariLayak(DateOnly tanggal) => new(tanggal, true, null);
}

/// <summary>Pilihan saat mengekspor laporan (PRD §F7).</summary>
public sealed record OpsiEkspor
{
    public OpsiEkspor(int horizonTahun, bool sertakanJadwal, string? pathTujuan = null)
        : this(horizonTahun, sertakanJadwal, pathTujuan, null, null, null, null)
    {
    }

    public OpsiEkspor(
        int horizonTahun,
        bool sertakanJadwal,
        string? pathTujuan = null,
        IReadOnlyList<Guid>? komponenDipilih = null,
        IReadOnlyList<Guid>? bahanDipilih = null,
        IReadOnlyList<int>? horizonDipilih = null,
        string? snapshotHash = null)
    {
        HorizonTahun = horizonTahun;
        SertakanJadwal = sertakanJadwal;
        PathTujuan = pathTujuan;
        KomponenDipilih = komponenDipilih;
        BahanDipilih = bahanDipilih;
        HorizonDipilih = horizonDipilih;
        SnapshotHash = snapshotHash;
    }

    public int HorizonTahun { get; init; }
    public bool SertakanJadwal { get; init; }
    public string? PathTujuan { get; init; }
    public IReadOnlyList<Guid>? KomponenDipilih { get; init; }
    public IReadOnlyList<Guid>? BahanDipilih { get; init; }
    public IReadOnlyList<int>? HorizonDipilih { get; init; }
    public string? SnapshotHash { get; init; }
}

/// <summary>Laporan yang sudah diekspor sebagai berkas lokal (PRD §F7).</summary>
public sealed record Laporan
{
    public Laporan(Guid id, Guid proyekId, string pathBerkas, DateTimeOffset dibuatPada)
        : this(id, proyekId, pathBerkas, dibuatPada, null, null, null, "Tersimpan")
    {
    }

    public Laporan(
        Guid id,
        Guid proyekId,
        string pathBerkas,
        DateTimeOffset dibuatPada,
        string? checksumSha256 = null,
        string? snapshotHash = null,
        long? ukuranByte = null,
        string status = "Tersimpan")
    {
        Id = id;
        ProyekId = proyekId;
        PathBerkas = pathBerkas;
        DibuatPada = dibuatPada;
        ChecksumSha256 = checksumSha256;
        SnapshotHash = snapshotHash;
        UkuranByte = ukuranByte;
        Status = status;
    }

    public Guid Id { get; init; }
    public Guid ProyekId { get; init; }
    public string PathBerkas { get; init; }
    public DateTimeOffset DibuatPada { get; init; }
    public string? ChecksumSha256 { get; init; }
    public string? SnapshotHash { get; init; }
    public long? UkuranByte { get; init; }
    public string Status { get; init; } = "Tersimpan";
}
