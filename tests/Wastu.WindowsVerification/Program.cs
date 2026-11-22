using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wastu.Data;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.Domain.Weather;
using Wastu.Services;

if (args.Length != 2) throw new ArgumentException("Use api|database|benchmark <output-directory>.");
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
if (args[0] == "benchmark")
{
    var measurements = new List<object>();
    foreach (var kind in new[] { JenisKomponen.Dinding, JenisKomponen.Atap })
    {
        var component = new Komponen(Guid.NewGuid(), Guid.NewGuid(), kind, 120);
        var materials = KatalogSeed.BahanDomain.Where(b => b.Jenis == kind).ToArray();
        var climate = new ProfilIklim(2400, 80, 28, 5, TingkatKorosi.Tinggi);
        var times = new List<double>();
        for (int i = 0; i < 105; i++)
        {
            var watch = Stopwatch.StartNew();
            var ranking = Wastu.Domain.Scoring.SuitabilityScoringService.Rank(component, climate, materials);
            watch.Stop();
            if (ranking.Count != materials.Length) throw new InvalidOperationException("Incomplete ranking.");
            if (i >= 5) times.Add(watch.Elapsed.TotalMilliseconds);
        }
        times.Sort();
        measurements.Add(new { kind = kind.ToString(), materials = materials.Length, samples = times.Count,
            medianMs = times[50], p95Ms = times[94], maxMs = times[^1], cache = false, http = false, ui = false });
    }
    await File.WriteAllTextAsync(Path.Combine(output, "ranking.json"), JsonSerializer.Serialize(measurements,
        new JsonSerializerOptions { WriteIndented = true }));
    return;
}
if (args[0] == "api")
{
    var client = new OpenMeteo();
    var measurements = new List<object>();
    var timer = Stopwatch.StartNew();
    try
    {
        var profile = await client.GetAnnualProfileAsync(-6.2, 106.8);
        var state = new AppState();
        state.TambahProyek("Smoke Windows", "Jakarta", -6.2, 106.8,
            [(Wastu.Models.JenisKomponen.Dinding, 120)], profile.JarakPesisirKm);
        state.Aktif.ProfilIklim = new Wastu.Models.ProfilIklim
        {
            CurahHujanTahunan = profile.CurahHujanTahunanMm, KelembapanRata = profile.KelembapanRataPct,
            SuhuRata = profile.SuhuRataC, JarakPesisirKm = profile.JarakPesisirKm,
            Provenance = profile.Provenance, DiambilPadaUtc = profile.DiambilPada,
            StatusPengambilan = StatusPengambilanIklim.Tersedia
        };
        _ = state.Peringkat();
        measurements.Add(new { type = "historical-to-ranking", success = true, elapsedMs = timer.Elapsed.TotalMilliseconds,
            profile.Metadata?.PeriodeAwal, profile.Metadata?.PeriodeAkhir, profile.Provenance });
    }
    catch (Exception ex) { measurements.Add(new { type = "historical-to-ranking", success = false,
        elapsedMs = timer.Elapsed.TotalMilliseconds, error = ex.GetType().Name }); }
    for (int i = 0; i < 20; i++)
    {
        timer.Restart();
        try
        {
            var result = await client.GetForecastWithMetadataAsync(-6.2 + i * 0.001, 106.8, 14);
            int complete = result.Hari.Count(d => d.DataLengkap);
            measurements.Add(new { type = "forecast", sample = i + 1, success = complete == 14,
                completeDays = complete, result.ZonaWaktu, elapsedMs = timer.Elapsed.TotalMilliseconds });
        }
        catch (Exception ex) { measurements.Add(new { type = "forecast", sample = i + 1, success = false,
            elapsedMs = timer.Elapsed.TotalMilliseconds, error = ex.GetType().Name }); }
        await File.WriteAllTextAsync(Path.Combine(output, "api.json"), JsonSerializer.Serialize(measurements,
            new JsonSerializerOptions { WriteIndented = true }));
        await Task.Delay(2000);
    }
    return;
}
if (args[0] != "database") throw new ArgumentException("Unknown mode.");
var connection = Environment.GetEnvironmentVariable("WASTU_TEST_CONNECTION_STRING")
    ?? throw new InvalidOperationException("An isolated test connection is required.");
var config = new NpgsqlConnectionStringBuilder(connection);
if (config.Host != "127.0.0.1" || config.Database != "wastu_windows_ui")
    throw new InvalidOperationException("Only the dedicated loopback wastu_windows_ui database is allowed.");
var factory = new WastuDbContextFactory(connection);
await using (var db = factory.CreateDbContext()) await db.Database.MigrateAsync();
var projects = new ProyekRepositoryEf(factory);
if ((await projects.ListAsync()).Count == 0)
{
    var id = Guid.NewGuid();
    await projects.AddAsync(new(id, "Rumah PostgreSQL Windows", "Jakarta", -6.2, 106.8, 1, "Asia/Jakarta"),
        komponen: [new(Guid.NewGuid(), id, JenisKomponen.Dinding, 120), new(Guid.NewGuid(), id, JenisKomponen.Atap, 90)]);
    await projects.SimpanProfilIklimAsync(id, new(2400, 80, 28, 5, TingkatKorosi.Tinggi,
        provenance: ProvenanceIklim.Contoh));
}
Environment.SetEnvironmentVariable("WASTU_CONNECTION_STRING", connection);
var online = await AppState.MuatAsync();
if (!online.PostgreSql || !online.AdaProyek) throw new InvalidOperationException("Online startup failed.");
online.Horizon = 20;
await online.Workflows.SimulateAsync();
online.Horizon = 50;
await online.Workflows.SimulateAsync();
var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, "Asia/Jakarta");
var cache = new PrakiraanCacheRepositoryEf(factory);
await cache.SimpanCacheAsync(online.Aktif.Latitude, online.Aktif.Longitude,
    new([new(today, 90, 0, 20, 30, 70)], "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture Windows", 1));
online.Aktif.PrakiraanCache = await cache.GetCacheAsync(online.Aktif.Latitude, online.Aktif.Longitude);
var job = new Wastu.Models.Pekerjaan { Nama = "Cat risiko", Jenis = Wastu.Models.JenisPekerjaan.Pengecatan,
    TanggalMulaiRencana = today.ToDateTime(TimeOnly.MinValue), TanggalSelesaiRencana = today.AddDays(29).ToDateTime(TimeOnly.MinValue) };
await online.Workflows.AddJobAsync(job);
await online.Workflows.LockJobAsync(job.Id, today, new(SetujuiRisiko: true));
var selected = online.Terpilih().Select(i => i.Kode).ToHashSet();
await online.Workflows.ExportAsync(ReportSnapshotFactory.Build(online, online.Jenis, 50, true, selected),
    Path.Combine(output, "database-report.pdf"));
online.Ubah();
online.SimpanSekarang();
var restored = await AppState.MuatAsync();
if (!restored.Aktif.Pekerjaan.Any(j => j.Keputusan?.PersetujuanRisiko == true)
    || restored.Aktif.Laporan.Count == 0) throw new InvalidOperationException("Database restart lost workflow data.");
await File.WriteAllTextAsync(Path.Combine(output, "database.json"), JsonSerializer.Serialize(new
{
    success = true, restored.LabelModePenyimpanan, projects = restored.Proyek.Count,
    jobs = restored.Aktif.Pekerjaan.Count, reports = restored.Aktif.Laporan.Count,
    simulations = restored.Aktif.RiwayatSimulasi.Count
}));
