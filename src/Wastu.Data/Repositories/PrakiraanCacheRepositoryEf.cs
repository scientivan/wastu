using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Data.Repositories;

public sealed class PrakiraanCacheRepositoryEf : IPrakiraanCacheRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public PrakiraanCacheRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<HasilPrakiraan?> GetCacheAsync(double lat, double lon, int revisiLokasi = 1, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.PrakiraanCache.AsNoTracking()
            .Where(x => x.Latitude == lat && x.Longitude == lon && x.RevisiLokasi == revisiLokasi)
            .OrderBy(x => x.TanggalCuaca)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (baris.Count == 0)
        {
            return null;
        }

        var pertama = baris[0];
        var daftarHari = baris.Select(r => new DailyForecast(
            r.TanggalCuaca,
            r.ProbabilitasHujanPct,
            r.CurahHujanMm,
            r.SuhuMinC,
            r.SuhuMaksC,
            r.KelembapanMaksPct,
            r.DataLengkap,
            r.CatatanKualitas)).ToList();

        return new HasilPrakiraan(
            daftarHari,
            pertama.ZonaWaktu,
            pertama.DiambilPadaUtc,
            pertama.Penyedia,
            revisiLokasi,
            pertama.DataParsial);
    }

    public async Task SimpanCacheAsync(double lat, double lon, HasilPrakiraan hasil, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(hasil);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        // Serialize refreshes for the same exact location across application instances.
        string key = FormattableString.Invariant($"{lat:R}:{lon:R}:{hasil.RevisiLokasi}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct).ConfigureAwait(false);
        var scope = db.PrakiraanCache.Where(x => x.Latitude == lat && x.Longitude == lon
            && x.RevisiLokasi == hasil.RevisiLokasi);
        if (await scope.AnyAsync(x => x.DiambilPadaUtc > hasil.DiambilPadaUtc, ct).ConfigureAwait(false))
            return;

        Guid? projectId = await db.Bangunan
            .Where(b => b.Latitude == lat && b.Longitude == lon && b.RevisiLokasi == hasil.RevisiLokasi)
            .OrderBy(b => b.Id).Select(b => (Guid?)b.Id).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        await scope.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        db.PrakiraanCache.AddRange(hasil.Hari.Select(hari => new PrakiraanCacheEntity
        {
            Id = Guid.NewGuid(),
            BangunanId = projectId,
            Latitude = lat,
            Longitude = lon,
            RevisiLokasi = hasil.RevisiLokasi,
            TanggalCuaca = hari.Tanggal,
            ZonaWaktu = hasil.ZonaWaktu,
            DiambilPadaUtc = hasil.DiambilPadaUtc,
            Penyedia = hasil.Penyedia,
            DataParsial = hasil.DataParsial,
            DataLengkap = hari.DataLengkap,
            CatatanKualitas = hari.CatatanKualitas,
            ProbabilitasHujanPct = hari.ProbabilitasHujanPct,
            CurahHujanMm = hari.CurahHujanMm,
            SuhuMinC = hari.SuhuMinC,
            SuhuMaksC = hari.SuhuMaksC,
            KelembapanMaksPct = hari.KelembapanPct
        }));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task PruneExpiredAsync(DateTimeOffset sebelumUtc, CancellationToken ct = default)
    {
        await using var db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        // Pruning operasional cache HANYA menghapus baris prakiraan_cache lama (T40).
        // Keputusan jadwal historis (keputusan_jadwal) tetap utuh!
        await db.PrakiraanCache
            .Where(c => c.DiambilPadaUtc < sebelumUtc)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }
}
