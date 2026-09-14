namespace ImplementasiClassDiagram.Models;

public class ProfilIklim
{
    private double _curahHujanTahunan;
    private double _kelembapanRata;
    private double _suhuRata;
    private double _jarakPesisirKm;
    private string _tingkatKorosi = string.Empty;

    public double CurahHujanTahunan { get => _curahHujanTahunan; set => _curahHujanTahunan = value; }
    public double KelembapanRata { get => _kelembapanRata; set => _kelembapanRata = value; }
    public double SuhuRata { get => _suhuRata; set => _suhuRata = value; }
    public double JarakPesisirKm { get => _jarakPesisirKm; set => _jarakPesisirKm = value; }
    public string TingkatKorosi { get => _tingkatKorosi; set => _tingkatKorosi = value; }
}
