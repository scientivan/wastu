using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.InMemory;

public sealed class PekerjaanRepositoryInMemory : IPekerjaanRepository
{
    private readonly ConcurrentDictionary<Guid, Pekerjaan> _pekerjaan = new();
    private readonly List<KeputusanJadwal> _keputusan = new();

    public Task<IReadOnlyList<Pekerjaan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        IReadOnlyList<Pekerjaan> hasil = _pekerjaan.Values
            .Where(p => p.ProyekId == proyekId)
            .OrderBy(p => p.TanggalMulai)
            .ThenBy(p => p.Nama)
            .ToList();

        return Task.FromResult(hasil);
    }

    public Task<Pekerjaan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_pekerjaan.GetValueOrDefault(id));

    public Task AddAsync(Pekerjaan pekerjaan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);
        _pekerjaan[pekerjaan.Id] = pekerjaan;

        if (pekerjaan.Status == StatusPekerjaan.Terkunci && pekerjaan.TanggalDikunci.HasValue)
        {
            lock (_keputusan)
            {
                _keputusan.Add(new KeputusanJadwal(
                    pekerjaan.TanggalDikunci.Value,
                    StatusKelayakanCuaca.Layak,
                    ["Jadwal dikunci oleh pengguna."],
                    Terkunci: true,
                    DikunciPadaUtc: pekerjaan.TerakhirDiperbaruiUtc,
                    Konfirmasi: pekerjaan.Konfirmasi));
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Pekerjaan pekerjaan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);
        _pekerjaan[pekerjaan.Id] = pekerjaan;

        if (pekerjaan.Status == StatusPekerjaan.Terkunci && pekerjaan.TanggalDikunci.HasValue)
        {
            lock (_keputusan)
            {
                _keputusan.Add(new KeputusanJadwal(
                    pekerjaan.TanggalDikunci.Value,
                    StatusKelayakanCuaca.Layak,
                    ["Jadwal dikunci oleh pengguna."],
                    Terkunci: true,
                    DikunciPadaUtc: pekerjaan.TerakhirDiperbaruiUtc,
                    Konfirmasi: pekerjaan.Konfirmasi));
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _pekerjaan.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public IReadOnlyList<KeputusanJadwal> AmbilKeputusanHistoris()
    {
        lock (_keputusan)
        {
            return [.. _keputusan];
        }
    }
}
