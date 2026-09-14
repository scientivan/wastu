namespace ImplementasiClassDiagram.Models;

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

    public bool LayakKerja() => ProbabilitasHujan < 0.5 && CurahHujan <= 5 && SuhuMaks <= 35;
}
