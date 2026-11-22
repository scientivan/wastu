using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wastu.Data;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Emissions;
using Wastu.Domain.Scoring;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;
using Wastu.Reporting;

internal static class SupabaseVerification
{
    private sealed record Check(string Name, bool Passed, string? Detail = null);

    public static async Task<int> RunAsync(string output)
    {
        if (Environment.GetEnvironmentVariable("WASTU_ALLOW_SUPABASE_TESTS") != "1")
        {
            Console.WriteLine("Supabase verification requires explicit WASTU_ALLOW_SUPABASE_TESTS=1.");
            return 2;
        }
        var checks = new List<Check>();
        var runId = Guid.NewGuid().ToString("N");
        var schema = "wastu_v2_test_" + runId;
        var marker = "Wastu isolated verification " + runId;
        NpgsqlConnectionStringBuilder? admin = null;
        bool cleanupVerified = false, creationAttempted = false;
        Dictionary<string, string>? before = null;
        var timer = Stopwatch.StartNew();
        async Task SaveAsync() => await File.WriteAllTextAsync(Path.Combine(output, "supabase.json"),
            JsonSerializer.Serialize(new { runId, schema, creationAttempted, cleanupVerified,
                elapsedMs = timer.Elapsed.TotalMilliseconds, weather = "Fixture; not a real weather API test",
                checks, success = checks.Count > 0 && checks.All(c => c.Passed) && cleanupVerified },
                new JsonSerializerOptions { WriteIndented = true }));
        try
        {
            // Credentials remain in process memory; never put the DSN in output or arguments.
            admin = new(KoneksiSupabase.Ambil()) { Pooling = false, Timeout = 12,
                CommandTimeout = 30, IncludeErrorDetail = false, ApplicationName = "Wastu isolated verification" };
            if (admin.Host is not { } host || !(host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".supabase.com", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Configured endpoint is not Supabase.");
            before = await PublicFingerprintsAsync(admin.ConnectionString);
            checks.Add(new("Supabase connection and read-only baseline", true));
            var publicConfig = new NpgsqlConnectionStringBuilder(admin.ConnectionString) { SearchPath = "public" };
            var publicStatus = await new PemeriksaSkemaDatabase(new WastuDbContextFactory(publicConfig.ConnectionString)).PeriksaAsync();
            checks.Add(new("Current public schema compatible with this desktop version", publicStatus.ApakahKompatibel,
                "Applied migrations: " + publicStatus.MigrasiDiterapkan.Count + "; pending: " + publicStatus.MigrasiTertunda.Count));
            creationAttempted = true;
            await SaveAsync();
            await using (var conn = new NpgsqlConnection(admin.ConnectionString))
            {
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();
                await using var cmd = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"; COMMENT ON SCHEMA \"{schema}\" IS '{marker}'", conn, tx);
                await cmd.ExecuteNonQueryAsync();
                await tx.CommitAsync();
            }
            var scoped = new NpgsqlConnectionStringBuilder(admin.ConnectionString) { SearchPath = schema, Pooling = true };
            var factory = new WastuDbContextFactory(scoped.ConnectionString);
            await using (var conn = new NpgsqlConnection(scoped.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT current_schema()", conn);
                if ((string?)await cmd.ExecuteScalarAsync() != schema) throw new InvalidOperationException("Schema isolation failed.");
            }
            await using (var db = factory.CreateDbContext()) await db.Database.MigrateAsync();
            checks.Add(new("EF migrations completed in owned test schema", true));

            var projects = new ProyekRepositoryEf(factory);
            var materials = new MaterialRepositoryEf(factory);
            var evaluations = new EvaluasiRepositoryEf(factory);
            var batches = new BatchEvaluasiRepositoryEf(factory);
            var jobs = new PekerjaanRepositoryEf(factory);
            var cache = new PrakiraanCacheRepositoryEf(factory);
            var limits = new AmbangPekerjaanRepositoryEf(factory);
            var reports = new LaporanRepositoryEf(factory);
            var p = new Proyek(Guid.NewGuid(), "Wastu test " + runId, "Jakarta", -6.2, 106.8, zonaWaktu: "Asia/Jakarta");
            var wall = new Komponen(Guid.NewGuid(), p.Id, JenisKomponen.Dinding, 120);
            var roof = new Komponen(Guid.NewGuid(), p.Id, JenisKomponen.Atap, 90);
            await projects.AddAsync(p, komponen: [wall, roof]);
            checks.Add(new("Project and component persistence", (await projects.ListKomponenAsync(p.Id)).Count == 2));
            await projects.SimpanProfilIklimAsync(p.Id, new(2400, 80, 28, 5, TingkatKorosi.Tinggi,
                new MetadataIklim(new(2016, 1, 1), new(2025, 12, 31), "Fixture Supabase verification", null,
                    DateTimeOffset.UtcNow, "Asia/Jakarta", "Fixture", ProvenanceIklim.Contoh), provenance: ProvenanceIklim.Contoh));
            checks.Add(new("Canonical catalog has 15 walls and 10 roofs",
                (await materials.ListByKindAsync(JenisKomponen.Dinding)).Count == 15
                && (await materials.ListByKindAsync(JenisKomponen.Atap)).Count == 10));
            var scoring = new SuitabilityScoringService(materials, projects, batches, evaluations);
            var weights = new KriteriaBobot(.1, .2, .3, .4);
            var ranking = await scoring.RankAsync(wall.Id, weights);
            var repeated = await scoring.RankAsync(wall.Id, weights);
            checks.Add(new("Ranking snapshots, weights and deduplication", ranking.Count == 15
                && ranking.Select(e => e.Id).SequenceEqual(repeated.Select(e => e.Id))
                && ranking.All(e => e.Input?.Bobot == weights)));
            var top = ranking[0];
            var simulationService = new EmissionSimulationService(evaluations, materials);
            foreach (var horizon in new[] { 20, 30, 50 }) await simulationService.SimulateAsync(top.Id, horizon);
            await using (var db = factory.CreateDbContext()) checks.Add(new("20/30/50 year simulations persisted",
                await db.SimulasiEmisi.CountAsync(s => s.EvaluasiId == top.Id) == 3));

            var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, p.ZonaWaktu);
            var job = new Pekerjaan(Guid.NewGuid(), p.Id, "Fixture painting", JenisPekerjaan.Pengecatan,
                today, today.AddDays(29), overrideAmbang: new(30, null, 85, 10, 35));
            await jobs.AddAsync(job);
            await cache.SimpanCacheAsync(p.Latitude, p.Longitude,
                new([new(today, 90, 0, 20, 30, 70)], "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture; not real API"));
            var scheduling = new WorkSchedulingService(jobs, projects, cache, new NoNetworkForecast(), limits);
            var days = await scheduling.EvaluateDaysDetailedAsync(job.Id);
            checks.Add(new("Job evaluation, override and dates without forecast", days.Count == 30
                && days[0].Status == StatusKelayakanCuaca.TidakLayak
                && days[^1].Status == StatusKelayakanCuaca.DataTidakTersedia));
            bool rejected = false;
            try { await scheduling.LockDateAsync(job.Id, today); } catch (InvalidOperationException) { rejected = true; }
            await scheduling.LockDateAsync(job.Id, today, new KonfirmasiPenguncian(SetujuiRisiko: true));
            var restored = (await new PekerjaanRepositoryEf(factory).GetByIdAsync(job.Id))!;
            checks.Add(new("Risk consent required and actual weather survives reload", rejected
                && restored.KeputusanTerakhir?.PersetujuanRisiko == true
                && restored.KeputusanTerakhir.Cuaca?.ProbabilitasHujanPct == 90));

            var reportService = new ReportExportService(reports, projects, evaluations,
                pekerjaanRepository: jobs, pendingMetadataDirectory: Path.Combine(output, "pending"));
            var report = await reportService.ExportAsync(p.Id, new(30, true, Path.Combine(output, "supabase-report.pdf"),
                [wall.Id], [top.BahanId], [20, 30, 50]));
            var savedReport = (await new LaporanRepositoryEf(factory).GetByIdAsync(report.Id))!;
            var copy = await reportService.RegenerateAsync(savedReport, Path.Combine(output, "supabase-report-copy.pdf"));
            checks.Add(new("PDF metadata, snapshots and historical copy", savedReport.SnapshotJson is not null
                && copy.SnapshotHash == report.SnapshotHash && File.Exists(copy.PathBerkas)));

            // Probe known gaps as actual acceptance failures; do not turn them into passing assertions.
            await cache.SimpanCacheAsync(p.Latitude, p.Longitude,
                new([new(today, 20, 0, 20, 30, 70)], "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture A"));
            await cache.SimpanCacheAsync(p.Latitude + .0001, p.Longitude + .0001,
                new([new(today, 90, 0, 20, 30, 70)], "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture B"));
            var originalLocation = await cache.GetCacheAsync(p.Latitude, p.Longitude);
            checks.Add(new("Nearby coordinates have isolated forecast caches", originalLocation?.Hari.Single().ProbabilitasHujanPct == 20,
                "Expected A=20%; actual A=" + originalLocation?.Hari.Single().ProbabilitasHujanPct + "%"));
            try
            {
                await cache.SimpanCacheAsync(-6.9, 107.6,
                    new([new(today, double.NaN, 0, 20, 30, 70, false, "Fixture missing probability")],
                        "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture partial", DataParsial: true));
                var partial = await cache.GetCacheAsync(-6.9, 107.6);
                var partialDay = partial?.Hari.Single();
                checks.Add(new("Partial forecast can persist without inventing zero", partial?.DataParsial == true
                    && partialDay is { DataLengkap: false } && double.IsNaN(partialDay.ProbabilitasHujanPct)));
            }
            catch (Exception ex) { checks.Add(new("Partial forecast can persist without inventing zero", false, SafeError(ex))); }

            var testProject = FindRepositoryFile("tests/Wastu.Data.Tests/Wastu.Data.Tests.csproj");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "test", testProject, "-c", "Release", "--no-build", "--nologo", "--filter", "FullyQualifiedName~AlurSupabaseTests" }) start.ArgumentList.Add(arg);
            start.Environment[KoneksiSupabase.NamaVariabelLingkungan] = scoped.ConnectionString;
            start.Environment["WASTU_SUPABASE_TEST_SCHEMA"] = schema;
            start.Environment["WASTU_ALLOW_SUPABASE_TESTS"] = "1";
            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try { await child.WaitForExitAsync(deadline.Token); }
            catch (OperationCanceledException) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
            var raw = await stdout;
            await stderr; // Do not write raw failure text, which could contain connection details.
            bool ranOne = Regex.IsMatch(raw, @"Passed:\s+1\b") && Regex.IsMatch(raw, @"Skipped:\s+0\b");
            checks.Add(new("AlurSupabaseTests ran in isolated schema", child.ExitCode == 0 && ranOne,
                "Exit code " + child.ExitCode + "; exactly one passing, non-skipped test: " + ranOne));

            if (Environment.GetEnvironmentVariable("WASTU_SUPABASE_DESKTOP_SCRIPT") is { Length: > 0 } script)
            {
                await projects.DeleteAsync(p.Id);
                if ((await projects.ListAsync()).Count != 0)
                    throw new InvalidOperationException("Desktop verification requires an empty test workspace.");
                var exe = Environment.GetEnvironmentVariable("WASTU_SUPABASE_DESKTOP_EXE")
                    ?? throw new InvalidOperationException("Specify the published Wastu.exe.");
                var ui = new ProcessStartInfo("python") { UseShellExecute = false, CreateNoWindow = true };
                foreach (var argument in new[] { script, exe, output, Path.Combine(output, "desktop-state") })
                    ui.ArgumentList.Add(argument);
                ui.Environment[KoneksiSupabase.NamaVariabelLingkungan] = scoped.ConnectionString;
                ui.Environment["WASTU_STORAGE_MODE"] = "postgres";
                ui.Environment.Remove("WASTU_SNAPSHOT");
                using var desktopTest = Process.Start(ui)!;
                using var uiDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(8));
                try { await desktopTest.WaitForExitAsync(uiDeadline.Token); }
                catch (OperationCanceledException) { desktopTest.Kill(entireProcessTree: true); await desktopTest.WaitForExitAsync(); }
                checks.Add(new("Published Windows desktop uses Supabase for F1–F7", desktopTest.ExitCode == 0
                    && File.Exists(Path.Combine(output, "desktop.json"))));
            }
        }
        catch (Exception ex) { checks.Add(new("Verification completed without setup/runtime failure", false, SafeError(ex))); }
        finally
        {
            if (admin is not null && creationAttempted)
            {
                NpgsqlConnection.ClearAllPools();
                for (int attempt = 0; attempt < 3 && !cleanupVerified; attempt++)
                {
                    try
                    {
                        await using var conn = new NpgsqlConnection(admin.ConnectionString);
                        await conn.OpenAsync();
                        await using var ownership = new NpgsqlCommand("SELECT obj_description(oid, 'pg_namespace') FROM pg_namespace WHERE nspname = @schema", conn);
                        ownership.Parameters.AddWithValue("schema", schema);
                        var actualMarker = await ownership.ExecuteScalarAsync();
                        if (actualMarker is string text && text == marker)
                        {
                            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", conn);
                            await drop.ExecuteNonQueryAsync();
                        }
                        else if (actualMarker is not null) throw new InvalidOperationException("Schema ownership marker mismatch; cleanup refused.");
                        await using var exists = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = @schema)", conn);
                        exists.Parameters.AddWithValue("schema", schema);
                        cleanupVerified = (bool?)await exists.ExecuteScalarAsync() == false;
                    }
                    catch (Exception ex) { if (attempt == 2) checks.Add(new("Test schema cleanup", false, SafeError(ex))); }
                }
                checks.Add(new("Owned test schema and all test rows removed", cleanupVerified));
            }
            if (admin is not null && before is not null)
            {
                try
                {
                    var after = await PublicFingerprintsAsync(admin.ConnectionString);
                    checks.Add(new("Existing public data unchanged", before.Count == after.Count
                        && before.All(k => after.TryGetValue(k.Key, out var fingerprint) && fingerprint == k.Value)));
                }
                catch (Exception ex) { checks.Add(new("Verify existing public data", false, SafeError(ex))); }
            }
            await SaveAsync();
        }
        Console.WriteLine(JsonSerializer.Serialize(new { checks = checks.Count, passed = checks.Count(c => c.Passed),
            failed = checks.Where(c => !c.Passed).Select(c => c.Name), cleanupVerified }));
        return checks.All(c => c.Passed) && cleanupVerified ? 0 : 1;
    }

    private static string SafeError(Exception ex)
    {
        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        return pg is null ? ex.GetType().Name : "PostgreSQL " + pg.SqlState + "; constraint=" + pg.ConstraintName;
    }

    private static async Task<Dictionary<string, string>> PublicFingerprintsAsync(string connection)
    {
        var result = new Dictionary<string, string>();
        await using var conn = new NpgsqlConnection(connection);
        await conn.OpenAsync();
        foreach (var table in new[] { "bangunan", "komponen", "profil_iklim", "bahan", "sumber_properti_bahan",
            "batch_evaluasi", "evaluasi", "simulasi_emisi", "pekerjaan", "keputusan_jadwal", "prakiraan_cache", "laporan", "ambang_pekerjaan", "konfigurasi_model" })
        {
            await using var exists = new NpgsqlCommand("SELECT to_regclass(@relation) IS NOT NULL", conn);
            exists.Parameters.AddWithValue("relation", "public." + table);
            if ((bool?)await exists.ExecuteScalarAsync() != true) continue;
            await using var cmd = new NpgsqlCommand($"SELECT count(*)::text || ':' || md5(COALESCE(string_agg(md5(row_to_json(t)::text), '' ORDER BY row_to_json(t)::text), '')) FROM public.\"{table}\" t", conn);
            result[table] = (string)(await cmd.ExecuteScalarAsync())!;
        }
        return result;
    }

    private static string FindRepositoryFile(string relative)
    {
        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Repository test project not found.");
    }

    private sealed class NoNetworkForecast : IDailyForecastClient
    {
        public Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days, CancellationToken ct = default)
            => throw new InvalidOperationException("Expected the supplied forecast fixture.");
    }
}
