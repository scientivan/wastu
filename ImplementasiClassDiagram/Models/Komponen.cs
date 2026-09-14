namespace ImplementasiClassDiagram.Models;

public class Komponen
{
    private JenisKomponen _jenis;
    private double _luas;
    private readonly List<Evaluasi> _evaluasi = [];

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
