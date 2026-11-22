using Microsoft.EntityFrameworkCore;

namespace Wastu.Data;

/// <summary>
/// Hasil analisis status kecocokan skema database terhadap migrasi aplikasi (PRD §6.3).
/// </summary>
public sealed record HasilPemeriksaanSkema(
    bool ApakahKompatibel,
    bool AdaMigrasiTertunda,
    IReadOnlyList<string> MigrasiDiterapkan,
    IReadOnlyList<string> MigrasiTertunda,
    bool DatabaseDapatDiakses,
    string? Pesan = null);

/// <summary>
/// Helper untuk memeriksa status migrasi yang diterapkan dan tertunda pada database remote/lokal
/// tanpa melakukan auto-migrasi otomatis untuk mencegah migrasi race condition antar-klien (PRD §6.3).
/// </summary>
public sealed class PemeriksaSkemaDatabase
{
    private readonly IDbContextFactory<WastuDbContext> _pabrik;

    public PemeriksaSkemaDatabase(IDbContextFactory<WastuDbContext> pabrik)
    {
        ArgumentNullException.ThrowIfNull(pabrik);
        _pabrik = pabrik;
    }

    public async Task<HasilPemeriksaanSkema> PeriksaAsync(CancellationToken ct = default)
    {
        try
        {
            await using WastuDbContext db = await _pabrik.CreateDbContextAsync(ct).ConfigureAwait(false);
            bool canConnect = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
            if (!canConnect)
            {
                return new HasilPemeriksaanSkema(
                    ApakahKompatibel: false,
                    AdaMigrasiTertunda: false,
                    MigrasiDiterapkan: [],
                    MigrasiTertunda: [],
                    DatabaseDapatDiakses: false,
                    Pesan: "Database tidak dapat dihubungi.");
            }

            var diterapkan = (await db.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToList();
            var tertunda = (await db.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).ToList();

            bool adaTertunda = tertunda.Count > 0;
            bool kompatibel = !adaTertunda && diterapkan.Count > 0;

            string pesan = adaTertunda
                ? $"Terdapat {tertunda.Count} migrasi yang belum diterapkan: {string.Join(", ", tertunda)}"
                : "Skema database up-to-date dan kompatibel.";

            return new HasilPemeriksaanSkema(
                ApakahKompatibel: kompatibel,
                AdaMigrasiTertunda: adaTertunda,
                MigrasiDiterapkan: diterapkan,
                MigrasiTertunda: tertunda,
                DatabaseDapatDiakses: true,
                Pesan: pesan);
        }
        catch (Exception ex)
        {
            return new HasilPemeriksaanSkema(
                ApakahKompatibel: false,
                AdaMigrasiTertunda: false,
                MigrasiDiterapkan: [],
                MigrasiTertunda: [],
                DatabaseDapatDiakses: false,
                Pesan: ex.Message);
        }
    }
}
