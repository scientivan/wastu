using Wastu.Domain.Abstractions;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Scoring;

public sealed class ScoringSnapshotPersistenceTests
{
    [Fact]
    public async Task ConcurrentRanks_StoreOneBatchAndReuseEvaluationIds()
    {
        var repository = new ControlledBatchRepository { HoldWrites = true };
        var service = CreateService(repository);
        var first = service.RankAsync(DataContoh.KomponenDindingId);
        await repository.WriteStarted.Task;

        var second = service.RankAsync(DataContoh.KomponenDindingId);
        try
        {
            Assert.Equal(1, repository.WriteCount);
        }
        finally
        {
            repository.ReleaseWrites.TrySetResult();
        }

        var results = await Task.WhenAll(first, second);
        Assert.Equal(1, repository.WriteCount);
        Assert.Equal(results[0].Select(e => e.Id), results[1].Select(e => e.Id));
        Assert.Equal(results[0].Select(e => e.BatchId), results[1].Select(e => e.BatchId));
    }

    [Fact]
    public async Task Snapshot_UsesCapturedInputsWithoutReloadingRepositories()
    {
        var repository = new ControlledBatchRepository();
        var service = new SuitabilityScoringService(new MaterialRepositoryInMemory([]),
            new ProyekRepositoryInMemory(), repository, repository);
        var component = DataContoh.Dinding() with { LuasM2 = 240 };
        var climate = DataContoh.IklimDinginInland();
        Bahan[] materials = [DataContoh.BetonRingan()];
        var weights = KriteriaBobot.DariPersentase(70, 10, 10, 10);
        var expected = SuitabilityScoringService.Rank(component, climate, materials, weights);

        var actual = await service.RankSnapshotAsync(component, climate, materials, weights);

        Assert.Equal(expected.Select(e => e.SkorKesesuaian), actual.Select(e => e.SkorKesesuaian));
        Assert.Equal(expected.Select(e => e.EmisiAwalKg), actual.Select(e => e.EmisiAwalKg));
        Assert.Equal(SuitabilityScoringService.HitungHash(component, climate, materials, weights),
            Assert.Single(repository.StoredBatches).HashInput);
        Assert.Equal(240, Assert.Single(repository.StoredBatches).LuasM2);
    }

    [Fact]
    public async Task WaitingSnapshot_CopiesCatalogBeforeCallerChangesItsList()
    {
        var repository = new ControlledBatchRepository { HoldWrites = true };
        var service = CreateService(repository);
        var first = service.RankAsync(DataContoh.KomponenDindingId);
        await repository.WriteStarted.Task;
        var sources = new List<SumberPropertiBahan> { new(Guid.NewGuid(), "faktor_emisi", "Referensi asli") };
        var captured = DataContoh.PanelGipsum() with { SumberProperti = sources };
        var materials = new List<Bahan> { captured };
        var second = service.RankSnapshotAsync(DataContoh.Dinding(), DataContoh.IklimDinginInland(), materials);
        materials.Clear();
        materials.Add(DataContoh.BetonBertulang());
        sources.Clear();
        sources.Add(new(captured.Id, "faktor_emisi", "Referensi yang diubah saat antrean"));
        repository.ReleaseWrites.TrySetResult();

        await first;
        var stored = Assert.Single(await second);
        Assert.Equal(captured.Id, stored.BahanId);
        Assert.Equal("Referensi asli", Assert.Single(stored.Input!.Bahan.SumberProperti!).Referensi);
    }

    [Fact]
    public async Task FailedWrite_AllowsRetryWithSameInputs()
    {
        var repository = new ControlledBatchRepository { FailFirstWrite = true };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<IOException>(() => service.RankAsync(DataContoh.KomponenDindingId));
        var actual = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(2, repository.WriteCount);
        Assert.Single(repository.StoredBatches);
        Assert.All(actual, e => Assert.Equal(repository.StoredBatches[0].Id, e.BatchId));
    }

    [Fact]
    public async Task CancelledWait_DoesNotCancelOtherRankOrPreventLaterReuse()
    {
        var repository = new ControlledBatchRepository { HoldWrites = true };
        var service = CreateService(repository);
        var first = service.RankAsync(DataContoh.KomponenDindingId);
        await repository.WriteStarted.Task;
        using var cancellation = new CancellationTokenSource();
        var cancelled = service.RankAsync(DataContoh.KomponenDindingId, ct: cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        repository.ReleaseWrites.TrySetResult();
        var original = await first;
        var retry = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(1, repository.WriteCount);
        Assert.Equal(original.Select(e => e.Id), retry.Select(e => e.Id));
    }

    [Fact]
    public async Task CancelledWrite_ReleasesGateAndAllowsRetry()
    {
        var repository = new ControlledBatchRepository { HoldWrites = true };
        var service = CreateService(repository);
        using var cancellation = new CancellationTokenSource();
        var cancelled = service.RankAsync(DataContoh.KomponenDindingId, ct: cancellation.Token);
        await repository.WriteStarted.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        repository.ReleaseWrites.TrySetResult();

        var retry = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(2, repository.WriteCount);
        Assert.Single(repository.StoredBatches);
        Assert.All(retry, e => Assert.Equal(repository.StoredBatches[0].Id, e.BatchId));
    }

    [Fact]
    public async Task RepositoryResolvesDuplicate_ReturnsCanonicalPersistedIds()
    {
        var repository = new ControlledBatchRepository { SubstituteStoredIds = true };
        var service = CreateService(repository);

        var actual = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(repository.StoredEvaluations.Select(e => e.Id).Order(), actual.Select(e => e.Id).Order());
        Assert.All(actual, e => Assert.Equal(Assert.Single(repository.StoredBatches).Id, e.BatchId));
    }

    [Fact]
    public async Task ConcurrentWriterWinsBeforeFailedSave_ReusesCommittedCanonicalResults()
    {
        var repository = new ControlledBatchRepository { ThrowAfterCommit = true };
        var service = CreateService(repository);

        var actual = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(repository.StoredEvaluations.Select(e => e.Id).Order(), actual.Select(e => e.Id).Order());
        Assert.Single(repository.StoredBatches);
    }

    private static SuitabilityScoringService CreateService(ControlledBatchRepository batches)
    {
        var projects = new ProyekRepositoryInMemory();
        projects.AddKomponenAsync(DataContoh.Dinding()).GetAwaiter().GetResult();
        projects.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung()).GetAwaiter().GetResult();
        return new SuitabilityScoringService(
            new MaterialRepositoryInMemory([DataContoh.BetonBertulang(), DataContoh.BetonRingan()]),
            projects, batches, batches);
    }

    private sealed class ControlledBatchRepository : IBatchEvaluasiRepository, IEvaluasiRepository
    {
        private readonly BatchEvaluasiRepositoryPerekam _inner = new();
        public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseWrites { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldWrites { get; init; }
        public bool FailFirstWrite { get; init; }
        public bool SubstituteStoredIds { get; init; }
        public bool ThrowAfterCommit { get; init; }
        public int WriteCount { get; private set; }
        public IReadOnlyList<BatchEvaluasi> StoredBatches => _inner.Batch;
        public IReadOnlyList<Evaluasi> StoredEvaluations => _inner.Evaluasi;

        public async Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
        {
            WriteCount++;
            WriteStarted.TrySetResult();
            if (HoldWrites) await ReleaseWrites.Task.WaitAsync(ct);
            if (FailFirstWrite && WriteCount == 1) throw new IOException("Kegagalan simpan uji.");
            if (SubstituteStoredIds)
            {
                batch = batch with { Id = Guid.NewGuid() };
                evaluasi = evaluasi.Select(e => e with { Id = Guid.NewGuid(), BatchId = batch.Id }).ToArray();
            }
            await _inner.SimpanBatchAsync(batch, evaluasi, ct);
            if (ThrowAfterCommit) throw new IOException("Transaksi bersamaan sudah tersimpan.");
        }

        public Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default) => _inner.GetBatchAsync(id, ct);
        public Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid id, CancellationToken ct = default) => _inner.GetLatestBatchByKomponenAsync(id, ct);
        public Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid id, CancellationToken ct = default) => _inner.ListBatchesByProyekAsync(id, ct);
        public Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid id, CancellationToken ct = default) => _inner.ListByKomponenAsync(id, ct);
        public Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default) => _inner.GetByIdAsync(id, ct);
        public Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default) => _inner.SimpanAsync(evaluasi, ct);
        public Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default) => _inner.SimpanAsync(simulasi, ct);
    }
}
