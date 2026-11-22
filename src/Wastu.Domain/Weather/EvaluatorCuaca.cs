using System.Globalization;
using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Weather;

/// <summary>
/// Evaluator tunggal kelayakan cuaca untuk pekerjaan lapangan (PRD §12, PRD-R08, PRD-R10, T24–T28).
/// Sumber kebenaran tunggal: tidak ada formula evaluasi kedua di lapisan lain.
/// </summary>
public static class EvaluatorCuaca
{
    /// <summary>
    /// Menentukan ambang efektif menurut hierarki:
    /// Override Pekerjaan -> Profil Spesifikasi Produk/Repository -> Default Asumsi Generik (PRD §12.1, T28).
    /// </summary>
    public static AmbangPekerjaan TentukanAmbangEfektif(
        Pekerjaan pekerjaan,
        AmbangPekerjaan? ambangDariRepo = null)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);
        return pekerjaan.OverrideAmbang ?? ambangDariRepo ?? AmbangLayakKerja.Untuk(pekerjaan.Jenis);
    }

    /// <summary>
    /// Mengevaluasi satu hari kerja terhadap data prakiraan cuaca dan ambang efektif (PRD §12, T24–T28).
    /// </summary>
    public static HasilEvaluasiCuaca EvaluasiHari(
        DateOnly tanggal,
        DailyForecast? cuaca,
        JenisPekerjaan jenis,
        AmbangPekerjaan ambangEfektif,
        bool dataLama = false,
        TimeSpan? umurCache = null)
    {
        ArgumentNullException.ThrowIfNull(ambangEfektif);

        // Kasus data tidak tersedia atau cacat kualitas (F6-05, T27, T29)
        if (cuaca is null)
        {
            return new HasilEvaluasiCuaca(
                Tanggal: tanggal,
                Status: StatusKelayakanCuaca.DataTidakTersedia,
                Alasan: ["Prakiraan cuaca belum tersedia untuk tanggal ini (di luar rentang 14 hari)."],
                DataLama: dataLama,
                UmurCache: umurCache,
                AmbangEfektif: ambangEfektif);
        }

        if (!cuaca.DataLengkap)
        {
            return new HasilEvaluasiCuaca(
                Tanggal: tanggal,
                Status: StatusKelayakanCuaca.DataTidakTersedia,
                Alasan: [$"Data cuaca tidak lengkap atau tidak valid: {cuaca.CatatanKualitas ?? "Data tidak lengkap"}"],
                DataLama: dataLama,
                UmurCache: umurCache,
                AmbangEfektif: ambangEfektif);
        }

        var pelanggaranKeras = new List<string>(4);
        var peringatan = new List<string>(2);

        // 1. Probabilitas hujan (persen 0-100)
        if (ambangEfektif.ProbabilitasHujanMaksPct.HasValue
            && cuaca.ProbabilitasHujanPct > ambangEfektif.ProbabilitasHujanMaksPct.Value)
        {
            pelanggaranKeras.Add(
                $"Peluang hujan {Format(cuaca.ProbabilitasHujanPct)}% melebihi ambang batas {Format(ambangEfektif.ProbabilitasHujanMaksPct.Value)}%");
        }

        // 2. Curah hujan (mm)
        if (ambangEfektif.CurahHujanMaksMm.HasValue
            && cuaca.CurahHujanMm > ambangEfektif.CurahHujanMaksMm.Value)
        {
            pelanggaranKeras.Add(
                $"Curah hujan {Format(cuaca.CurahHujanMm)} mm melebihi ambang batas {Format(ambangEfektif.CurahHujanMaksMm.Value)} mm");
        }

        // 3. Kelembapan relatif maksimum harian (persen 0-100) (T26)
        if (ambangEfektif.KelembapanMaksPct.HasValue
            && cuaca.KelembapanPct > ambangEfektif.KelembapanMaksPct.Value)
        {
            pelanggaranKeras.Add(
                $"Kelembapan relatif maks harian {Format(cuaca.KelembapanPct)}% melebihi ambang batas {Format(ambangEfektif.KelembapanMaksPct.Value)}%");
        }

        // 4. Suhu minimum (°C)
        if (ambangEfektif.SuhuMinC.HasValue
            && cuaca.SuhuMinC < ambangEfektif.SuhuMinC.Value)
        {
            pelanggaranKeras.Add(
                $"Suhu minimum {Format(cuaca.SuhuMinC)}°C di bawah ambang batas {Format(ambangEfektif.SuhuMinC.Value)}°C");
        }

        // 5. Suhu maksimum (°C) (T24, T25)
        if (ambangEfektif.SuhuMaksC.HasValue
            && cuaca.SuhuMaksC > ambangEfektif.SuhuMaksC.Value)
        {
            if (ambangEfektif.TingkatSuhu == TingkatPelanggaran.Peringatan)
            {
                // PRD §12: suhu > 35°C pada pengecoran hanya peringatan (Waspada), butuh mitigasi penguapan / retak susut
                peringatan.Add(
                    $"Suhu maksimum {Format(cuaca.SuhuMaksC)}°C melebihi {Format(ambangEfektif.SuhuMaksC.Value)}°C; butuh mitigasi retak susut / hidrasi cepat");
            }
            else
            {
                pelanggaranKeras.Add(
                    $"Suhu maksimum {Format(cuaca.SuhuMaksC)}°C melebihi ambang batas {Format(ambangEfektif.SuhuMaksC.Value)}°C");
            }
        }

        // Menentukan status utama dan menggabungkan semua alasan (PRD §12.1)
        StatusKelayakanCuaca status;
        var semuaAlasan = new List<string>(pelanggaranKeras.Count + peringatan.Count);

        if (pelanggaranKeras.Count > 0)
        {
            status = StatusKelayakanCuaca.TidakLayak;
            semuaAlasan.AddRange(pelanggaranKeras);
            semuaAlasan.AddRange(peringatan); // Tetap tampilkan peringatan bila ada pelanggaran keras
        }
        else if (peringatan.Count > 0)
        {
            status = StatusKelayakanCuaca.Waspada;
            semuaAlasan.AddRange(peringatan);
        }
        else
        {
            status = StatusKelayakanCuaca.Layak;
            semuaAlasan.Add("Memenuhi seluruh ambang kelayakan cuaca");
        }

        return new HasilEvaluasiCuaca(
            Tanggal: tanggal,
            Status: status,
            Alasan: semuaAlasan,
            DataLama: dataLama,
            UmurCache: umurCache,
            AmbangEfektif: ambangEfektif);
    }

    private static string Format(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
