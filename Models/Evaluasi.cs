namespace Wastu.Models;

public class Evaluasi
{
    private double _skorKesesuaian;
    private double _emisiAwal;
    private string _alasan = string.Empty;
    private double _faktorPenuaanA = 0.5;

    public Evaluasi(Bahan bahan) => Bahan = bahan ?? throw new ArgumentNullException(nameof(bahan));

    internal Evaluasi(Bahan bahan, Domain.Evaluasi hasil) : this(bahan)
    {
        HasilDomain = hasil;
        SkorKesesuaian = hasil.SkorKesesuaian;
        EmisiAwal = hasil.EmisiAwalKg;
        Alasan = hasil.Alasan;
        SubSkor = hasil.SubSkor;
    }

    public Domain.Evaluasi? HasilDomain { get; internal set; }
    public double SkorKesesuaian { get => _skorKesesuaian; private set => _skorKesesuaian = value; }
    public double EmisiAwal { get => _emisiAwal; set => _emisiAwal = value; }
    public string Alasan { get => _alasan; set => _alasan = value; }
    public Domain.SubSkorKesesuaian SubSkor { get; private set; } = new(0, 0, 0, 0);
    public double FaktorPenuaanA { get => _faktorPenuaanA; set => _faktorPenuaanA = value; }
    public Bahan Bahan { get; }
    public SimulasiEmisi? SimulasiEmisi { get; private set; }

    public double HitungSkor(ProfilIklim iklim, Domain.KriteriaBobot? bobot = null) =>
        HitungSkor(iklim, Bahan.UsiaPakaiTahun, Bahan.UsiaPakaiTahun, bobot);

    public double HitungSkor(ProfilIklim iklim, double usiaMinKelompok, double usiaMaksKelompok, Domain.KriteriaBobot? bobot = null)
    {
        ArgumentNullException.ThrowIfNull(iklim);
        var bobotEfektif = bobot ?? Domain.KriteriaBobot.Default;
        bobotEfektif.PastikanValid();
        var bahan = new Domain.Bahan(Bahan.DomainId, Bahan.Nama, Bahan.JenisDomain,
            Bahan.KonduktivitasTermal, Bahan.KetahananLembap, Bahan.KetahananKorosi, Bahan.UsiaPakaiTahun,
            Bahan.FaktorEmisi, string.IsNullOrWhiteSpace(Bahan.DataSource) ? "Estimasi awal" : Bahan.DataSource);
        var profil = iklim.KeDomain();
        var skor = Domain.Scoring.PerhitunganSkor.HitungSubSkor(bahan, profil, usiaMinKelompok, usiaMaksKelompok);
        SkorKesesuaian = Domain.Scoring.PerhitunganSkor.SkorKesesuaian(skor, bobotEfektif);
        SubSkor = skor;
        Alasan = Domain.Scoring.PerhitunganSkor.SusunAlasan(bahan, profil, skor);
        return SkorKesesuaian;
    }

    public void ProyeksikanEmisi(int horizonTahun) => ProyeksikanEmisi(horizonTahun, null);

    public void ProyeksikanEmisi(int horizonTahun, double? faktorPenuaanA)
    {
        double a = faktorPenuaanA ?? FaktorPenuaanA;
        FaktorPenuaanA = a;
        double usiaEfektif = Domain.Emissions.PerhitunganSimulasi.UsiaPakaiEfektif(Bahan.UsiaPakaiTahun, SkorKesesuaian, a);
        SimulasiEmisi = new SimulasiEmisi
        {
            HorizonTahun = horizonTahun,
            UsiaPakaiNominal = Bahan.UsiaPakaiTahun,
            SkorKesesuaian = SkorKesesuaian,
            FaktorPenuaanA = a,
            UsiaPakaiEfektif = usiaEfektif
        };
        SimulasiEmisi.Proyeksikan(EmisiAwal);
    }
}
