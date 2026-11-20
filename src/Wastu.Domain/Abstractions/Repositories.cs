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
    /// <remarks>
    /// <paramref name="proyekId"/> diterima terpisah, bukan dibaca dari
    /// <paramref name="profil"/>, supaya sisi ambil dan sisi simpan memakai bentuk
    /// argumen yang sama dan adapter dapat melakukan upsert pada kunci
    /// <c>profil_iklim.bangunan_id</c> tanpa pernah mengarang nilai.
    /// </remarks>
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
