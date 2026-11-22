namespace Wastu.Models;

public class Komponen
{
    private JenisKomponen _jenis;
    private double _luas;
    private readonly List<Evaluasi> _evaluasi = [];

    private Guid _localId = Guid.NewGuid();

    public Guid Id
    {
        get => DatabaseId is { } id && id != Guid.Empty ? id : _localId;
        set => _localId = value == Guid.Empty ? Guid.NewGuid() : value;
    }

    public Guid? DatabaseId { get; set; }

    public JenisKomponen Jenis { get => _jenis; set => _jenis = value; }
    public double Luas { get => _luas; set => _luas = value; }

    public Evaluasi[] BandingkanBahan() =>
        _evaluasi.OrderByDescending(e => e.SkorKesesuaian).ToArray();

    public void TambahEvaluasi(Evaluasi evaluasi)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);
        _evaluasi.Add(evaluasi);
    }
}
