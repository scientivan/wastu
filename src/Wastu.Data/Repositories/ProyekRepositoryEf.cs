using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Data.Repositories;

public sealed class ProyekRepositoryEf : IProyekRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public ProyekRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<Proyek>> ListAsync(CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<ProyekEntity> baris = await db.Bangunan.AsNoTracking()
            .OrderBy(p => p.Nama)
            .ThenBy(p => p.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Proyek?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProyekEntity? baris = await db.Bangunan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task AddAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        db.Bangunan.Add(new ProyekEntity
        {
            Id = proyek.Id,
            Nama = proyek.Nama,
            Kota = proyek.Kota,
            Latitude = proyek.Latitude,
            Longitude = proyek.Longitude,
            DibuatPada = DateTimeOffset.UtcNow,
            RevisiLokasi = proyek.RevisiLokasi,
            ZonaWaktu = proyek.ZonaWaktu
        });

        if (komponen is not null)
        {
            db.Komponen.AddRange(komponen.Select(KomponenBaru));
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Proyek proyek, CancellationToken ct = default, IReadOnlyList<Komponen>? komponen = null)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        ProyekEntity baris = await KunciProyekAsync(db, proyek.Id, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyek.Id} tidak ditemukan.");

        bool koordinatBerubah = (baris.Latitude != proyek.Latitude || baris.Longitude != proyek.Longitude);

        if (koordinatBerubah)
        {
            var profil = await db.ProfilIklim.FindAsync([proyek.Id], ct).ConfigureAwait(false);
            if (profil is not null)
            {
                db.ProfilIklim.Remove(profil);
            }

            // Tandai pekerjaan yang terkunci menjadi PerluDitinjau tanpa menghapus TanggalDikunci (PRD §6.3, §12)
            var terkunci = await db.Pekerjaan
                .Where(p => p.BangunanId == proyek.Id && p.Status == StatusPekerjaan.Terkunci)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var pekerjaan in terkunci)
            {
                pekerjaan.Status = StatusPekerjaan.PerluDitinjau;
                pekerjaan.TerakhirDiperbaruiUtc = DateTimeOffset.UtcNow;
            }
        }

        baris.Nama = proyek.Nama;
        baris.Kota = proyek.Kota;
        baris.Latitude = proyek.Latitude;
        baris.Longitude = proyek.Longitude;
        baris.RevisiLokasi = proyek.RevisiLokasi;
        baris.ZonaWaktu = proyek.ZonaWaktu;

        if (komponen is not null)
        {
            var tersimpan = await db.Komponen.Where(k => k.BangunanId == proyek.Id)
                .ToDictionaryAsync(k => k.Id, ct).ConfigureAwait(false);

            var keepIds = komponen.Select(k => k.Id).ToHashSet();
            foreach (var lamaKomp in tersimpan.Values)
            {
                if (!keepIds.Contains(lamaKomp.Id))
                {
                    db.Komponen.Remove(lamaKomp);
                }
            }

            foreach (var item in komponen)
            {
                if (tersimpan.TryGetValue(item.Id, out var lama))
                {
                    lama.LuasM2 = item.LuasM2;
                }
                else
                {
                    db.Komponen.Add(KomponenBaru(item));
                }
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Bangunan.Where(p => p.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
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

        db.Komponen.Add(KomponenBaru(komponen));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static KomponenEntity KomponenBaru(Komponen komponen) => new()
    {
        Id = komponen.Id,
        BangunanId = komponen.ProyekId,
        Jenis = komponen.Jenis,
        LuasM2 = komponen.LuasM2
    };

    public async Task<ProfilIklim?> GetProfilIklimAsync(Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        ProfilIklimEntity? baris = await db.ProfilIklim.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BangunanId == proyekId, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
    }

    public async Task SimpanProfilIklimAsync(
        Guid proyekId, ProfilIklim profil, CancellationToken ct = default, Proyek? lokasi = null)
    {
        ArgumentNullException.ThrowIfNull(profil);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var proyek = await KunciProyekAsync(db, proyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");

        if (lokasi is not null && (proyek.Latitude != lokasi.Latitude || proyek.Longitude != lokasi.Longitude))
        {
            throw new InvalidOperationException("Lokasi proyek berubah saat data iklim diambil.");
        }

        ProfilIklimEntity baris = await db.ProfilIklim
            .FirstOrDefaultAsync(x => x.BangunanId == proyekId, ct)
            .ConfigureAwait(false)
            ?? TambahProfil(db, proyekId);

        Pemetaan.SalinKe(profil, baris);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    private static Task<ProyekEntity?> KunciProyekAsync(WastuDbContext db, Guid id, CancellationToken ct) =>
        db.Bangunan.FromSqlInterpolated($"SELECT * FROM bangunan WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);

    private static ProfilIklimEntity TambahProfil(WastuDbContext db, Guid proyekId)
    {
        var baris = new ProfilIklimEntity { BangunanId = proyekId };
        db.ProfilIklim.Add(baris);
        return baris;
    }
}
