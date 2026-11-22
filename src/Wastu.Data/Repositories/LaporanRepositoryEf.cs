using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Data.Repositories;

public sealed class LaporanRepositoryEf : ILaporanRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public LaporanRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<Laporan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<LaporanEntity> baris = await db.Laporan.AsNoTracking()
            .Where(x => x.BangunanId == proyekId)
            .OrderByDescending(x => x.DibuatPadaUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        LaporanEntity? baris = await db.Laporan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task SimpanAsync(Laporan laporan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(laporan);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var existing = await db.Laporan
            .FirstOrDefaultAsync(x => x.Id == laporan.Id, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.Laporan.Add(Pemetaan.KeEntity(laporan));
        }
        else
        {
            existing.PathBerkas = laporan.PathBerkas;
            existing.DibuatPadaUtc = laporan.DibuatPada;
            existing.ChecksumSha256 = laporan.ChecksumSha256;
            existing.HashSnapshot = laporan.SnapshotHash;
            existing.UkuranByte = laporan.UkuranByte;
            existing.StatusBerkas = laporan.Status;
            existing.OpsiJson = laporan.SnapshotJson ?? existing.OpsiJson;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Laporan.Where(x => x.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
