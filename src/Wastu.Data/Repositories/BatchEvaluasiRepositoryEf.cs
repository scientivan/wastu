using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class BatchEvaluasiRepositoryEf : IBatchEvaluasiRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public BatchEvaluasiRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(evaluasi);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var existingBatch = await db.BatchEvaluasi
            .FirstOrDefaultAsync(b => b.Id == batch.Id
                || (b.KomponenId == batch.KomponenId && b.HashInput == batch.HashInput && b.VersiModel == batch.VersiModel), ct)
            .ConfigureAwait(false);

        Guid batchId = batch.Id;

        if (existingBatch is null)
        {
            db.BatchEvaluasi.Add(Pemetaan.KeEntity(batch));
        }
        else
        {
            batchId = existingBatch.Id;
            existingBatch.BobotTermal = batch.Bobot.Termal;
            existingBatch.BobotLembap = batch.Bobot.Lembap;
            existingBatch.BobotKorosi = batch.Bobot.Korosi;
            existingBatch.BobotUsia = batch.Bobot.Usia;
            existingBatch.LuasM2 = batch.LuasM2;
            existingBatch.RevisiLokasi = batch.RevisiLokasi;
            existingBatch.DihitungPadaUtc = batch.DihitungPadaUtc;
        }

        foreach (var item in evaluasi)
        {
            var evaluasiDenganBatch = item with { BatchId = batchId };
            var existingEvaluasi = await db.Evaluasi
                .FirstOrDefaultAsync(e => e.Id == item.Id, ct)
                .ConfigureAwait(false);

            if (existingEvaluasi is null)
            {
                db.Evaluasi.Add(Pemetaan.KeEntity(evaluasiDenganBatch));
            }
            else
            {
                Pemetaan.SalinKe(evaluasiDenganBatch, existingEvaluasi);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.BatchEvaluasi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid komponenId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.BatchEvaluasi.AsNoTracking()
            .Where(x => x.KomponenId == komponenId)
            .OrderByDescending(x => x.DihitungPadaUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.BatchEvaluasi.AsNoTracking()
            .Where(x => x.ProyekId == proyekId)
            .OrderByDescending(x => x.DihitungPadaUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }
}
