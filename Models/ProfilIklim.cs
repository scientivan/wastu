namespace Wastu.Models;

public class ProfilIklim
{
    private double _curahHujanTahunan;
    private double _kelembapanRata;
    private double _suhuRata;
    private double _jarakPesisirKm;
    private string _tingkatKorosi = string.Empty;
    private string _sumber = string.Empty;
    private string? _diperbaruiPada;

    public double CurahHujanTahunan { get => _curahHujanTahunan; set => _curahHujanTahunan = value; }
    public double KelembapanRata { get => _kelembapanRata; set => _kelembapanRata = value; }
    public double SuhuRata { get => _suhuRata; set => _suhuRata = value; }
    public double JarakPesisirKm
    {
        get => _jarakPesisirKm;
        set
        {
            _jarakPesisirKm = value;
            _tingkatKorosi = value < 10 ? "Tinggi" : value < 35 ? "Sedang" : "Rendah";
        }
    }
    public string TingkatKorosi { get => _tingkatKorosi; set => _tingkatKorosi = value; }
    public string Sumber { get => _sumber; set => _sumber = value; }

    // Null selama profil masih memakai nilai bawaan kota, belum diambil dari layanan iklim.
    public string? DiperbaruiPada { get => _diperbaruiPada; set => _diperbaruiPada = value; }
    public bool SudahDiperbarui => _diperbaruiPada is not null;
}
