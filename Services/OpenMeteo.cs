using System.Globalization;
using System.IO;
using System.Net.Http;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;
using Wastu.Models;

namespace Wastu.Services;

public sealed class OpenMeteo : IHistoricalClimateClient, IDailyForecastClient
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    { Timeout = Timeout.InfiniteTimeSpan };

    private readonly HttpClient _http;
    private readonly TimeSpan _historicalTimeout;
    private readonly TimeSpan _forecastTimeout;

    public OpenMeteo(HttpClient? http = null, TimeSpan? historicalTimeout = null, TimeSpan? forecastTimeout = null)
    {
        _http = http ?? Http;
        _historicalTimeout = historicalTimeout ?? TimeSpan.FromSeconds(60);
        _forecastTimeout = forecastTimeout ?? TimeSpan.FromSeconds(12);
    }

    private static string Num(double v) => v.ToString(CultureInfo.InvariantCulture);

    public async Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
    {
        var akhir = new DateTime(DateTime.UtcNow.AddDays(-7).Year, 1, 1).AddDays(-1);
        var mulai = new DateTime(akhir.Year - 9, 1, 1);
        var url = $"https://archive-api.open-meteo.com/v1/archive?latitude={Num(lat)}&longitude={Num(lon)}" +
                  $"&start_date={Format.Iso(mulai)}&end_date={Format.Iso(akhir)}" +
                  "&daily=precipitation_sum,temperature_2m_mean&hourly=relative_humidity_2m&timezone=auto";

        byte[] bytes;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_historicalTimeout);
        try
        {
            bytes = await _http.GetByteArrayAsync(url, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Open-Meteo belum menjawab permintaan profil iklim.", ex);
        }

        double pesisir = JarakPesisir.HitungKm(lat, lon);
        return ParserOpenMeteo.ParseHistoris(bytes, pesisir, DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<DailyForecast>> GetForecastAsync(
        double lat, double lon, int days, CancellationToken ct = default)
    {
        var hasil = await GetForecastWithMetadataAsync(lat, lon, days, ct).ConfigureAwait(false);
        return hasil.Hari;
    }

    public async Task<HasilPrakiraan> GetForecastWithMetadataAsync(
        double lat, double lon, int days, CancellationToken ct = default)
    {
        int hariAmbil = Math.Clamp(days, 1, 16);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={Num(lat)}&longitude={Num(lon)}" +
                  "&daily=precipitation_probability_max,precipitation_sum,temperature_2m_min,temperature_2m_max" +
                  $"&hourly=relative_humidity_2m&forecast_days={hariAmbil}&timezone=auto";

        byte[] bytes;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_forecastTimeout);
        try
        {
            bytes = await _http.GetByteArrayAsync(url, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Open-Meteo belum menjawab permintaan prakiraan cuaca.", ex);
        }

        return ParserOpenMeteo.ParsePrakiraan(bytes, DateTimeOffset.UtcNow);
    }

    public static async Task<List<PrakiraanCuaca>> AmbilPrakiraanAsync(double lintang, double bujur)
    {
        var client = new OpenMeteo();
        var hasil = await client.GetForecastWithMetadataAsync(lintang, bujur, 14).ConfigureAwait(false);

        var list = new List<PrakiraanCuaca>(hasil.Hari.Count);
        foreach (var h in hasil.Hari)
        {
            list.Add(new PrakiraanCuaca
            {
                Tanggal = h.Tanggal.ToDateTime(TimeOnly.MinValue),
                ProbabilitasHujan = h.ProbabilitasHujanPct,
                CurahHujan = h.CurahHujanMm,
                SuhuMin = h.SuhuMinC,
                SuhuMaks = h.SuhuMaksC,
                Kelembapan = h.KelembapanPct,
                DataLengkap = h.DataLengkap,
                CatatanKualitas = h.CatatanKualitas
            });
        }

        if (list.Count == 0)
        {
            throw new InvalidDataException("Data prakiraan belum lengkap");
        }

        return list;
    }
}
