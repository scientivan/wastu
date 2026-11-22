using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

public sealed class BatchEvaluasiRepositoryInMemory : IBatchEvaluasiRepository
{
    private readonly ConcurrentDictionary<Guid, BatchEvaluasi> _batches = new();
    private readonly IEvaluasiRepository? _evaluasiRepo;

    public BatchEvaluasiRepositoryInMemory(IEvaluasiRepository? evaluasiRepo = null)
    {
        _evaluasiRepo = evaluasiRepo;
    }

    public async Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(evaluasi);

        var existing = _batches.Values.FirstOrDefault(b =>
            b.Id == batch.Id || (b.KomponenId == batch.KomponenId && b.HashInput == batch.HashInput && b.VersiModel == batch.VersiModel));

        Guid batchId = existing?.Id ?? batch.Id;
        _batches[batchId] = batch with { Id = batchId };

        if (_evaluasiRepo is not null)
        {
            var evaluasiDenganBatch = evaluasi.Select(e => e with { BatchId = batchId }).ToList();
            await _evaluasiRepo.SimpanAsync(evaluasiDenganBatch, ct).ConfigureAwait(false);
        }
    }

    public Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_batches.GetValueOrDefault(id));

    public Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid komponenId, CancellationToken ct = default)
    {
        var batch = _batches.Values
            .Where(b => b.KomponenId == komponenId)
            .OrderByDescending(b => b.DihitungPadaUtc)
            .FirstOrDefault();

        return Task.FromResult(batch);
    }

    public Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        IReadOnlyList<BatchEvaluasi> hasil = _batches.Values
            .Where(b => b.ProyekId == proyekId)
            .OrderByDescending(b => b.DihitungPadaUtc)
            .ToList();

        return Task.FromResult(hasil);
    }
}
