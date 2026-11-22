using System.Globalization;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Models;

public class PrakiraanCuaca
{
    private DateTime _tanggal;
    private double _probabilitasHujan;
    private double _curahHujan;
    private double _suhuMin;
    private double _suhuMaks;
    private double _kelembapan;
    private bool _dataLengkap = true;
    private string? _catatanKualitas;

    public DateTime Tanggal { get => _tanggal; set => _tanggal = value; }
    public double ProbabilitasHujan { get => _probabilitasHujan; set => _probabilitasHujan = value; }
    public double CurahHujan { get => _curahHujan; set => _curahHujan = value; }
    public double SuhuMin { get => _suhuMin; set => _suhuMin = value; }
    public double SuhuMaks { get => _suhuMaks; set => _suhuMaks = value; }
    public double Kelembapan { get => _kelembapan; set => _kelembapan = value; }
    public bool DataLengkap { get => _dataLengkap; set => _dataLengkap = value; }
    public string? CatatanKualitas { get => _catatanKualitas; set => _catatanKualitas = value; }

    public bool LayakKerja() => LayakKerja(JenisPekerjaan.Pengecoran);

    public bool LayakKerja(JenisPekerjaan jenis) =>
        EvaluasiKelayakan(jenis).Status != StatusKelayakan.TidakLayak;

    public bool LayakKerja(JenisPekerjaan jenis, AmbangPekerjaan? overrideAmbang) =>
        EvaluasiKelayakan(jenis, overrideAmbang).Status != StatusKelayakan.TidakLayak;

    public KelayakanCuaca EvaluasiKelayakan(JenisPekerjaan jenis) => EvaluasiKelayakan(jenis, null);

    /// <summary>
    /// Mendelegasikan evaluasi kelayakan cuaca langsung ke domain <see cref="EvaluatorCuaca"/> (PRD §11.3, §12).
    /// </summary>
    public KelayakanCuaca EvaluasiKelayakan(JenisPekerjaan jenis, AmbangPekerjaan? overrideAmbang)
    {
        var domainHasil = EvaluasiDomain(jenis, overrideAmbang);

        var status = domainHasil.Status switch
        {
            StatusKelayakanCuaca.Layak => StatusKelayakan.Layak,
            StatusKelayakanCuaca.Waspada => StatusKelayakan.Waspada,
            _ => StatusKelayakan.TidakLayak
        };

        var alasan = domainHasil.Alasan.Count > 0 ? string.Join("; ", domainHasil.Alasan) : "Memenuhi ambang kelayakan cuaca";
        return new KelayakanCuaca(status, alasan);
    }

    /// <summary>
    /// Mendapatkan hasil evaluasi domain terstruktur lengkap.
    /// </summary>
    public HasilEvaluasiCuaca EvaluasiDomain(JenisPekerjaan jenis, AmbangPekerjaan? overrideAmbang = null)
    {
        var domainJenis = jenis == JenisPekerjaan.Pengecatan
            ? Domain.Weather.JenisPekerjaan.Pengecatan
            : Domain.Weather.JenisPekerjaan.Pengecoran;

        var ambang = overrideAmbang ?? AmbangLayakKerja.Untuk(domainJenis);
        var tgl = DateOnly.FromDateTime(Tanggal);

        var daily = new DailyForecast(
            Tanggal: tgl,
            ProbabilitasHujanPct: ProbabilitasHujan,
            CurahHujanMm: CurahHujan,
            SuhuMinC: SuhuMin,
            SuhuMaksC: SuhuMaks,
            KelembapanPct: Kelembapan,
            DataLengkap: DataLengkap,
            CatatanKualitas: CatatanKualitas);

        return EvaluatorCuaca.EvaluasiHari(tgl, daily, domainJenis, ambang);
    }
}

public readonly record struct KelayakanCuaca(StatusKelayakan Status, string Alasan);
