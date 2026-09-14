namespace ImplementasiClassDiagram.Models;

public class Pengguna
{
    private int _id;
    private string _nama = string.Empty;
    private string _email = string.Empty;
    private readonly List<Bangunan> _bangunan = [];

    public int Id { get => _id; set => _id = value; }
    public string Nama { get => _nama; set => _nama = value; }
    public string Email { get => _email; set => _email = value; }
    public IReadOnlyList<Bangunan> Bangunan => _bangunan.AsReadOnly();

    public bool Login() => !string.IsNullOrWhiteSpace(_email);

    public void TambahBangunan(Bangunan bangunan)
    {
        ArgumentNullException.ThrowIfNull(bangunan);
        if (!_bangunan.Contains(bangunan))
            _bangunan.Add(bangunan);
    }
}
