using System.Reflection;
using Wastu.Models;
using Wastu.Services;
using Xunit;

namespace Wastu.Application.Tests;

public sealed class ProjectInputIntegrityTests
{
    [Fact]
    public void ImporTanpaLuasTidakMembuatDindingFiktifDanDapatDiperbaiki()
    {
        var dto = new StateDto { Proyek = [new() { Id = 1, Nama = "Legacy", Kota = "Jakarta", Lintang = -6.2, Bujur = 106.8 }] };
        var state = new AppState();
        typeof(AppState).GetMethod("TerapkanDto", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [state, dto]);
        Assert.Empty(state.Aktif.Komponen);
        Assert.NotNull(state.Aktif.MasalahData);
        Assert.Empty(state.Peringkat());
        Assert.Empty(state.Terpilih());
        state.Aktif.AturKomponen([(JenisKomponen.Atap, 90)]);
        Assert.Null(state.Aktif.MasalahData);
        Assert.NotEmpty(state.Peringkat(JenisKomponen.Atap));
    }

    [Theory]
    [InlineData(94.9)]
    [InlineData(141.1)]
    public void KoordinatLegacyDiLuarBatasDipertahankanTanpaHasilPalsu(double longitude)
    {
        var dto = new StateDto { Proyek = [new() { Id = 1, Nama = "Legacy", Kota = "Asal",
            Lintang = -6.2, Bujur = longitude, Dinding = 120 }] };
        var state = new AppState();
        typeof(AppState).GetMethod("TerapkanDto", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [state, dto]);
        Assert.Equal(longitude, state.Aktif.Longitude);
        Assert.Equal(120, state.Aktif.Komponen[0].Luas);
        Assert.NotNull(state.Aktif.MasalahData);
        Assert.Empty(state.Peringkat());
        Assert.Empty(state.Terpilih());
    }

    [Theory]
    [InlineData("", -6.2, 106.8, 120)]
    [InlineData("Rumah", double.NaN, 106.8, 120)]
    [InlineData("Rumah", -6.2, 94.9, 120)]
    [InlineData("Rumah", -6.2, 106.8, double.PositiveInfinity)]
    [InlineData("Rumah", -6.2, 106.8, 0)]
    public async Task JalurLokalMenolakInputTidakValidSebelumMutasi(string nama, double lat, double lon, double luas)
    {
        var state = new AppState();
        Assert.ThrowsAny<ArgumentException>(() => state.TambahProyek(nama, "Jakarta", lat, lon, [(JenisKomponen.Dinding, luas)], 10));
        Assert.Empty(state.Proyek);
        state.TambahProyek("Asli", "Jakarta", -6.2, 106.8, [(JenisKomponen.Dinding, 120)], 10);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => state.UbahProyekAsync(nama, "Jakarta", lat, lon, [(JenisKomponen.Dinding, luas)]));
        Assert.Equal("Asli", state.Aktif.Nama);
        Assert.Equal(-6.2, state.Aktif.Latitude);
        Assert.Equal(120, state.Aktif.Komponen[0].Luas);
    }
}
