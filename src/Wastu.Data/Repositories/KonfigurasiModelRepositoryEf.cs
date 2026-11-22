using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class KonfigurasiModelRepositoryEf : IKonfigurasiModelRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public KonfigurasiModelRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<KonfigurasiModel> GetActiveAsync(CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var aktif = await db.KonfigurasiModel.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Aktif, ct)
            .ConfigureAwait(false);

        if (aktif is not null)
        {
            return Pemetaan.KeDomain(aktif);
        }

        var v2 = await db.KonfigurasiModel.AsNoTracking()
            .FirstOrDefaultAsync(x => x.VersiModel == "v2.0", ct)
            .ConfigureAwait(false);

        return v2 is not null ? Pemetaan.KeDomain(v2) : KonfigurasiModel.Bawaan;
    }

    public async Task<KonfigurasiModel?> GetByVersiAsync(string versiModel, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versiModel);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.KonfigurasiModel.AsNoTracking()
            .FirstOrDefaultAsync(x => x.VersiModel == versiModel, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task SimpanAsync(KonfigurasiModel konfigurasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(konfigurasi);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var existing = await db.KonfigurasiModel
            .FirstOrDefaultAsync(x => x.VersiModel == konfigurasi.VersiModel, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.KonfigurasiModel.Add(Pemetaan.KeEntity(konfigurasi, aktif: true));
        }
        else
        {
            existing.BobotLembapHujanAlpha = konfigurasi.BobotLembapHujanAlpha;
            existing.AcuanCurahHujanMm = konfigurasi.AcuanCurahHujanMm;
            existing.AcuanJarakPesisirKm = konfigurasi.AcuanJarakPesisirKm;
            existing.PitaTermalBatasBawah = konfigurasi.PitaTermalBatasBawah;
            existing.PitaTermalBatasAtasSejuk = konfigurasi.PitaTermalBatasAtasSejuk;
            existing.PitaTermalBatasAtasPanas = konfigurasi.PitaTermalBatasAtasPanas;
            existing.SuhuSejukC = konfigurasi.SuhuSejukC;
            existing.SuhuPanasC = konfigurasi.SuhuPanasC;
            existing.FaktorPenuaanA = konfigurasi.FaktorPenuaanA;
            existing.AmbangTidakDisarankan = konfigurasi.AmbangTidakDisarankan;
            existing.AmbangCocok = konfigurasi.AmbangCocok;
            existing.Aktif = true;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }
}
