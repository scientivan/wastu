using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Wastu.Domain.Abstractions;
using Wastu.Models;

namespace Wastu.Services;

public sealed class OpenMeteo : IHistoricalClimateClient
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    { Timeout = TimeSpan.FromSeconds(12) };

    private static string Num(double v) => v.ToString(CultureInfo.InvariantCulture);

    public async Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
    {
        var akhir = new DateTime(DateTime.UtcNow.AddDays(-7).Year, 1, 1).AddDays(-1);
        var mulai = new DateTime(akhir.Year - 9, 1, 1);
        var url = $"https://archive-api.open-meteo.com/v1/archive?latitude={Num(lat)}&longitude={Num(lon)}" +
                  $"&start_date={Format.Iso(mulai)}&end_date={Format.Iso(akhir)}" +
                  "&daily=precipitation_sum,temperature_2m_mean&hourly=relative_humidity_2m&timezone=auto";
        byte[] bytes;
        try
        {
            bytes = await Http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Open-Meteo belum menjawab permintaan profil iklim.", ex);
        }

        // Arsip 10 tahun berisi ~90 ribu nilai per jam; dibaca streaming tanpa membangun DOM.
        return await Task.Run(() =>
        {
            var reader = new Utf8JsonReader(bytes);
            string? induk = null, properti = null;
            int kedalaman = 0;
            double hujan = 0, suhu = 0, lembap = 0;
            int nHujan = 0, nSuhu = 0, nLembap = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject: kedalaman++; break;
                    case JsonTokenType.EndObject: if (--kedalaman == 1) induk = null; break;
                    case JsonTokenType.PropertyName:
                        if (kedalaman == 1) induk = reader.GetString();
                        else properti = reader.GetString();
                        break;
                    case JsonTokenType.Number when kedalaman == 2:
                        var v = reader.GetDouble();
                        if (induk == "daily" && properti == "precipitation_sum") { hujan += v; nHujan++; }
                        else if (induk == "daily" && properti == "temperature_2m_mean") { suhu += v; nSuhu++; }
                        else if (induk == "hourly" && properti == "relative_humidity_2m") { lembap += v; nLembap++; }
                        break;
                }
            }
            var hari = (akhir - mulai).Days + 1;
            if (nHujan != hari || nSuhu != hari || nLembap != hari * 24)
                throw new InvalidDataException("Data iklim belum lengkap.");

            double pesisir = JarakPesisir.HitungKm(lat, lon);
            var korosi = pesisir < 10 ? Domain.TingkatKorosi.Tinggi
                : pesisir < 35 ? Domain.TingkatKorosi.Sedang
                : Domain.TingkatKorosi.Rendah;
            return new Domain.ProfilIklim(hujan / 10, lembap / nLembap, suhu / nSuhu, pesisir, korosi)
            {
                DiambilPada = DateTimeOffset.UtcNow
            };
        }, ct).ConfigureAwait(false);
    }

    public static async Task<List<PrakiraanCuaca>> AmbilPrakiraanAsync(double lintang, double bujur)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={Num(lintang)}&longitude={Num(bujur)}" +
                  "&daily=precipitation_probability_max,precipitation_sum,temperature_2m_min,temperature_2m_max" +
                  "&hourly=relative_humidity_2m&forecast_days=14&timezone=auto";
        await using var stream = await Http.GetStreamAsync(url).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        var daily = doc.RootElement.GetProperty("daily");
        var hourly = doc.RootElement.GetProperty("hourly");

        var jam = hourly.GetProperty("time");
        var kelembapanJam = hourly.GetProperty("relative_humidity_2m");
        var perTanggal = new Dictionary<string, (double Total, int N)>();
        for (var i = 0; i < jam.GetArrayLength(); i++)
        {
            var nilai = kelembapanJam[i];
            if (nilai.ValueKind != JsonValueKind.Number) continue;
            var tanggal = jam[i].GetString()![..10];
            perTanggal.TryGetValue(tanggal, out var akumulasi);
            perTanggal[tanggal] = (akumulasi.Total + nilai.GetDouble(), akumulasi.N + 1);
        }

        static double? Ambil(JsonElement larik, int i) =>
            larik[i].ValueKind == JsonValueKind.Number ? larik[i].GetDouble() : null;

        var waktu = daily.GetProperty("time");
        var peluang = daily.GetProperty("precipitation_probability_max");
        var curah = daily.GetProperty("precipitation_sum");
        var min = daily.GetProperty("temperature_2m_min");
        var maks = daily.GetProperty("temperature_2m_max");
        var hasil = new List<PrakiraanCuaca>(14);
        for (var i = 0; i < waktu.GetArrayLength(); i++)
        {
            var tanggal = waktu[i].GetString()!;
            if (Ambil(peluang, i) is not { } p || Ambil(curah, i) is not { } c ||
                Ambil(min, i) is not { } tmin || Ambil(maks, i) is not { } tmaks ||
                !perTanggal.TryGetValue(tanggal, out var h))
                continue;
            hasil.Add(new PrakiraanCuaca
            {
                Tanggal = DateTime.ParseExact(tanggal, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ProbabilitasHujan = p,
                CurahHujan = c,
                SuhuMin = tmin,
                SuhuMaks = tmaks,
                Kelembapan = h.Total / h.N
            });
        }
        if (hasil.Count == 0) throw new InvalidDataException("Data prakiraan belum lengkap");
        return hasil;
    }
}
