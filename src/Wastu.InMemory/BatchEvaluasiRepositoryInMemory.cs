using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

public sealed class BatchEvaluasiRepositoryInMemory : IBatchEvaluasiRepository
{
    private readonly ConcurrentDictionary<Guid, BatchEvaluasi> _batches = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _writeGates = new();
    private readonly IEvaluasiRepository? _evaluasiRepo;

    public BatchEvaluasiRepositoryInMemory(IEvaluasiRepository? evaluasiRepo = null)
    {
        _evaluasiRepo = evaluasiRepo;
    }

    public async Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(evaluasi);
        ct.ThrowIfCancellationRequested();
        Evaluasi[] captured = evaluasi.ToArray();

        SemaphoreSlim gate = _writeGates.GetOrAdd(batch.KomponenId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var existing = _batches.Values.FirstOrDefault(b =>
                b.Id == batch.Id || (b.KomponenId == batch.KomponenId && b.HashInput == batch.HashInput && b.VersiModel == batch.VersiModel));

            // Identitas dan timestamp batch pertama tetap kanonikal, termasuk
            // ketika penulisan evaluasinya gagal dan perlu dilengkapi saat retry.
            BatchEvaluasi canonical = existing ?? batch;
            _batches.TryAdd(canonical.Id, canonical);

            if (_evaluasiRepo is not null)
            {
                var stored = await _evaluasiRepo.ListByKomponenAsync(canonical.KomponenId, ct).ConfigureAwait(false);
                var materialIds = stored.Where(e => e.BatchId == canonical.Id).Select(e => e.BahanId).ToHashSet();
                var missing = captured
                    .Where(e => !materialIds.Contains(e.BahanId))
                    .GroupBy(e => e.BahanId)
                    .Select(group => group.First() with
                    {
                        BatchId = canonical.Id,
                        HashInput = canonical.HashInput,
                        VersiModel = canonical.VersiModel,
                        DihitungPadaUtc = canonical.DihitungPadaUtc
                    })
                    .ToArray();

                if (missing.Length > 0)
                    await _evaluasiRepo.SimpanAsync(missing, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
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
