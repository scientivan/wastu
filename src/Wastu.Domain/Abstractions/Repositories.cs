namespace Wastu.Domain.Abstractions;

/// <summary>Sumber data bahan (PRD §F2). Implementasi: EF Core + Npgsql di Wastu.Data.</summary>
public interface IMaterialRepository
{
    Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default);

    Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Sumber data proyek dan profil iklimnya (PRD §F1, §F3).</summary>
public interface IProyekRepository
{
    Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Komponen>> ListKomponenAsync(Guid proyekId, CancellationToken ct = default);

    Task AddAsync(Proyek proyek, CancellationToken ct = default);

    Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default);

    Task SimpanProfilIklimAsync(ProfilIklim profil, CancellationToken ct = default);
}

/// <summary>Penyimpanan hasil penilaian dan simulasi (PRD §F4, §F5).</summary>
public interface IEvaluasiRepository
{
    Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid komponenId, CancellationToken ct = default);

    Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default);

    Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default);
}