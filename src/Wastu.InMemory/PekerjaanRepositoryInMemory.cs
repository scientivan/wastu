using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.InMemory;

public sealed class PekerjaanRepositoryInMemory : IPekerjaanRepository
{
    private readonly ConcurrentDictionary<Guid, Pekerjaan> _pekerjaan = new();
    private readonly List<KeputusanJadwal> _keputusan = new();
    private readonly Dictionary<Guid, string> _keputusanTerakhir = new();

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

        CatatKeputusan(pekerjaan);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Pekerjaan pekerjaan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);
        _pekerjaan[pekerjaan.Id] = pekerjaan;

        CatatKeputusan(pekerjaan);
        return Task.CompletedTask;
    }

    private void CatatKeputusan(Pekerjaan pekerjaan)
    {
        var decision = pekerjaan.KeputusanTerakhir;
        if (decision is null && pekerjaan.TanggalDikunci is { } date)
            decision = new KeputusanJadwal(date, StatusKelayakanCuaca.DataTidakTersedia,
                ["Keputusan legacy tanpa snapshot cuaca; kelayakan belum terverifikasi."], Terkunci: true,
                DikunciPadaUtc: pekerjaan.TerakhirDiperbaruiUtc, Konfirmasi: pekerjaan.Konfirmasi);
        if (decision is not null)
        {
            lock (_keputusan)
            {
                string fingerprint = System.Text.Json.JsonSerializer.Serialize(decision);
                if (_keputusanTerakhir.GetValueOrDefault(pekerjaan.Id) == fingerprint) return;
                _keputusanTerakhir[pekerjaan.Id] = fingerprint;
                _keputusan.Add(decision with { Catatan = decision.Catatan.ToArray() });
            }
        }
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
