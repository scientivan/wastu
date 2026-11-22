using System.Reflection;
using System.Text.Json;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Services;
using Xunit;

namespace Wastu.Application.Tests;

public sealed class ClimateTimezoneIntegrityTests
{
    [Fact]
    public async Task ProviderTimezoneAndModelSurviveDesktopMappingAndRestart()
    {
        var state = new AppState();
        state.TambahProyek("Rumah", "Ambon", -3.7, 128.2, [(Models.JenisKomponen.Dinding, 120)], 50);
        state.HistoricalClient = new ClimateClient();
        Assert.Null(await state.PerbaruiIklimAsync(state.Aktif));
        Assert.Equal("Asia/Jayapura", state.Aktif.ZonaWaktu);
        var domain = state.Iklim.KeDomain();
        Assert.Equal("Asia/Jayapura", domain.Metadata!.ZonaWaktu);
        Assert.Equal("Fixture-model", domain.Metadata.Model);
        var saved = typeof(AppState).GetMethod("KeDto", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var restore = typeof(AppState).GetMethod("TerapkanDto", BindingFlags.NonPublic | BindingFlags.Static)!;
        var dto = JsonSerializer.Deserialize<StateDto>(JsonSerializer.Serialize((StateDto)saved.Invoke(state, null)!))!;
        var restarted = new AppState();
        restore.Invoke(null, [restarted, dto]);
        Assert.Equal("Asia/Jayapura", restarted.Aktif.ZonaWaktu);
        Assert.Equal("Fixture-model", restarted.Iklim.KeDomain().Metadata!.Model);
        var when = new DateTimeOffset(2026, 11, 22, 16, 0, 0, TimeSpan.Zero);
        Assert.Equal("23 November 2026, 01.00 WIT", Format.WaktuProyek(when, restarted.Aktif.ZonaWaktu));
        Assert.Equal("22 November 2026, 23.00 WIB", Format.WaktuProyek(when, "Asia/Jakarta"));
    }

    private sealed class ClimateClient : IHistoricalClimateClient
    {
        public Task<ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
            => Task.FromResult(new ProfilIklim(700, 80, 26, 50, TingkatKorosi.Rendah,
                new MetadataIklim(new(2016, 1, 1), new(2025, 12, 31), "Fixture", "Fixture-model",
                    new(2026, 11, 22, 0, 0, 0, TimeSpan.Zero), "Asia/Jayapura", "Fixture", ProvenanceIklim.Contoh)));
    }
}
