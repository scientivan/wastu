using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Data.Repositories;

public sealed class PekerjaanRepositoryEf : IPekerjaanRepository
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public PekerjaanRepositoryEf(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<IReadOnlyList<Pekerjaan>> ListByProyekAsync(Guid proyekId, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.Pekerjaan.AsNoTracking()
            .Where(x => x.BangunanId == proyekId)
            .OrderBy(x => x.TanggalMulai)
            .ThenBy(x => x.Nama)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = baris.Select(x => x.Id).ToArray();
        var keputusan = await db.KeputusanJadwal.AsNoTracking().Where(x => ids.Contains(x.PekerjaanId))
            .OrderByDescending(x => x.DibuatPadaUtc).ToListAsync(ct).ConfigureAwait(false);
        return baris.Select(x => DenganKeputusan(Pemetaan.KeDomain(x),
            keputusan.FirstOrDefault(k => k.PekerjaanId == x.Id))).ToList();
    }

    public async Task<Pekerjaan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.Pekerjaan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        if (baris is null) return null;
        var keputusan = await db.KeputusanJadwal.AsNoTracking().Where(x => x.PekerjaanId == id)
            .OrderByDescending(x => x.DibuatPadaUtc).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return DenganKeputusan(Pemetaan.KeDomain(baris), keputusan);
    }

    public async Task AddAsync(Pekerjaan pekerjaan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var entity = Pemetaan.KeEntity(pekerjaan);
        db.Pekerjaan.Add(entity);

        if (pekerjaan.Status == StatusPekerjaan.Terkunci && pekerjaan.TanggalDikunci.HasValue)
        {
            db.KeputusanJadwal.Add(BentukKeputusan(pekerjaan));
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Pekerjaan pekerjaan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);

        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaksi = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var baris = await db.Pekerjaan
            .FirstOrDefaultAsync(x => x.Id == pekerjaan.Id, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Pekerjaan {pekerjaan.Id} tidak ditemukan.");

        var terakhir = await db.KeputusanJadwal.AsNoTracking().Where(x => x.PekerjaanId == pekerjaan.Id)
            .OrderByDescending(x => x.DibuatPadaUtc).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        bool keputusanBaru = pekerjaan.KeputusanTerakhir is not null
            && (terakhir is null || terakhir.SnapshotCuacaJson != JsonSerializer.Serialize(pekerjaan.KeputusanTerakhir));

        baris.Nama = pekerjaan.Nama;
        baris.Jenis = pekerjaan.Jenis;
        baris.TanggalMulai = pekerjaan.TanggalMulai;
        baris.TanggalSelesai = pekerjaan.TanggalSelesai;
        baris.Status = pekerjaan.Status;
        baris.TanggalDikunci = pekerjaan.TanggalDikunci;
        baris.KomponenId = pekerjaan.KomponenId;
        baris.BahanId = pekerjaan.BahanId;
        baris.RevisiLokasiSaatKunci = pekerjaan.RevisiLokasiSaatKunci;
        baris.OverrideAmbangJson = pekerjaan.OverrideAmbang is not null ? JsonSerializer.Serialize(pekerjaan.OverrideAmbang) : null;
        baris.KonfirmasiJson = pekerjaan.Konfirmasi is not null ? JsonSerializer.Serialize(pekerjaan.Konfirmasi) : null;
        baris.TerakhirDiperbaruiUtc = pekerjaan.TerakhirDiperbaruiUtc;

        if (keputusanBaru && pekerjaan.TanggalDikunci.HasValue)
        {
            db.KeputusanJadwal.Add(BentukKeputusan(pekerjaan));
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    private static KeputusanJadwalEntity BentukKeputusan(Pekerjaan p)
    {
        var k = p.KeputusanTerakhir;
        return new KeputusanJadwalEntity
        {
            Id = Guid.NewGuid(), PekerjaanId = p.Id, Tanggal = p.TanggalDikunci!.Value,
            Status = k?.Status.ToString() ?? "BelumTerverifikasi", Terkunci = true,
            CatatanJson = JsonSerializer.Serialize(k?.Catatan ?? ["Keputusan legacy; cuaca belum terverifikasi."]),
            DikunciPadaUtc = k?.DikunciPadaUtc ?? p.TerakhirDiperbaruiUtc,
            PersetujuanRisiko = k?.PersetujuanRisiko ?? p.Konfirmasi?.SetujuiRisiko ?? false,
            KonfirmasiDataLama = k?.KonfirmasiDataLama ?? p.Konfirmasi?.SetujuiDataLama ?? false,
            CatatanKonfirmasi = p.Konfirmasi?.Catatan,
            SnapshotCuacaJson = k is null ? null : JsonSerializer.Serialize(k),
            SnapshotAmbangJson = k?.AmbangEfektif is { } ambang ? JsonSerializer.Serialize(ambang) : null,
            UmurDataJam = k?.UmurDataSaatKunci?.TotalHours,
            DibuatPadaUtc = k?.DiputuskanPadaUtc ?? p.TerakhirDiperbaruiUtc
        };
    }

    private static Pekerjaan DenganKeputusan(Pekerjaan p, KeputusanJadwalEntity? entity)
    {
        if (string.IsNullOrWhiteSpace(entity?.SnapshotCuacaJson)) return p;
        try { return p with { KeputusanTerakhir = JsonSerializer.Deserialize<KeputusanJadwal>(entity.SnapshotCuacaJson) }; }
        catch (JsonException) { return p; } // Older rows did not contain a structured decision.
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Pekerjaan.Where(p => p.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
