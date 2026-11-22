using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Tests.Scoring;

/// <summary>
/// Perekam batch dan evaluasi untuk menguji deduplikasi render ulang. Satu instance
/// dipakai sebagai <see cref="IBatchEvaluasiRepository"/> sekaligus
/// <see cref="IEvaluasiRepository"/>, meniru tabel yang sama di adapter nyata.
/// </summary>
internal sealed class BatchEvaluasiRepositoryPerekam : IBatchEvaluasiRepository, IEvaluasiRepository
{
    private readonly List<BatchEvaluasi> _batch = [];
    private readonly Dictionary<Guid, Evaluasi> _evaluasi = [];
    private readonly Dictionary<Guid, SimulasiEmisi> _simulasi = [];

    public int JumlahSimpanBatch { get; private set; }

    public IReadOnlyList<BatchEvaluasi> Batch => _batch;

    public IReadOnlyList<Evaluasi> Evaluasi => _evaluasi.Values.ToArray();

    public Task SimpanBatchAsync(
        BatchEvaluasi batch,
        IReadOnlyList<Evaluasi> evaluasi,
        CancellationToken ct = default)
    {
        JumlahSimpanBatch++;
        _batch.Add(batch);

        foreach (Evaluasi item in evaluasi)
        {
            _evaluasi[item.Id] = item;
        }

        return Task.CompletedTask;
    }

    public Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_batch.FirstOrDefault(item => item.Id == id));

    public Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid komponenId, CancellationToken ct = default)
        => Task.FromResult(_batch.LastOrDefault(item => item.KomponenId == komponenId));

    public Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BatchEvaluasi>>(
            _batch.Where(item => item.ProyekId == proyekId).ToArray());

    public Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid komponenId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Evaluasi>>(
            _evaluasi.Values.Where(item => item.KomponenId == komponenId).ToArray());

    public Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_evaluasi.GetValueOrDefault(id));

    public Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        foreach (Evaluasi item in evaluasi)
        {
            _evaluasi[item.Id] = item;
        }

        return Task.CompletedTask;
    }

    public Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default)
    {
        _simulasi[simulasi.EvaluasiId] = simulasi;
        return Task.CompletedTask;
    }
}
