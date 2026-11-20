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

    Task<ProfilIklim?> UpdateProjectAsync(Guid proyekId, string nama, string kota, double latitude, double longitude,
        double luasDinding, double luasAtap, CancellationToken ct = default);

    Task DeleteAsync(Guid proyekId, CancellationToken ct = default);

    /// <summary>Perubahan lokasi memicu pengambilan ulang profil iklim (PRD §F1).</summary>
    Task UpdateLocationAsync(Guid proyekId, double latitude, double longitude, CancellationToken ct = default);

    Task AddComponentAsync(Guid proyekId, JenisKomponen jenis, double luasM2, CancellationToken ct = default);
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
/// <remarks>
/// Kontrak saja yang dideklarasikan di sini. Logika ambang dan algoritme penjadwalan
/// adalah milik frontend developer karena PRD §16.1 menugaskan §12 kepadanya.
/// </remarks>
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
}

/// <summary>Ekspor laporan rekomendasi ke PDF (PRD §F7, §10).</summary>
public interface IReportExportService
{
    /// <summary>Menghasilkan berkas PDF ringkasan rekomendasi dan mencatatnya.</summary>
    Task<Laporan> ExportAsync(Guid proyekId, OpsiEkspor opsi, CancellationToken ct = default);
}

/// <summary>Hasil penilaian kelayakan satu hari kerja (PRD §F6).</summary>
public sealed record PenilaianKesiapanHari(DateOnly Tanggal, bool Layak, string? Alasan)
{
    public static PenilaianKesiapanHari HariTidakLayak(DateOnly tanggal, string alasan) => new(tanggal, false, alasan);

    public static PenilaianKesiapanHari HariLayak(DateOnly tanggal) => new(tanggal, true, null);
}

/// <summary>Pilihan saat mengekspor laporan (PRD §F7).</summary>
public sealed record OpsiEkspor(int HorizonTahun, bool SertakanJadwal, string? PathTujuan = null);

/// <summary>Laporan yang sudah diekspor sebagai berkas lokal (PRD §F7).</summary>
public sealed record Laporan(Guid Id, Guid ProyekId, string PathBerkas, DateTimeOffset DibuatPada);
