namespace Wastu.Models;

public class Evaluasi
{
    private double _skorKesesuaian;
    private double _emisiAwal;
    private string _alasan = string.Empty;

    public Evaluasi(Bahan bahan) => Bahan = bahan ?? throw new ArgumentNullException(nameof(bahan));

    internal Evaluasi(Bahan bahan, Domain.Evaluasi hasil) : this(bahan)
    {
        SkorKesesuaian = hasil.SkorKesesuaian;
        EmisiAwal = hasil.EmisiAwalKg;
        Alasan = hasil.Alasan;
    }

    public double SkorKesesuaian { get => _skorKesesuaian; private set => _skorKesesuaian = value; }
    public double EmisiAwal { get => _emisiAwal; set => _emisiAwal = value; }
    public string Alasan { get => _alasan; set => _alasan = value; }
    public Bahan Bahan { get; }
    public SimulasiEmisi? SimulasiEmisi { get; private set; }

    public double HitungSkor(ProfilIklim iklim) =>
        HitungSkor(iklim, Bahan.UsiaPakaiTahun, Bahan.UsiaPakaiTahun);

    public double HitungSkor(ProfilIklim iklim, double usiaMinKelompok, double usiaMaksKelompok)
    {
        ArgumentNullException.ThrowIfNull(iklim);
        var bahan = new Domain.Bahan(Guid.Empty, Bahan.Nama, Domain.JenisKomponen.Dinding,
            Bahan.KonduktivitasTermal, Bahan.KetahananLembap, Bahan.KetahananKorosi, Bahan.UsiaPakaiTahun,
            Bahan.FaktorEmisi, string.IsNullOrWhiteSpace(Bahan.DataSource) ? "Estimasi awal" : Bahan.DataSource);
        var profil = iklim.KeDomain();
        var skor = Domain.Scoring.PerhitunganSkor.HitungSubSkor(bahan, profil, usiaMinKelompok, usiaMaksKelompok);
        SkorKesesuaian = Domain.Scoring.PerhitunganSkor.SkorKesesuaian(skor, Domain.KriteriaBobot.Default);
        Alasan = Domain.Scoring.PerhitunganSkor.SusunAlasan(bahan, profil, skor);
        return SkorKesesuaian;
    }

    public void ProyeksikanEmisi(int horizonTahun)
    {
        // Bahan yang kurang cocok dengan iklim bertahan lebih singkat (PRD §11.3).
        SimulasiEmisi = new SimulasiEmisi
        {
            HorizonTahun = horizonTahun,
            UsiaPakaiEfektif = Domain.Emissions.PerhitunganSimulasi.UsiaPakaiEfektif(Bahan.UsiaPakaiTahun, SkorKesesuaian)
        };
        SimulasiEmisi.Proyeksikan(EmisiAwal);
    }

}
