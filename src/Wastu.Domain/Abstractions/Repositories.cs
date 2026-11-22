using Wastu.Domain.Services;
using Wastu.Domain.Weather;

namespace Wastu.Domain.Abstractions;

/// <summary>Sumber data bahan (PRD §F2). Implementasi: EF Core + Npgsql di Wastu.Data.</summary>
public interface IMaterialRepository
{
    Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default);

    Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Sumber data proyek, komponennya, dan profil iklimnya (PRD §F1, §F3).</summary>
public interface IProyekRepository
{
    Task<IReadOnlyList<Proyek>> ListAsync(CancellationToken ct = default);

    Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null);

    /// <summary>Menyimpan perubahan proyek dan menghapus profil iklim bila koordinat berubah.</summary>
    Task UpdateAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Komponen>> ListKomponenAsync(Guid proyekId, CancellationToken ct = default);

    Task<Komponen?> GetKomponenAsync(Guid id, CancellationToken ct = default);

    Task AddKomponenAsync(Komponen komponen, CancellationToken ct = default);

    Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan atau menimpa profil iklim milik sebuah proyek.
    /// </summary>
    Task SimpanProfilIklimAsync(Guid proyekId, ProfilIklim profil, CancellationToken ct = default, Proyek? lokasi = null);
}

/// <summary>Penyimpanan hasil penilaian dan simulasi (PRD §F4, §F5).</summary>
public interface IEvaluasiRepository
{
    Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid komponenId, CancellationToken ct = default);

    Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default);

    Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default);
}

/// <summary>Penyimpanan sumber rujukan dan transparansi properti bahan (PRD-R09).</summary>
public interface ISumberBahanRepository
{
    Task<IReadOnlyList<SumberPropertiBahan>> ListByBahanIdAsync(Guid bahanId, CancellationToken ct = default);

    Task SimpanAsync(Guid bahanId, IReadOnlyList<SumberPropertiBahan> sumber, CancellationToken ct = default);
}

/// <summary>Penyimpanan batch hasil evaluasi berversi dan ber-hash (PRD §4.3, PRD-R10).</summary>
public interface IBatchEvaluasiRepository
{
    Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default);

    Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default);

    Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid komponenId, CancellationToken ct = default);

    Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid proyekId, CancellationToken ct = default);

    /// <summary>
    /// Mencari batch tersimpan yang identik untuk satu komponen berdasarkan hash
    /// masukan dan versi model (PRD §4.3, §9.5). Implementasi bawaan memeriksa batch
    /// terbaru; adapter dapat menimpa dengan pencarian terindeks tanpa mengubah
    /// pemanggil, sehingga render ulang tidak membuat batch baru.
    /// </summary>
    async Task<BatchEvaluasi?> GetByHashAsync(
        Guid komponenId,
        string hashInput,
        string versiModel,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashInput);
        ArgumentException.ThrowIfNullOrWhiteSpace(versiModel);

        BatchEvaluasi? terbaru = await GetLatestBatchByKomponenAsync(komponenId, ct).ConfigureAwait(false);
        return terbaru is not null
            && string.Equals(terbaru.HashInput, hashInput, StringComparison.Ordinal)
            && string.Equals(terbaru.VersiModel, versiModel, StringComparison.Ordinal)
                ? terbaru
                : null;
    }
}

/// <summary>Penyimpanan rencana pekerjaan lapangan sensitif cuaca (PRD §F6, §12).</summary>
public interface IPekerjaanRepository
{
    Task<IReadOnlyList<Pekerjaan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default);

    Task<Pekerjaan?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Pekerjaan pekerjaan, CancellationToken ct = default);

    Task UpdateAsync(Pekerjaan pekerjaan, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Penyimpanan cache prakiraan cuaca per lokasi dan revisi koordinat (PRD §8.3, PRD-R08).</summary>
public interface IPrakiraanCacheRepository
{
    Task<HasilPrakiraan?> GetCacheAsync(double lat, double lon, int revisiLokasi = 1, CancellationToken ct = default);

    Task SimpanCacheAsync(double lat, double lon, HasilPrakiraan hasil, CancellationToken ct = default);

    Task PruneExpiredAsync(DateTimeOffset sebelumUtc, CancellationToken ct = default);
}

/// <summary>Penyimpanan ambang layak-kerja spesifik pekerjaan dan produk (PRD §12, PRD-R05).</summary>
public interface IAmbangPekerjaanRepository
{
    Task<AmbangPekerjaan?> GetByPekerjaanIdAsync(Guid pekerjaanId, CancellationToken ct = default);

    Task<AmbangPekerjaan?> GetByJenisAsync(JenisPekerjaan jenis, CancellationToken ct = default);

    Task SimpanOverrideAsync(Guid pekerjaanId, AmbangPekerjaan ambang, CancellationToken ct = default);
}

/// <summary>Penyimpanan konfigurasi model perhitungan berversi (PRD-R11).</summary>
public interface IKonfigurasiModelRepository
{
    Task<KonfigurasiModel> GetActiveAsync(CancellationToken ct = default);

    Task<KonfigurasiModel?> GetByVersiAsync(string versiModel, CancellationToken ct = default);

    Task SimpanAsync(KonfigurasiModel konfigurasi, CancellationToken ct = default);
}

/// <summary>Penyimpanan riwayat dan metadata berkas laporan rekomendasi yang diekspor (PRD §F7).</summary>
public interface ILaporanRepository
{
    Task<IReadOnlyList<Laporan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default);

    Task<Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task SimpanAsync(Laporan laporan, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
