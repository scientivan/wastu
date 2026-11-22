namespace Wastu.Models;

public class Bahan
{
    private int _id;
    private string _nama = string.Empty;
    private double _konduktivitasTermal;
    private int _ketahananLembap;
    private int _ketahananKorosi;
    private double _usiaPakaiTahun;
    private double _faktorEmisi;
    private string _dataSource = string.Empty;
    private string? _susunanKetebalan;
    private IReadOnlyList<Domain.SumberPropertiBahan> _sumberProperti = [];

    public int Id { get => _id; set => _id = value; }
    public string Nama { get => _nama; set => _nama = value; }
    public double KonduktivitasTermal { get => _konduktivitasTermal; set => _konduktivitasTermal = value; }
    public int KetahananLembap { get => _ketahananLembap; set => _ketahananLembap = value; }
    public int KetahananKorosi { get => _ketahananKorosi; set => _ketahananKorosi = value; }
    public double UsiaPakaiTahun { get => _usiaPakaiTahun; set => _usiaPakaiTahun = value; }
    public double FaktorEmisi { get => _faktorEmisi; set => _faktorEmisi = value; }
    public string DataSource { get => _dataSource; set => _dataSource = value; }
    public string? SusunanKetebalan { get => _susunanKetebalan; set => _susunanKetebalan = value; }
    public IReadOnlyList<Domain.SumberPropertiBahan> SumberProperti { get => _sumberProperti; set => _sumberProperti = value; }
}
