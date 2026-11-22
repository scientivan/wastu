using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

/// <summary>
/// Penyimpanan hasil penilaian dan simulasi di dalam memori proses.
/// Mendukung multi-horizon simulasi berversi (PRD §11.3, T20).
/// </summary>
public sealed class EvaluasiRepositoryInMemory : IEvaluasiRepository
{
    private readonly ConcurrentDictionary<Guid, Evaluasi> _evaluasi = new();
    private readonly ConcurrentDictionary<(Guid EvaluasiId, int HorizonTahun, string VersiModel), SimulasiEmisi> _simulasi = new();

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
        ArgumentNullException.ThrowIfNull(simulasi);
        string versi = simulasi.VersiModel ?? "v2.0";
        _simulasi[(simulasi.EvaluasiId, simulasi.HorizonTahun, versi)] = simulasi;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Hasil simulasi yang tersimpan untuk sebuah evaluasi dan horizon tahun.
    /// </summary>
    public SimulasiEmisi? AmbilSimulasi(Guid evaluasiId, int horizonTahun = 30, string versiModel = "v2.0")
        => _simulasi.GetValueOrDefault((evaluasiId, horizonTahun, versiModel))
           ?? _simulasi.Values.FirstOrDefault(s => s.EvaluasiId == evaluasiId && s.HorizonTahun == horizonTahun);

    /// <summary>
    /// Mengambil semua simulasi yang tersimpan untuk sebuah evaluasi.
    /// </summary>
    public IReadOnlyList<SimulasiEmisi> ListSimulasiByEvaluasi(Guid evaluasiId)
        => _simulasi.Values.Where(s => s.EvaluasiId == evaluasiId).OrderBy(s => s.HorizonTahun).ToList();
}