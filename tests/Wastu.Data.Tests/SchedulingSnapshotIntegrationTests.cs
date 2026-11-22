using Microsoft.EntityFrameworkCore;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Data.Tests;

public sealed class SchedulingSnapshotIntegrationTests
{
    [PerluPostgreSql]
    public async Task RiskDecisionWeatherThresholdAndLocationSurviveReloadAndReviewWithoutDuplication()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var db = database.Factory.CreateDbContext()) await db.Database.MigrateAsync();
        var projects = new ProyekRepositoryEf(database.Factory);
        var jobs = new PekerjaanRepositoryEf(database.Factory);
        var cache = new PrakiraanCacheRepositoryEf(database.Factory);
        var limits = new AmbangPekerjaanRepositoryEf(database.Factory);
        var project = new Proyek(Guid.NewGuid(), "Rumah uji", "Jakarta", -6.2, 106.8, 4, "Asia/Jakarta");
        await projects.AddAsync(project);
        var date = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, project.ZonaWaktu);
        var job = new Pekerjaan(Guid.NewGuid(), project.Id, "Cat", JenisPekerjaan.Pengecatan, date, date,
            overrideAmbang: new(30, null, 85, 10, 35));
        await jobs.AddAsync(job);
        var weather = new DailyForecast(date, 35, 0, 20, 30, 70);
        await cache.SimpanCacheAsync(project.Latitude, project.Longitude,
            new([weather], "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture", 4));
        var service = new WorkSchedulingService(jobs, projects, cache, new NoNetworkClient(), limits);
        await service.LockDateAsync(job.Id, date, new KonfirmasiPenguncian(SetujuiRisiko: true));
        var restored = await new PekerjaanRepositoryEf(database.Factory).GetByIdAsync(job.Id);
        Assert.NotNull(restored);
        Assert.Equal(4, restored.RevisiLokasiSaatKunci);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, restored.KeputusanTerakhir!.Status);
        Assert.Equal(weather, restored.KeputusanTerakhir.Cuaca);
        Assert.Equal(30, restored.KeputusanTerakhir.AmbangEfektif!.ProbabilitasHujanMaksPct);
        Assert.True(restored.KeputusanTerakhir.PersetujuanRisiko);
        await jobs.UpdateAsync(restored with { Status = StatusPekerjaan.PerluDitinjau });
        await cache.PruneExpiredAsync(DateTimeOffset.UtcNow.AddDays(1));
        var reviewed = await jobs.GetByIdAsync(job.Id);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(restored.KeputusanTerakhir),
            System.Text.Json.JsonSerializer.Serialize(reviewed!.KeputusanTerakhir));
        await using var check = database.Factory.CreateDbContext();
        Assert.Equal(1, await check.KeputusanJadwal.CountAsync(k => k.PekerjaanId == job.Id));
    }

    private sealed class NoNetworkClient : IDailyForecastClient
    {
        public Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days,
            CancellationToken ct = default) => throw new InvalidOperationException("Expected cached forecast.");
    }
}
