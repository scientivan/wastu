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

        var ids = baris.Select(b => b.Id).ToList();
        var sumberList = await db.SumberPropertiBahan.AsNoTracking()
            .Where(s => ids.Contains(s.BahanId))
            .OrderBy(s => s.Properti)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sumberByBahan = sumberList
            .GroupBy(s => s.BahanId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SumberPropertiBahan>)g.Select(Pemetaan.KeDomain).ToList());

        return baris.Select(b =>
        {
            sumberByBahan.TryGetValue(b.Id, out var sumber);
            return new Bahan(
                b.Id,
                b.Nama,
                b.Jenis,
                b.KonduktivitasTermal,
                b.KetahananLembap,
                b.KetahananKorosi,
                b.UsiaPakaiNominalTahun,
                b.FaktorEmisi,
                b.SumberData,
                b.SusunanKetebalan,
                b.VersiData,
                sumber ?? []);
        }).ToList();
    }

    public async Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        BahanEntity? baris = await db.Bahan.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            .ConfigureAwait(false);

        if (baris is null)
        {
            return null;
        }

        var sumberList = await db.SumberPropertiBahan.AsNoTracking()
            .Where(s => s.BahanId == id)
            .OrderBy(s => s.Properti)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new Bahan(
            baris.Id,
            baris.Nama,
            baris.Jenis,
            baris.KonduktivitasTermal,
            baris.KetahananLembap,
            baris.KetahananKorosi,
            baris.UsiaPakaiNominalTahun,
            baris.FaktorEmisi,
            baris.SumberData,
            baris.SusunanKetebalan,
            baris.VersiData,
            sumberList.Select(Pemetaan.KeDomain).ToList());
    }
}
