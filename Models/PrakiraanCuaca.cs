using System.Globalization;

namespace Wastu.Models;

public class PrakiraanCuaca
{
    private DateTime _tanggal;
    private double _probabilitasHujan;
    private double _curahHujan;
    private double _suhuMin;
    private double _suhuMaks;
    private double _kelembapan;

    public DateTime Tanggal { get => _tanggal; set => _tanggal = value; }
    public double ProbabilitasHujan { get => _probabilitasHujan; set => _probabilitasHujan = value; }
    public double CurahHujan { get => _curahHujan; set => _curahHujan = value; }
    public double SuhuMin { get => _suhuMin; set => _suhuMin = value; }
    public double SuhuMaks { get => _suhuMaks; set => _suhuMaks = value; }
    public double Kelembapan { get => _kelembapan; set => _kelembapan = value; }

    public bool LayakKerja() => LayakKerja(JenisPekerjaan.Pengecoran);

    public bool LayakKerja(JenisPekerjaan jenis) =>
        EvaluasiKelayakan(jenis).Status != StatusKelayakan.TidakLayak;

    // Ambang layak kerja per jenis pekerjaan (PRD §12). Probabilitas hujan dalam persen.
    public KelayakanCuaca EvaluasiKelayakan(JenisPekerjaan jenis)
    {
        var melanggar = new List<string>(3);
        var peringatan = new List<string>(1);
        if (jenis == JenisPekerjaan.Pengecoran)
        {
            if (ProbabilitasHujan > 60) melanggar.Add($"Peluang hujan {Angka(ProbabilitasHujan)}% > 60%");
            if (CurahHujan > 5) melanggar.Add($"Curah hujan {Angka(CurahHujan)} mm > 5 mm");
            if (SuhuMaks > 35) peringatan.Add($"Suhu maksimum {Angka(SuhuMaks)}°C; butuh mitigasi");
        }
        else
        {
            if (Kelembapan > 85) melanggar.Add($"Kelembapan {Angka(Kelembapan)}% > 85%");
            if (ProbabilitasHujan > 40) melanggar.Add($"Peluang hujan {Angka(ProbabilitasHujan)}% > 40%");
            if (SuhuMin < 10 || SuhuMaks > 35) melanggar.Add("Suhu di luar 10–35°C");
        }

        var status = melanggar.Count > 0 ? StatusKelayakan.TidakLayak
            : peringatan.Count > 0 ? StatusKelayakan.Waspada
            : StatusKelayakan.Layak;
        melanggar.AddRange(peringatan);
        var alasan = melanggar.Count > 0 ? string.Join("; ", melanggar) : "Tidak melewati ambang jenis pekerjaan";
        return new KelayakanCuaca(status, alasan);
    }

    private static string Angka(double nilai) => nilai.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct KelayakanCuaca(StatusKelayakan Status, string Alasan);
