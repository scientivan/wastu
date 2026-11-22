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

        return baris.Select(Pemetaan.KeDomain).ToList();
    }

    public async Task<Pekerjaan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);

        var baris = await db.Pekerjaan.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return baris is null ? null : Pemetaan.KeDomain(baris);
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
            db.KeputusanJadwal.Add(new KeputusanJadwalEntity
            {
                Id = Guid.NewGuid(),
                PekerjaanId = pekerjaan.Id,
                Tanggal = pekerjaan.TanggalDikunci.Value,
                Status = "Terkunci",
                CatatanJson = JsonSerializer.Serialize(new[] { "Jadwal dikunci oleh pengguna." }),
                Terkunci = true,
                DikunciPadaUtc = pekerjaan.TerakhirDiperbaruiUtc,
                PersetujuanRisiko = pekerjaan.Konfirmasi?.SetujuiRisiko ?? false,
                KonfirmasiDataLama = pekerjaan.Konfirmasi?.SetujuiDataLama ?? false,
                CatatanKonfirmasi = pekerjaan.Konfirmasi?.Catatan,
                SnapshotAmbangJson = pekerjaan.OverrideAmbang is not null ? JsonSerializer.Serialize(pekerjaan.OverrideAmbang) : null,
                DibuatPadaUtc = DateTimeOffset.UtcNow
            });
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

        bool baruDikunci = baris.Status != StatusPekerjaan.Terkunci && pekerjaan.Status == StatusPekerjaan.Terkunci;

        baris.Nama = pekerjaan.Nama;
        baris.Jenis = pekerjaan.Jenis;
        baris.TanggalMulai = pekerjaan.TanggalMulai;
        baris.TanggalSelesai = pekerjaan.TanggalSelesai;
        baris.Status = pekerjaan.Status;
        baris.TanggalDikunci = pekerjaan.TanggalDikunci;
        baris.KomponenId = pekerjaan.KomponenId;
        baris.BahanId = pekerjaan.BahanId;
        baris.OverrideAmbangJson = pekerjaan.OverrideAmbang is not null ? JsonSerializer.Serialize(pekerjaan.OverrideAmbang) : null;
        baris.KonfirmasiJson = pekerjaan.Konfirmasi is not null ? JsonSerializer.Serialize(pekerjaan.Konfirmasi) : null;
        baris.TerakhirDiperbaruiUtc = pekerjaan.TerakhirDiperbaruiUtc;

        if ((baruDikunci || pekerjaan.Status == StatusPekerjaan.Terkunci) && pekerjaan.TanggalDikunci.HasValue)
        {
            db.KeputusanJadwal.Add(new KeputusanJadwalEntity
            {
                Id = Guid.NewGuid(),
                PekerjaanId = pekerjaan.Id,
                Tanggal = pekerjaan.TanggalDikunci.Value,
                Status = "Terkunci",
                CatatanJson = JsonSerializer.Serialize(new[] { "Jadwal dikunci oleh pengguna." }),
                Terkunci = true,
                DikunciPadaUtc = pekerjaan.TerakhirDiperbaruiUtc,
                PersetujuanRisiko = pekerjaan.Konfirmasi?.SetujuiRisiko ?? false,
                KonfirmasiDataLama = pekerjaan.Konfirmasi?.SetujuiDataLama ?? false,
                CatatanKonfirmasi = pekerjaan.Konfirmasi?.Catatan,
                SnapshotAmbangJson = pekerjaan.OverrideAmbang is not null ? JsonSerializer.Serialize(pekerjaan.OverrideAmbang) : null,
                DibuatPadaUtc = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaksi.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Pekerjaan.Where(p => p.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
