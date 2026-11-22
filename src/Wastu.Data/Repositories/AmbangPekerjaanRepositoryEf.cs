using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Data.Repositories;

public sealed class AmbangPekerjaanRepositoryEf : IAmbangPekerjaanRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public AmbangPekerjaanRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<AmbangPekerjaan?> GetByPekerjaanIdAsync(Guid pekerjaanId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var spesifik = await db.AmbangPekerjaan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PekerjaanId == pekerjaanId, ct)
            .ConfigureAwait(false);

        if (spesifik is not null)
        {
            return Pemetaan.KeDomain(spesifik);
        }

        var pekerjaan = await db.Pekerjaan.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pekerjaanId, ct)
            .ConfigureAwait(false);

        if (pekerjaan is not null)
        {
            if (!string.IsNullOrWhiteSpace(pekerjaan.OverrideAmbangJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<AmbangPekerjaan>(pekerjaan.OverrideAmbangJson);
                }
                catch
                {
                    // abaikan jika format tidak cocok
                }
            }

            return await GetByJenisAsync(pekerjaan.Jenis, ct).ConfigureAwait(false);
        }

        return null;
    }

    public async Task<AmbangPekerjaan?> GetByJenisAsync(JenisPekerjaan jenis, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.AmbangPekerjaan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PekerjaanId == null && x.JenisPekerjaan == jenis, ct)
            .ConfigureAwait(false);

        if (baris is not null)
        {
            return Pemetaan.KeDomain(baris);
        }

        // Default asumsi generik bawaan domain (PRD §12, PRD-R05)
        return AmbangLayakKerja.Untuk(jenis);
    }

    public async Task SimpanOverrideAsync(Guid pekerjaanId, AmbangPekerjaan ambang, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ambang);
        ambang.PastikanValid();

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var existing = await db.AmbangPekerjaan
            .FirstOrDefaultAsync(x => x.PekerjaanId == pekerjaanId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.AmbangPekerjaan.Add(new AmbangPekerjaanEntity
            {
                Id = Guid.NewGuid(),
                PekerjaanId = pekerjaanId,
                ProbabilitasHujanMaksPct = ambang.ProbabilitasHujanMaksPct,
                CurahHujanMaksMm = ambang.CurahHujanMaksMm,
                KelembapanMaksPct = ambang.KelembapanMaksPct,
                SuhuMinC = ambang.SuhuMinC,
                SuhuMaksC = ambang.SuhuMaksC,
                TingkatSuhu = ambang.TingkatSuhu,
                SumberAmbang = "Override Pekerjaan"
            });
        }
        else
        {
            existing.ProbabilitasHujanMaksPct = ambang.ProbabilitasHujanMaksPct;
            existing.CurahHujanMaksMm = ambang.CurahHujanMaksMm;
            existing.KelembapanMaksPct = ambang.KelembapanMaksPct;
            existing.SuhuMinC = ambang.SuhuMinC;
            existing.SuhuMaksC = ambang.SuhuMaksC;
            existing.TingkatSuhu = ambang.TingkatSuhu;
            existing.SumberAmbang = "Override Pekerjaan";
        }

        var pekerjaan = await db.Pekerjaan
            .FirstOrDefaultAsync(p => p.Id == pekerjaanId, ct)
            .ConfigureAwait(false);

        if (pekerjaan is not null)
        {
            pekerjaan.OverrideAmbangJson = JsonSerializer.Serialize(ambang);
            pekerjaan.TerakhirDiperbaruiUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }
}
