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

        // Cari toleransi koordinat 0.001 derajat (~100 meter)
        var baris = await db.PrakiraanCache.AsNoTracking()
            .Where(x => Math.Abs(x.Latitude - lat) < 0.001 && Math.Abs(x.Longitude - lon) < 0.001 && x.RevisiLokasi == revisiLokasi)
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

        // Cari bangunan terdekat bila ada
        var bangunan = await db.Bangunan
            .FirstOrDefaultAsync(b => Math.Abs(b.Latitude - lat) < 0.001 && Math.Abs(b.Longitude - lon) < 0.001, ct)
            .ConfigureAwait(false);

        Guid? bangunanId = bangunan?.Id;
        int revisi = hasil.RevisiLokasi;

        foreach (var hari in hasil.Hari)
        {
            var existing = await db.PrakiraanCache
                .FirstOrDefaultAsync(x => Math.Abs(x.Latitude - lat) < 0.001
                    && Math.Abs(x.Longitude - lon) < 0.001
                    && x.RevisiLokasi == revisi
                    && x.TanggalCuaca == hari.Tanggal, ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                db.PrakiraanCache.Add(new PrakiraanCacheEntity
                {
                    Id = Guid.NewGuid(),
                    BangunanId = bangunanId,
                    Latitude = lat,
                    Longitude = lon,
                    RevisiLokasi = revisi,
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
                });
            }
            else
            {
                existing.BangunanId ??= bangunanId;
                existing.ZonaWaktu = hasil.ZonaWaktu;
                existing.DiambilPadaUtc = hasil.DiambilPadaUtc;
                existing.Penyedia = hasil.Penyedia;
                existing.DataParsial = hasil.DataParsial;
                existing.DataLengkap = hari.DataLengkap;
                existing.CatatanKualitas = hari.CatatanKualitas;
                existing.ProbabilitasHujanPct = hari.ProbabilitasHujanPct;
                existing.CurahHujanMm = hari.CurahHujanMm;
                existing.SuhuMinC = hari.SuhuMinC;
                existing.SuhuMaksC = hari.SuhuMaksC;
                existing.KelembapanMaksPct = hari.KelembapanPct;
            }
        }

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
