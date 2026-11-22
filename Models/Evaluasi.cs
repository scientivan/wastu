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
        SkorKesesuaian = hasil.SkorKesesuaian;
        EmisiAwal = hasil.EmisiAwalKg;
        Alasan = hasil.Alasan;
    }

    public double SkorKesesuaian { get => _skorKesesuaian; private set => _skorKesesuaian = value; }
    public double EmisiAwal { get => _emisiAwal; set => _emisiAwal = value; }
    public string Alasan { get => _alasan; set => _alasan = value; }
    public double FaktorPenuaanA { get => _faktorPenuaanA; set => _faktorPenuaanA = value; }
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
