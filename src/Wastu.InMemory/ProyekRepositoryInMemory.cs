using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

/// <summary>
/// Sumber proyek, komponen, dan profil iklim di dalam memori proses.
/// </summary>
/// <remarks>
/// Lihat <see cref="MaterialRepositoryInMemory"/> untuk penjelasan kenapa adapter
/// in-memory ada dan apa batasannya.
/// </remarks>
public sealed class ProyekRepositoryInMemory : IProyekRepository
{
    private readonly ConcurrentDictionary<Guid, Proyek> _proyek = new();
    private readonly ConcurrentDictionary<Guid, Komponen> _komponen = new();
    private readonly ConcurrentDictionary<Guid, ProfilIklim> _profilIklim = new();

    /// <inheritdoc />
    public Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_proyek.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task AddAsync(Proyek proyek, CancellationToken ct = default)
    {
        _proyek[proyek.Id] = proyek;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Proyek proyek, CancellationToken ct = default)
    {
        _proyek[proyek.Id] = proyek;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Komponen>> ListKomponenAsync(Guid proyekId, CancellationToken ct = default)
    {
        IReadOnlyList<Komponen> hasil = _komponen.Values
            .Where(k => k.ProyekId == proyekId)
            .OrderBy(k => k.Id)
            .ToList();

        return Task.FromResult(hasil);
    }

    /// <inheritdoc />
    public Task<Komponen?> GetKomponenAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_komponen.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task AddKomponenAsync(Komponen komponen, CancellationToken ct = default)
    {
        _komponen[komponen.Id] = komponen;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult(_profilIklim.GetValueOrDefault(proyekId));

    /// <inheritdoc />
    public Task SimpanProfilIklimAsync(Guid proyekId, ProfilIklim profil, CancellationToken ct = default)
    {
        _profilIklim[proyekId] = profil;
        return Task.CompletedTask;
    }
}