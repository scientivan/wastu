using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class EvaluasiRepositoryEf : IEvaluasiRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public EvaluasiRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(
        Guid komponenId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<EvaluasiEntity> baris = await db.Evaluasi.AsNoTracking()
            .Where(x => x.KomponenId == komponenId)
            .OrderBy(x => x.BahanId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        EvaluasiEntity? baris = await db.Evaluasi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        foreach (Evaluasi item in evaluasi)
        {
            EvaluasiEntity? baris = await db.Evaluasi
                .FirstOrDefaultAsync(x => x.Id == item.Id, ct)
                .ConfigureAwait(false);

            if (baris is null)
            {
                baris = Pemetaan.KeEntity(item);
                baris.DihitungPada = DateTimeOffset.UtcNow;
                db.Evaluasi.Add(baris);
            }
            else
            {
                Pemetaan.SalinKe(item, baris);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(simulasi);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        SimulasiEmisiEntity? baris = await db.SimulasiEmisi
            .FirstOrDefaultAsync(x => x.EvaluasiId == simulasi.EvaluasiId, ct)
            .ConfigureAwait(false);

        if (baris is null)
        {
            db.SimulasiEmisi.Add(Pemetaan.KeEntity(simulasi));
        }
        else
        {
            baris.HorizonTahun = simulasi.HorizonTahun;
            baris.UsiaPakaiEfektifTahun = simulasi.UsiaPakaiEfektifTahun;
            baris.JumlahPenggantian = simulasi.JumlahPenggantian;
            baris.EmisiKumulatifKg = simulasi.EmisiKumulatifKg;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
