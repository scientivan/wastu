using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class SumberBahanRepositoryEf : ISumberBahanRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public SumberBahanRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<SumberPropertiBahan>> ListByBahanIdAsync(Guid bahanId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<SumberPropertiBahanEntity> baris = await db.SumberPropertiBahan.AsNoTracking()
            .Where(x => x.BahanId == bahanId)
            .OrderBy(x => x.Properti)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task SimpanAsync(Guid bahanId, IReadOnlyList<SumberPropertiBahan> sumber, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sumber);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var existing = await db.SumberPropertiBahan
            .Where(x => x.BahanId == bahanId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        db.SumberPropertiBahan.RemoveRange(existing);

        foreach (var item in sumber)
        {
            var entity = Pemetaan.KeEntity(item);
            entity.BahanId = bahanId;
            db.SumberPropertiBahan.Add(entity);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }
}
