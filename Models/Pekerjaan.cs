using Wastu.Domain.Weather;

namespace Wastu.Models;

public class Pekerjaan
{
    private string _nama = string.Empty;
    private JenisPekerjaan _jenis;
    private DateTime _tanggalMulaiRencana;
    private DateTime _tanggalSelesaiRencana;
    private DateTime? _tanggalDikunci;
    private string _status = "Draft";
    private readonly List<PrakiraanCuaca> _prakiraanCuaca = [];
    private AmbangPekerjaan? _overrideAmbang;
    private bool _persetujuanRisiko;
    private bool _konfirmasiDataLama;
    private string? _catatanKeputusan;

    public string Nama { get => _nama; set => _nama = value; }
    public JenisPekerjaan Jenis { get => _jenis; set => _jenis = value; }
    public DateTime TanggalMulaiRencana { get => _tanggalMulaiRencana; set => _tanggalMulaiRencana = value; }
    public DateTime TanggalSelesaiRencana { get => _tanggalSelesaiRencana; set => _tanggalSelesaiRencana = value; }
    public DateTime? TanggalDikunci { get => _tanggalDikunci; set => _tanggalDikunci = value; }
    public string Status { get => _status; set => _status = value; }
    public IReadOnlyList<PrakiraanCuaca> PrakiraanCuaca => _prakiraanCuaca.AsReadOnly();

    public AmbangPekerjaan? OverrideAmbang { get => _overrideAmbang; set => _overrideAmbang = value; }
    public bool PersetujuanRisiko { get => _persetujuanRisiko; set => _persetujuanRisiko = value; }
    public bool KonfirmasiDataLama { get => _konfirmasiDataLama; set => _konfirmasiDataLama = value; }
    public string? CatatanKeputusan { get => _catatanKeputusan; set => _catatanKeputusan = value; }

    public bool Jadwalkan(PrakiraanCuaca c) => Jadwalkan(c, tetapKunci: false);

    // Pengguna boleh mengunci hari tidak layak bila menanggung risikonya (PRD §12).
    public bool Jadwalkan(PrakiraanCuaca c, bool tetapKunci)
    {
        ArgumentNullException.ThrowIfNull(c);
        _prakiraanCuaca.Add(c);
        if (!tetapKunci && !c.LayakKerja(Jenis, OverrideAmbang))
        {
            Status = "Perlu ditinjau";
            return false;
        }

        TanggalDikunci = c.Tanggal.Date;
        Status = "Terkunci";
        return true;
    }

    public void PulihkanJadwal(DateTime? tanggalDikunci)
    {
        TanggalDikunci = tanggalDikunci?.Date;
        Status = tanggalDikunci is null ? "Draft" : "Terkunci";
    }
}
