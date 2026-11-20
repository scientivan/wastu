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

    public Task<IReadOnlyList<Proyek>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Proyek>>(_proyek.Values.OrderBy(p => p.Nama).ThenBy(p => p.Id).ToArray());

    /// <inheritdoc />
    public Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_proyek.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task AddAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null)
    {
        _proyek[proyek.Id] = proyek;
        foreach (var item in komponen ?? [])
            _komponen[item.Id] = item;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null)
    {
        lock (_proyek)
        {
            if (_proyek.TryGetValue(proyek.Id, out var lama) &&
                (lama.Latitude != proyek.Latitude || lama.Longitude != proyek.Longitude))
                _profilIklim.TryRemove(proyek.Id, out _);
            return AddAsync(proyek, ct, komponen);
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        lock (_proyek)
        {
            _proyek.TryRemove(id, out _);
            _profilIklim.TryRemove(id, out _);
            foreach (var item in _komponen.Values.Where(item => item.ProyekId == id))
                _komponen.TryRemove(item.Id, out _);
        }
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
    public Task SimpanProfilIklimAsync(Guid proyekId, ProfilIklim profil, CancellationToken ct = default, Proyek? lokasi = null)
    {
        lock (_proyek)
        {
            if (lokasi is not null &&
                (!_proyek.TryGetValue(proyekId, out var proyek) || proyek.Latitude != lokasi.Latitude ||
                    proyek.Longitude != lokasi.Longitude))
                throw new InvalidOperationException("Lokasi proyek berubah saat data iklim diambil.");

            _profilIklim[proyekId] = profil;
        }
        return Task.CompletedTask;
    }
}
