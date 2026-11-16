namespace Wastu.Models;

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

    public double LuasKomponen(JenisKomponen jenis) =>
        _komponen.Find(k => k.Jenis == jenis)?.Luas ?? 0;

    public void AturLuasKomponen(JenisKomponen jenis, double luas)
    {
        var komponen = _komponen.Find(k => k.Jenis == jenis);
        if (komponen is null)
            TambahKomponen(new Komponen { Jenis = jenis, Luas = luas });
        else
            komponen.Luas = luas;
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

    public bool HapusPekerjaan(Pekerjaan pekerjaan) => _pekerjaan.Remove(pekerjaan);
}
