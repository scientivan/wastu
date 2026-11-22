using System.Text.Json;
using Wastu.Domain;
using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests.Weather;

public class ParserOpenMeteoTests
{
    [Fact]
    public void T26_ParsingPrakiraan_MenggunakanRHMaksimumHarianBukanRataRata()
    {
        // 3 jam: 70%, 80%, 90%. Rata-rata = 80%, tetapi MAX = 90% (T26).
        var json = """
        {
          "latitude": -6.2,
          "longitude": 106.8,
          "timezone": "Asia/Jakarta",
          "daily": {
            "time": ["2026-11-20"],
            "precipitation_probability_max": [30],
            "precipitation_sum": [1.5],
            "temperature_2m_min": [24.0],
            "temperature_2m_max": [32.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00", "2026-11-20T12:00", "2026-11-20T16:00"],
            "relative_humidity_2m": [70.0, 80.0, 90.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json, DateTimeOffset.UtcNow, 1);

        Assert.Single(hasil.Hari);
        var hari = hasil.Hari[0];
        Assert.Equal(new DateOnly(2026, 11, 20), hari.Tanggal);
        Assert.Equal(30, hari.ProbabilitasHujanPct);
        Assert.Equal(1.5, hari.CurahHujanMm);
        Assert.Equal(24.0, hari.SuhuMinC);
        Assert.Equal(32.0, hari.SuhuMaksC);
        // T26: RH maksimum 90% digunakan, BUKAN rata-rata 80%
        Assert.Equal(90.0, hari.KelembapanPct);
        Assert.True(hari.DataLengkap);
        Assert.Null(hari.CatatanKualitas);
        Assert.False(hasil.DataParsial);
        Assert.Equal("Asia/Jakarta", hasil.ZonaWaktu);
    }

    [Fact]
    public void ParsingPrakiraan_MengekstrakZonaWaktuPenyedia()
    {
        var json = """
        {
          "timezone": "Asia/Jayapura",
          "daily": {
            "time": ["2026-11-21"],
            "precipitation_probability_max": [20],
            "precipitation_sum": [0.0],
            "temperature_2m_min": [25.0],
            "temperature_2m_max": [31.0]
          },
          "hourly": {
            "time": ["2026-11-21T09:00"],
            "relative_humidity_2m": [75.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Equal("Asia/Jayapura", hasil.ZonaWaktu);
    }

    [Fact]
    public void T27_ProbabilitasHujanNull_DitandaiDataTidakLengkapDanBukanNol()
    {
        var json = """
        {
          "daily": {
            "time": ["2026-11-20"],
            "precipitation_probability_max": [null],
            "precipitation_sum": [0.0],
            "temperature_2m_min": [24.0],
            "temperature_2m_max": [32.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00"],
            "relative_humidity_2m": [70.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Single(hasil.Hari);
        var hari = hasil.Hari[0];
        Assert.False(hari.DataLengkap);
        Assert.Contains("Probabilitas hujan bernilai null", hari.CatatanKualitas);
        Assert.True(hasil.DataParsial);
    }

    [Fact]
    public void T27_ArrayTimpang_DitandaiDataTidakLengkap()
    {
        // 2 hari pada time, tapi precipitation_probability_max hanya 1 elemen
        var json = """
        {
          "daily": {
            "time": ["2026-11-20", "2026-11-21"],
            "precipitation_probability_max": [40],
            "precipitation_sum": [0.0, 0.0],
            "temperature_2m_min": [24.0, 24.0],
            "temperature_2m_max": [32.0, 32.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00", "2026-11-21T08:00"],
            "relative_humidity_2m": [70.0, 70.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Equal(2, hasil.Hari.Count);
        Assert.True(hasil.Hari[0].DataLengkap);
        Assert.False(hasil.Hari[1].DataLengkap);
        Assert.Contains("tidak seimbang", hasil.Hari[1].CatatanKualitas);
        Assert.True(hasil.DataParsial);
    }

    [Fact]
    public void T27_SuhuMinLebihBesarDariMaks_DitandaiDataTidakLengkap()
    {
        var json = """
        {
          "daily": {
            "time": ["2026-11-20"],
            "precipitation_probability_max": [20],
            "precipitation_sum": [0.0],
            "temperature_2m_min": [35.0],
            "temperature_2m_max": [25.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00"],
            "relative_humidity_2m": [70.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Single(hasil.Hari);
        var hari = hasil.Hari[0];
        Assert.False(hari.DataLengkap);
        Assert.Contains("lebih tinggi dari suhu maksimum", hari.CatatanKualitas);
    }

    [Fact]
    public void T27_HujanNegatifDanKelembapanDiLuarRentang_DitandaiDataTidakLengkap()
    {
        var json = """
        {
          "daily": {
            "time": ["2026-11-20"],
            "precipitation_probability_max": [120],
            "precipitation_sum": [-2.0],
            "temperature_2m_min": [24.0],
            "temperature_2m_max": [32.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00"],
            "relative_humidity_2m": [150.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Single(hasil.Hari);
        var hari = hasil.Hari[0];
        Assert.False(hari.DataLengkap);
        Assert.Contains("negatif", hari.CatatanKualitas);
        Assert.Contains("100%", hari.CatatanKualitas);
    }

    [Fact]
    public void T27_DuplikasiTanggal_DitandaiDataTidakLengkap()
    {
        var json = """
        {
          "daily": {
            "time": ["2026-11-20", "2026-11-20"],
            "precipitation_probability_max": [20, 20],
            "precipitation_sum": [0.0, 0.0],
            "temperature_2m_min": [24.0, 24.0],
            "temperature_2m_max": [32.0, 32.0]
          },
          "hourly": {
            "time": ["2026-11-20T08:00"],
            "relative_humidity_2m": [70.0]
          }
        }
        """;

        var hasil = ParserOpenMeteo.ParsePrakiraan(json);

        Assert.Equal(2, hasil.Hari.Count);
        Assert.True(hasil.Hari[0].DataLengkap);
        Assert.False(hasil.Hari[1].DataLengkap);
        Assert.Contains("Duplikasi tanggal", hasil.Hari[1].CatatanKualitas);
    }

    [Fact]
    public void ParseHistoris_BerhasilMembuatProfilIklimDenganMetadataLengkap()
    {
        var json = """
        {
          "latitude": -5.4,
          "longitude": 105.2,
          "timezone": "Asia/Jakarta",
          "daily": {
            "time": ["2016-01-01", "2025-12-31"],
            "precipitation_sum": [25000.0, 0.0],
            "temperature_2m_mean": [28.0, 26.0]
          },
          "hourly": {
            "time": ["2016-01-01T00:00", "2016-01-01T01:00"],
            "relative_humidity_2m": [80.0, 84.0]
          }
        }
        """;

        var sekarangUtc = new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
        var profil = ParserOpenMeteo.ParseHistoris(json, jarakPesisirKm: 5.0, sekarangUtc, revisiLokasi: 2);

        // 25000 / 10 = 2500 mm/tahun
        Assert.Equal(2500.0, profil.CurahHujanTahunanMm);
        // (80 + 84) / 2 = 82%
        Assert.Equal(82.0, profil.KelembapanRataPct);
        // (28 + 26) / 2 = 27°C
        Assert.Equal(27.0, profil.SuhuRataC);
        Assert.Equal(5.0, profil.JarakPesisirKm);
        Assert.Equal(TingkatKorosi.Tinggi, profil.TingkatKorosi); // < 10 km = Tinggi
        Assert.Equal(2, profil.RevisiLokasi);
        Assert.Equal(ProvenanceIklim.ApiHistoris, profil.Provenance);

        Assert.NotNull(profil.Metadata);
        Assert.Equal(new DateOnly(2016, 1, 1), profil.Metadata!.PeriodeAwal);
        Assert.Equal(new DateOnly(2025, 12, 31), profil.Metadata!.PeriodeAkhir);
        Assert.Equal("Open-Meteo", profil.Metadata!.Penyedia);
        Assert.Equal("Asia/Jakarta", profil.Metadata!.ZonaWaktu);
        Assert.Equal(sekarangUtc, profil.Metadata!.DiambilPadaUtc);
        Assert.Equal(ProvenanceIklim.ApiHistoris, profil.Metadata!.Provenance);
        Assert.Equal(StatusPengambilanIklim.Tersedia, profil.Metadata!.StatusPengambilan);
    }
}
