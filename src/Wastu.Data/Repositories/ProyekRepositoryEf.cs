using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Repositories;

public sealed class ProyekRepositoryEf : IProyekRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public ProyekRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProyekEntity? baris = await db.Bangunan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task AddAsync(Proyek proyek, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        db.Bangunan.Add(new ProyekEntity
        {
            Id = proyek.Id,
            Nama = proyek.Nama,
            Kota = proyek.Kota,
            Latitude = proyek.Latitude,
            Longitude = proyek.Longitude,
            DibuatPada = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Proyek proyek, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProyekEntity baris = await db.Bangunan
            .FirstOrDefaultAsync(x => x.Id == proyek.Id, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyek.Id} tidak ditemukan.");

        baris.Nama = proyek.Nama;
        baris.Kota = proyek.Kota;
        baris.Latitude = proyek.Latitude;
        baris.Longitude = proyek.Longitude;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Komponen>> ListKomponenAsync(
        Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<KomponenEntity> baris = await db.Komponen.AsNoTracking()
            .Where(x => x.BangunanId == proyekId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Komponen?> GetKomponenAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        KomponenEntity? baris = await db.Komponen.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task AddKomponenAsync(Komponen komponen, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        db.Komponen.Add(new KomponenEntity
        {
            Id = komponen.Id,
            BangunanId = komponen.ProyekId,
            Jenis = komponen.Jenis,
            LuasM2 = komponen.LuasM2
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProfilIklimEntity? baris = await db.ProfilIklim.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BangunanId == proyekId, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task SimpanProfilIklimAsync(
        Guid proyekId, ProfilIklim profil, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profil);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProfilIklimEntity baris = await db.ProfilIklim
            .FirstOrDefaultAsync(x => x.BangunanId == proyekId, ct)
            .ConfigureAwait(false)
            ?? TambahProfil(db, proyekId);

        baris.CurahHujanTahunanMm = profil.CurahHujanTahunanMm;
        baris.KelembapanRataPct = profil.KelembapanRataPct;
        baris.SuhuRataC = profil.SuhuRataC;
        baris.JarakPesisirKm = profil.JarakPesisirKm;
        baris.TingkatKorosi = profil.TingkatKorosi;
        baris.DiambilPada = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static ProfilIklimEntity TambahProfil(WastuDbContext db, Guid proyekId)
    {
        var baris = new ProfilIklimEntity { BangunanId = proyekId };
        db.ProfilIklim.Add(baris);
        return baris;
    }
}
