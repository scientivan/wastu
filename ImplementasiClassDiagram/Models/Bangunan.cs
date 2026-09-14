namespace ImplementasiClassDiagram.Models;

public class Bangunan
{
    private int _id;
    private string _nama = string.Empty;
    private string _kota = string.Empty;
    private double _latitude;
    private double _longitude;
    private readonly List<Komponen> _komponen = [];
    private readonly List<Laporan> _laporan = [];
    private readonly List<Pekerjaan> _pekerjaan = [];

    public int Id { get => _id; set => _id = value; }
    public string Nama { get => _nama; set => _nama = value; }
    public string Kota { get => _kota; set => _kota = value; }
    public double Latitude { get => _latitude; set => _latitude = value; }
    public double Longitude { get => _longitude; set => _longitude = value; }
    public ProfilIklim? ProfilIklim { get; set; }
    public IReadOnlyList<Komponen> Komponen => _komponen.AsReadOnly();
    public IReadOnlyList<Laporan> Laporan => _laporan.AsReadOnly();
    public IReadOnlyList<Pekerjaan> Pekerjaan => _pekerjaan.AsReadOnly();

    public void TambahKomponen(Komponen k)
    {
        ArgumentNullException.ThrowIfNull(k);
        _komponen.Add(k);
    }

    public void TambahLaporan(Laporan laporan)
    {
        ArgumentNullException.ThrowIfNull(laporan);
        _laporan.Add(laporan);
    }

    public void JadwalkanPekerjaan(Pekerjaan pekerjaan)
    {
        ArgumentNullException.ThrowIfNull(pekerjaan);
        _pekerjaan.Add(pekerjaan);
    }
}
