using Wastu.Domain;
using Wastu.Services;
using Xunit;
using ProfilIklim = Wastu.Models.ProfilIklim;

namespace Wastu.Application.Tests;

public sealed class ReportClimateSnapshotTests
{
    [Fact]
    public void HistoricalCache_PreservesActualProvenancePeriodSourceAndRetrievalTime()
    {
        var retrieved = new DateTimeOffset(2026, 10, 12, 3, 45, 0, TimeSpan.Zero);
        var climate = Climate();
        climate.Provenance = ProvenanceIklim.CacheHistoris;
        climate.Provider = "Penyedia iklim tercatat";
        climate.Sumber = "Agregasi seri stasiun tersimpan";
        climate.PeriodeMulai = new DateOnly(2014, 3, 2);
        climate.PeriodeSelesai = new DateOnly(2023, 9, 17);
        climate.DiambilPadaUtc = retrieved;

        var snapshot = SnapshotIklimMapper.Buat(climate, offline: true);

        Assert.Equal("CacheHistoris", snapshot.Provenance);
        Assert.Equal(climate.Provider, snapshot.Provider);
        Assert.Contains(climate.Sumber, snapshot.CatatanOffline!);
        Assert.Contains("2014-03-02", snapshot.Periode);
        Assert.Contains("2023-09-17", snapshot.Periode);
        Assert.Equal(retrieved, snapshot.DiambilPada);
        Assert.True(snapshot.IsOffline);
        Assert.Equal(climate.CurahHujanTahunan, snapshot.CurahHujanTahunanMm);
        Assert.Equal(climate.KelembapanRata, snapshot.KelembapanRataPct);
        Assert.Equal(climate.SuhuRata, snapshot.SuhuRataC);
        Assert.Equal(climate.JarakPesisirKm, snapshot.JarakPesisirKm);
    }

    [Theory]
    [InlineData(ProvenanceIklim.EstimasiBawaan)]
    [InlineData(ProvenanceIklim.LegacyUnknown)]
    [InlineData(ProvenanceIklim.Contoh)]
    public void UnverifiedClimate_NeverFabricatesHistoricalSourceOrFreshTimestamp(ProvenanceIklim provenance)
    {
        var climate = Climate();
        climate.Provenance = provenance;
        climate.DiperbaruiPada = "Tanggal teks legacy tanpa timestamp terstruktur";

        var snapshot = SnapshotIklimMapper.Buat(climate, offline: false);

        Assert.Equal(provenance.ToString(), snapshot.Provenance);
        Assert.Null(snapshot.DiambilPada);
        Assert.DoesNotContain("Open-Meteo", snapshot.Provider);
        Assert.DoesNotContain("10 tahun", snapshot.Periode);
        Assert.False(snapshot.IsOffline);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.CatatanOffline));
    }

    [Fact]
    public void StaleMismatchedOrFailedClimate_RemainsExplicitInWarnings()
    {
        var climate = Climate();
        climate.Provenance = ProvenanceIklim.CacheHistoris;
        climate.KualitasCache = KualitasCacheIklim.TidakCocokLokasi;
        climate.StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui;

        var mismatch = SnapshotIklimMapper.Buat(climate, offline: false);

        Assert.Contains("tidak cocok", mismatch.CatatanOffline!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gagal", mismatch.CatatanOffline!, StringComparison.OrdinalIgnoreCase);
        climate.KualitasCache = KualitasCacheIklim.DataLama;
        var stale = SnapshotIklimMapper.Buat(climate, offline: false);
        Assert.Contains("data lama", stale.CatatanOffline!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CoastalManualOverrideAndUnavailableDistance_AreNotLost()
    {
        var climate = Climate();
        climate.CatatanOverridePesisir = "Diukur manual dari peta proyek";
        climate.PesisirTersedia = false;
        climate.VersiAlgoritmePesisir = "metode-pesisir-uji-v3";

        var snapshot = SnapshotIklimMapper.Buat(climate, offline: false);

        Assert.Contains(climate.CatatanOverridePesisir, snapshot.PesisirDisclaimer);
        Assert.Contains("manual", snapshot.PesisirDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tidak tersedia", snapshot.PesisirDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(climate.VersiAlgoritmePesisir, snapshot.PesisirDisclaimer);
    }

    [Fact]
    public void PartialPeriodAndSourceOnly_DoNotInventMissingMetadata()
    {
        var climate = Climate();
        climate.Sumber = "Referensi estimasi regional";
        climate.PeriodeMulai = new DateOnly(2016, 5, 20);

        var snapshot = SnapshotIklimMapper.Buat(climate, offline: false);

        Assert.Equal(climate.Sumber, snapshot.Provider);
        Assert.Contains("2016-05-20", snapshot.Periode);
        Assert.Contains("tidak tercatat", snapshot.Periode, StringComparison.OrdinalIgnoreCase);
        Assert.Null(snapshot.DiambilPada);
    }

    private static ProfilIklim Climate() => new()
    {
        CurahHujanTahunan = 2350,
        KelembapanRata = 80,
        SuhuRata = 27,
        JarakPesisirKm = 12,
        Provenance = ProvenanceIklim.EstimasiBawaan,
        StatusPengambilan = StatusPengambilanIklim.Tersedia
    };
}
