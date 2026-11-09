using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class MaterialRepositoryEf : IMaterialRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public MaterialRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<Bahan>> ListByKindAsync(
        JenisKomponen jenis, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<BahanEntity> baris = await db.Bahan.AsNoTracking()
            .Where(b => b.Jenis == jenis)
            .OrderBy(b => b.Nama)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        BahanEntity? baris = await db.Bahan.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }
}
