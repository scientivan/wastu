namespace ImplementasiClassDiagram.Models;

public class Bahan
{
    private int _id;
    private string _nama = string.Empty;
    private double _konduktivitasTermal;
    private int _ketahananLembap;
    private int _ketahananKorosi;
    private int _usiaPakaiTahun;
    private double _faktorEmisi;
    private string _dataSource = string.Empty;

    public int Id { get => _id; set => _id = value; }
    public string Nama { get => _nama; set => _nama = value; }
    public double KonduktivitasTermal { get => _konduktivitasTermal; set => _konduktivitasTermal = value; }
    public int KetahananLembap { get => _ketahananLembap; set => _ketahananLembap = value; }
    public int KetahananKorosi { get => _ketahananKorosi; set => _ketahananKorosi = value; }
    public int UsiaPakaiTahun { get => _usiaPakaiTahun; set => _usiaPakaiTahun = value; }
    public double FaktorEmisi { get => _faktorEmisi; set => _faktorEmisi = value; }
    public string DataSource { get => _dataSource; set => _dataSource = value; }
}
