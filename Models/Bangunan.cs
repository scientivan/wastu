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
    public Guid? DatabaseId { get; init; }
    public string Nama { get => _nama; set => _nama = value; }
    public string Kota { get => _kota; set => _kota = value; }
    public double Latitude { get => _latitude; set => _latitude = value; }
    public double Longitude { get => _longitude; set => _longitude = value; }
    public int RevisiLokasi { get; set; } = 1;
    public string ZonaWaktu { get; set; } = "Asia/Jakarta";
    public Domain.KriteriaBobot Bobot { get; set; } = Domain.KriteriaBobot.Default;
    public ProfilIklim? ProfilIklim { get; set; }
    public IReadOnlyList<Komponen> Komponen => _komponen.AsReadOnly();
    public IReadOnlyList<Laporan> Laporan => _laporan.AsReadOnly();
    public IReadOnlyList<Pekerjaan> Pekerjaan => _pekerjaan.AsReadOnly();

    public void TambahKomponen(Komponen k)
    {
        ArgumentNullException.ThrowIfNull(k);
        _komponen.Add(k);
    }

    public bool AdaKomponen(JenisKomponen jenis) => _komponen.Any(k => k.Jenis == jenis);

    public IReadOnlyList<JenisKomponen> JenisKomponenAktif => _komponen.Select(k => k.Jenis).Distinct().ToList();

    public void HapusKomponen(JenisKomponen jenis) => _komponen.RemoveAll(k => k.Jenis == jenis);

    public void AturKomponen(IReadOnlyList<(JenisKomponen Jenis, double Luas)> daftar)
    {
        ArgumentNullException.ThrowIfNull(daftar);
        if (daftar.Count == 0)
        {
            throw new ArgumentException("Daftar komponen tidak boleh kosong.", nameof(daftar));
        }

        if (daftar.Select(x => x.Jenis).Distinct().Count() != daftar.Count)
        {
            throw new ArgumentException("Jenis komponen tidak boleh duplikat.", nameof(daftar));
        }

        foreach (var item in daftar)
        {
            if (!double.IsFinite(item.Luas) || item.Luas <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(daftar), item.Luas, "Luas komponen harus bernilai finite dan lebih besar dari 0 m².");
            }
        }

        _komponen.RemoveAll(k => !daftar.Any(d => d.Jenis == k.Jenis));
        foreach (var item in daftar)
        {
            var existing = _komponen.Find(k => k.Jenis == item.Jenis);
            if (existing is not null)
            {
                existing.Luas = item.Luas;
            }
            else
            {
                _komponen.Add(new Komponen { Jenis = item.Jenis, Luas = item.Luas });
            }
        }
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
