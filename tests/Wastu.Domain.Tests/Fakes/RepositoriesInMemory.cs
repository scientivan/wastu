using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Tests.Fakes;

/// <summary>
/// Repository in-memory untuk menguji orkestrasi service tanpa database.
/// </summary>
/// <remarks>
/// Sengaja ditulis tangan, bukan memakai pustaka mocking: kontrak repository-nya
/// kecil, dan implementasi nyata lebih mudah dibaca daripada ekspektasi mock.
/// Isinya cuma tambah dan ambil, tanpa logika bisnis apa pun.
/// </remarks>
internal sealed class MaterialRepositoryInMemory : IMaterialRepository
{
    private readonly List<Bahan> _bahan = [];

    public MaterialRepositoryInMemory(params Bahan[] bahan) => _bahan.AddRange(bahan);

    public Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default)
    {
        IReadOnlyList<Bahan> hasil = _bahan.Where(b => b.Jenis == jenis).ToList();
        return Task.FromResult(hasil);
    }

    public Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_bahan.FirstOrDefault(b => b.Id == id));
}

internal sealed class ProyekRepositoryInMemory : IProyekRepository
{
    private readonly Dictionary<Guid, Komponen> _komponen = [];
    private readonly Dictionary<Guid, ProfilIklim> _profilIklim = [];

    public ProyekRepositoryInMemory Add(Komponen komponen, ProfilIklim? profil = null)
    {
        _komponen[komponen.Id] = komponen;
        if (profil is not null)
        {
            _profilIklim[profil.ProyekId] = profil;
        }

        return this;
    }

    public Task<Komponen?> GetKomponenAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_komponen.GetValueOrDefault(id));

    public Task<IReadOnlyList<Komponen>> ListKomponenAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Komponen>>(
            _komponen.Values.Where(k => k.ProyekId == proyekId).ToList());

    public Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Proyek?>(null);

    public Task AddAsync(Proyek proyek, CancellationToken ct = default) => Task.CompletedTask;

    public Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult(_profilIklim.GetValueOrDefault(proyekId));

    public Task SimpanProfilIklimAsync(ProfilIklim profil, CancellationToken ct = default)
    {
        _profilIklim[profil.ProyekId] = profil;
        return Task.CompletedTask;
    }
}