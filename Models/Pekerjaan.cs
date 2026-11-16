namespace Wastu.Models;

public class Pekerjaan
{
    private string _nama = string.Empty;
    private JenisPekerjaan _jenis;
    private DateTime _tanggalMulaiRencana;
    private DateTime _tanggalSelesaiRencana;
    private DateTime? _tanggalDikunci;
    private string _status = string.Empty;
    private readonly List<PrakiraanCuaca> _prakiraanCuaca = [];

    public string Nama { get => _nama; set => _nama = value; }
    public JenisPekerjaan Jenis { get => _jenis; set => _jenis = value; }
    public DateTime TanggalMulaiRencana { get => _tanggalMulaiRencana; set => _tanggalMulaiRencana = value; }
    public DateTime TanggalSelesaiRencana { get => _tanggalSelesaiRencana; set => _tanggalSelesaiRencana = value; }
    public DateTime? TanggalDikunci { get => _tanggalDikunci; private set => _tanggalDikunci = value; }
    public string Status { get => _status; private set => _status = value; }
    public IReadOnlyList<PrakiraanCuaca> PrakiraanCuaca => _prakiraanCuaca.AsReadOnly();

    public bool Jadwalkan(PrakiraanCuaca c) => Jadwalkan(c, tetapKunci: false);

    // Pengguna boleh mengunci hari tidak layak bila menanggung risikonya (PRD §12).
    public bool Jadwalkan(PrakiraanCuaca c, bool tetapKunci)
    {
        ArgumentNullException.ThrowIfNull(c);
        _prakiraanCuaca.Add(c);
        if (!tetapKunci && !c.LayakKerja(Jenis))
        {
            Status = "Perlu dijadwalkan ulang";
            return false;
        }

        TanggalDikunci = c.Tanggal.Date;
        Status = "Terjadwal";
        return true;
    }

    public void PulihkanJadwal(DateTime? tanggalDikunci)
    {
        TanggalDikunci = tanggalDikunci?.Date;
        Status = tanggalDikunci is null ? "Belum dikunci" : "Terjadwal";
    }
}
