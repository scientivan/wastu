using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.InMemory;

public sealed class LaporanRepositoryInMemory : ILaporanRepository
{
    private readonly ConcurrentDictionary<Guid, Laporan> _laporan = new();

    public Task<IReadOnlyList<Laporan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        IReadOnlyList<Laporan> hasil = _laporan.Values
            .Where(l => l.ProyekId == proyekId)
            .OrderByDescending(l => l.DibuatPada)
            .ToList();

        return Task.FromResult(hasil);
    }

    public Task<Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_laporan.GetValueOrDefault(id));

    public Task SimpanAsync(Laporan laporan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(laporan);
        _laporan[laporan.Id] = laporan;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _laporan.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
