using System.Globalization;
using System.Text.Json;
using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Weather;

/// <summary>
/// Parser murni JSON respons Open-Meteo tanpa ketergantungan jaringan (PRD §8.3, §PRD-R08, T26, T27).
/// </summary>
public static class ParserOpenMeteo
{
    /// <summary>
    /// Mem-parsing JSON prakiraan cuaca (daily + hourly) menjadi <see cref="HasilPrakiraan"/>.
    /// Menggunakan RH maksimum harian (bukan rata-rata, T26) dan mendeteksi kualitas data (T27).
    /// </summary>
    public static HasilPrakiraan ParsePrakiraan(
        string json,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var doc = JsonDocument.Parse(json);
        return ParsePrakiraan(doc.RootElement, diambilPadaUtc, revisiLokasi);
    }

    /// <summary>
    /// Mem-parsing memory byte JSON prakiraan cuaca menjadi <see cref="HasilPrakiraan"/>.
    /// </summary>
    public static HasilPrakiraan ParsePrakiraan(
        ReadOnlyMemory<byte> utf8Json,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        using var doc = JsonDocument.Parse(utf8Json);
        return ParsePrakiraan(doc.RootElement, diambilPadaUtc, revisiLokasi);
    }

    /// <summary>
    /// Mem-parsing elemen root JSON prakiraan cuaca menjadi <see cref="HasilPrakiraan"/>.
    /// </summary>
    public static HasilPrakiraan ParsePrakiraan(
        JsonElement root,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        string zonaWaktu = "Asia/Jakarta";
        if (root.TryGetProperty("timezone", out var tzProp) && tzProp.ValueKind == JsonValueKind.String)
        {
            var tzStr = tzProp.GetString();
            if (!string.IsNullOrWhiteSpace(tzStr))
            {
                zonaWaktu = tzStr;
            }
        }

        if (!root.TryGetProperty("daily", out var daily))
        {
            throw new InvalidDataException("Objek 'daily' tidak ditemukan dalam respons prakiraan.");
        }

        // Agregasi hourly RH: kumpulkan per tanggal dan cari MAX (T26) serta validasi rentang (T27)
        var hourlyRhPerTanggal = new Dictionary<string, (List<double> Nilai, bool AdaNilaiInvalid, string? Catatan)>(StringComparer.Ordinal);
        if (root.TryGetProperty("hourly", out var hourly)
            && hourly.TryGetProperty("time", out var hourlyTime)
            && hourly.TryGetProperty("relative_humidity_2m", out var hourlyRh))
        {
            int nHourly = hourlyTime.GetArrayLength();
            int nRh = hourlyRh.GetArrayLength();
            int limit = Math.Max(nHourly, nRh);

            for (int i = 0; i < limit; i++)
            {
                if (i >= nHourly) break;
                string? tStr = hourlyTime[i].GetString();
                if (string.IsNullOrWhiteSpace(tStr) || tStr.Length < 10) continue;
                string tglKey = tStr[..10];

                if (!hourlyRhPerTanggal.TryGetValue(tglKey, out var entry))
                {
                    entry = (new List<double>(), false, null);
                    hourlyRhPerTanggal[tglKey] = entry;
                }

                if (i >= nRh)
                {
                    hourlyRhPerTanggal[tglKey] = (entry.Nilai, true, "Array kelembapan per jam tidak seimbang");
                    continue;
                }

                var rhVal = hourlyRh[i];
                if (rhVal.ValueKind != JsonValueKind.Number)
                {
                    hourlyRhPerTanggal[tglKey] = (entry.Nilai, true, "Kelembapan per jam bernilai null atau bukan angka");
                }
                else
                {
                    double v = rhVal.GetDouble();
                    if (!double.IsFinite(v) || v < 0 || v > 100)
                    {
                        hourlyRhPerTanggal[tglKey] = (entry.Nilai, true, $"Kelembapan per jam ({v}%) di luar rentang wajar 0-100%");
                    }
                    else
                    {
                        entry.Nilai.Add(v);
                    }
                }
            }
        }

        // Ekstraksi array daily
        if (!daily.TryGetProperty("time", out var timeArray))
        {
            throw new InvalidDataException("Properti daily 'time' tidak ditemukan dalam respons prakiraan.");
        }

        daily.TryGetProperty("precipitation_probability_max", out var probArray);
        daily.TryGetProperty("precipitation_sum", out var precipArray);
        daily.TryGetProperty("temperature_2m_min", out var minTempArray);
        daily.TryGetProperty("temperature_2m_max", out var maxTempArray);

        int count = timeArray.GetArrayLength();
        int probCount = probArray.ValueKind == JsonValueKind.Array ? probArray.GetArrayLength() : 0;
        int precipCount = precipArray.ValueKind == JsonValueKind.Array ? precipArray.GetArrayLength() : 0;
        int minTempCount = minTempArray.ValueKind == JsonValueKind.Array ? minTempArray.GetArrayLength() : 0;
        int maxTempCount = maxTempArray.ValueKind == JsonValueKind.Array ? maxTempArray.GetArrayLength() : 0;

        var daftarHari = new List<DailyForecast>(count);
        var seenDates = new HashSet<DateOnly>();
        bool adaDataParsial = false;

        for (int i = 0; i < count; i++)
        {
            var catatanCacat = new List<string>(2);
            string? dateStr = timeArray[i].GetString();

            if (!DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tanggal))
            {
                catatanCacat.Add($"Format tanggal tidak valid: '{dateStr}'");
                tanggal = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i));
            }
            else if (!seenDates.Add(tanggal))
            {
                catatanCacat.Add($"Duplikasi tanggal prakiraan: {tanggal}");
            }

            // Validasi probabilitas hujan
            double prob = 0;
            if (i >= probCount || probArray[i].ValueKind != JsonValueKind.Number)
            {
                catatanCacat.Add(i >= probCount ? "Array probabilitas hujan tidak seimbang" : "Probabilitas hujan bernilai null");
            }
            else
            {
                prob = probArray[i].GetDouble();
                if (!double.IsFinite(prob) || prob < 0 || prob > 100)
                {
                    catatanCacat.Add($"Probabilitas hujan ({prob}%) di luar rentang wajar 0-100%");
                }
            }

            // Validasi curah hujan
            double curah = 0;
            if (i >= precipCount || precipArray[i].ValueKind != JsonValueKind.Number)
            {
                catatanCacat.Add(i >= precipCount ? "Array curah hujan tidak seimbang" : "Curah hujan bernilai null");
            }
            else
            {
                curah = precipArray[i].GetDouble();
                if (!double.IsFinite(curah) || curah < 0)
                {
                    catatanCacat.Add($"Curah hujan ({curah} mm) bernilai negatif atau tidak finite");
                }
            }

            // Validasi suhu udara
            double tMin = 0;
            double tMaks = 0;
            bool suhuMinValid = false;
            bool suhuMaksValid = false;

            if (i >= minTempCount || minTempArray[i].ValueKind != JsonValueKind.Number)
            {
                catatanCacat.Add(i >= minTempCount ? "Array suhu minimum tidak seimbang" : "Suhu minimum bernilai null");
            }
            else
            {
                tMin = minTempArray[i].GetDouble();
                if (!double.IsFinite(tMin))
                {
                    catatanCacat.Add($"Suhu minimum ({tMin}) bukan angka finite");
                }
                else
                {
                    suhuMinValid = true;
                }
            }

            if (i >= maxTempCount || maxTempArray[i].ValueKind != JsonValueKind.Number)
            {
                catatanCacat.Add(i >= maxTempCount ? "Array suhu maksimum tidak seimbang" : "Suhu maksimum bernilai null");
            }
            else
            {
                tMaks = maxTempArray[i].GetDouble();
                if (!double.IsFinite(tMaks))
                {
                    catatanCacat.Add($"Suhu maksimum ({tMaks}) bukan angka finite");
                }
                else
                {
                    suhuMaksValid = true;
                }
            }

            if (suhuMinValid && suhuMaksValid && tMin > tMaks)
            {
                catatanCacat.Add($"Suhu minimum ({tMin:0.#}°C) lebih tinggi dari suhu maksimum ({tMaks:0.#}°C)");
            }

            // Hitung MAX RH dari data jam lokal (T26)
            double kelembapanMaks = 0;
            if (dateStr is not null && hourlyRhPerTanggal.TryGetValue(dateStr, out var rhInfo))
            {
                if (rhInfo.AdaNilaiInvalid && rhInfo.Catatan is not null)
                {
                    catatanCacat.Add(rhInfo.Catatan);
                }

                if (rhInfo.Nilai.Count > 0)
                {
                    kelembapanMaks = rhInfo.Nilai.Max();
                }
                else
                {
                    catatanCacat.Add("Data kelembapan per jam kosong");
                }
            }
            else
            {
                catatanCacat.Add("Data kelembapan per jam tidak ditemukan untuk tanggal ini");
            }

            bool dataLengkap = catatanCacat.Count == 0;
            if (!dataLengkap)
            {
                adaDataParsial = true;
            }

            string? catatanKualitas = dataLengkap ? null : string.Join("; ", catatanCacat);

            daftarHari.Add(new DailyForecast(
                Tanggal: tanggal,
                ProbabilitasHujanPct: prob,
                CurahHujanMm: curah,
                SuhuMinC: tMin,
                SuhuMaksC: tMaks,
                KelembapanPct: kelembapanMaks,
                DataLengkap: dataLengkap,
                CatatanKualitas: catatanKualitas));
        }

        return new HasilPrakiraan(
            Hari: daftarHari,
            ZonaWaktu: zonaWaktu,
            DiambilPadaUtc: diambilPadaUtc ?? DateTimeOffset.UtcNow,
            Penyedia: "Open-Meteo",
            RevisiLokasi: revisiLokasi,
            DataParsial: adaDataParsial);
    }

    /// <summary>
    /// Mem-parsing JSON riwayat arsip iklim 10 tahun menjadi <see cref="ProfilIklim"/> ber-metadata lengkap (PRD §8.3, PRD-R09).
    /// </summary>
    public static ProfilIklim ParseHistoris(
        string json,
        double jarakPesisirKm = 0,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var doc = JsonDocument.Parse(json);
        return ParseHistoris(doc.RootElement, jarakPesisirKm, diambilPadaUtc, revisiLokasi);
    }

    /// <summary>
    /// Mem-parsing memory byte JSON arsip iklim 10 tahun menjadi <see cref="ProfilIklim"/>.
    /// </summary>
    public static ProfilIklim ParseHistoris(
        ReadOnlyMemory<byte> utf8Json,
        double jarakPesisirKm = 0,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        using var doc = JsonDocument.Parse(utf8Json);
        return ParseHistoris(doc.RootElement, jarakPesisirKm, diambilPadaUtc, revisiLokasi);
    }

    /// <summary>
    /// Mem-parsing elemen root JSON riwayat arsip iklim 10 tahun menjadi <see cref="ProfilIklim"/>.
    /// </summary>
    public static ProfilIklim ParseHistoris(
        JsonElement root,
        double jarakPesisirKm = 0,
        DateTimeOffset? diambilPadaUtc = null,
        int revisiLokasi = 1)
    {
        string zonaWaktu = "Asia/Jakarta";
        if (root.TryGetProperty("timezone", out var tzProp) && tzProp.ValueKind == JsonValueKind.String)
        {
            var tzStr = tzProp.GetString();
            if (!string.IsNullOrWhiteSpace(tzStr))
            {
                zonaWaktu = tzStr;
            }
        }

        DateOnly? periodeAwal = null;
        DateOnly? periodeAkhir = null;

        double totalHujan = 0;
        int nHujan = 0;
        double totalSuhu = 0;
        int nSuhu = 0;

        if (root.TryGetProperty("daily", out var daily))
        {
            if (daily.TryGetProperty("time", out var timeArray) && timeArray.ValueKind == JsonValueKind.Array)
            {
                int len = timeArray.GetArrayLength();
                if (len > 0)
                {
                    if (DateOnly.TryParse(timeArray[0].GetString(), CultureInfo.InvariantCulture, out var awal))
                        periodeAwal = awal;
                    if (DateOnly.TryParse(timeArray[len - 1].GetString(), CultureInfo.InvariantCulture, out var akhir))
                        periodeAkhir = akhir;
                }
            }

            if (daily.TryGetProperty("precipitation_sum", out var precipArray) && precipArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in precipArray.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Number)
                    {
                        totalHujan += el.GetDouble();
                        nHujan++;
                    }
                }
            }

            if (daily.TryGetProperty("temperature_2m_mean", out var tempArray) && tempArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in tempArray.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Number)
                    {
                        totalSuhu += el.GetDouble();
                        nSuhu++;
                    }
                }
            }
        }

        double totalHourlyRh = 0;
        int nHourlyRh = 0;
        if (root.TryGetProperty("hourly", out var hourly)
            && hourly.TryGetProperty("relative_humidity_2m", out var rhArray)
            && rhArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in rhArray.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.Number)
                {
                    totalHourlyRh += el.GetDouble();
                    nHourlyRh++;
                }
            }
        }

        if (nHujan == 0 || nSuhu == 0 || nHourlyRh == 0)
        {
            throw new InvalidDataException("Data iklim historis tidak lengkap.");
        }

        // Agregasi tahunan 10 tahun
        double curahHujanTahunan = totalHujan / 10.0;
        double kelembapanRata = totalHourlyRh / nHourlyRh;
        double suhuRata = totalSuhu / nSuhu;

        var korosi = jarakPesisirKm < 10 ? TingkatKorosi.Tinggi
            : jarakPesisirKm < 35 ? TingkatKorosi.Sedang
            : TingkatKorosi.Rendah;

        var metadata = new MetadataIklim(
            PeriodeAwal: periodeAwal,
            PeriodeAkhir: periodeAkhir,
            Penyedia: "Open-Meteo",
            Model: null,
            DiambilPadaUtc: diambilPadaUtc ?? DateTimeOffset.UtcNow,
            ZonaWaktu: zonaWaktu,
            MetodePesisir: "Haversine Proximity",
            Provenance: ProvenanceIklim.ApiHistoris,
            StatusPengambilan: StatusPengambilanIklim.Tersedia);

        return new ProfilIklim(
            curahHujanTahunanMm: curahHujanTahunan,
            kelembapanRataPct: kelembapanRata,
            suhuRataC: suhuRata,
            jarakPesisirKm: jarakPesisirKm,
            tingkatKorosi: korosi,
            metadata: metadata,
            revisiLokasi: revisiLokasi,
            provenance: ProvenanceIklim.ApiHistoris);
    }
}
