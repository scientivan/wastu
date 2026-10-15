using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

/// <summary>
/// Penyimpanan hasil penilaian dan simulasi di dalam memori proses.
/// </summary>
/// <remarks>
/// Lihat <see cref="MaterialRepositoryInMemory"/> untuk penjelasan kenapa adapter
/// in-memory ada dan apa batasannya.
/// </remarks>
public sealed class EvaluasiRepositoryInMemory : IEvaluasiRepository
{
    private readonly ConcurrentDictionary<Guid, Evaluasi> _evaluasi = new();
    private readonly ConcurrentDictionary<Guid, SimulasiEmisi> _simulasi = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid komponenId, CancellationToken ct = default)
    {
        IReadOnlyList<Evaluasi> hasil = _evaluasi.Values
            .Where(e => e.KomponenId == komponenId)
            .OrderBy(e => e.BahanId)
            .ToList();

        return Task.FromResult(hasil);
    }

    /// <inheritdoc />
    public Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_evaluasi.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);

        foreach (Evaluasi item in evaluasi)
        {
            _evaluasi[item.Id] = item;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default)
    {
        _simulasi[simulasi.EvaluasiId] = simulasi;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Hasil simulasi yang tersimpan untuk sebuah evaluasi.
    /// </summary>
    /// <remarks>
    /// Bukan bagian dari port karena tidak ada alur produksi yang membacanya; ini
    /// hanya supaya pengujian bisa memeriksa bahwa service benar-benar menyimpan.
    /// </remarks>
    public SimulasiEmisi? AmbilSimulasi(Guid evaluasiId) => _simulasi.GetValueOrDefault(evaluasiId);
}