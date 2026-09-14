namespace ImplementasiClassDiagram.Models;

public class Evaluasi
{
    private double _skorKesesuaian;
    private double _emisiAwal;
    private string _alasan = string.Empty;

    public Evaluasi(Bahan bahan) => Bahan = bahan ?? throw new ArgumentNullException(nameof(bahan));

    public double SkorKesesuaian { get => _skorKesesuaian; private set => _skorKesesuaian = value; }
    public double EmisiAwal { get => _emisiAwal; set => _emisiAwal = value; }
    public string Alasan { get => _alasan; set => _alasan = value; }
    public Bahan Bahan { get; }
    public SimulasiEmisi? SimulasiEmisi { get; private set; }

    public double HitungSkor(ProfilIklim iklim)
    {
        ArgumentNullException.ThrowIfNull(iklim);
        var skorLembap = Bahan.KetahananLembap - iklim.KelembapanRata / 10.0;
        var skorKorosi = Bahan.KetahananKorosi - (iklim.JarakPesisirKm < 10 ? 2 : 0);
        SkorKesesuaian = Math.Clamp((skorLembap + skorKorosi) * 5.0, 0.0, 100.0);
        return SkorKesesuaian;
    }

    public void ProyeksikanEmisi(int horizonTahun)
    {
        SimulasiEmisi = new SimulasiEmisi
        {
            HorizonTahun = horizonTahun,
            UsiaPakaiEfektif = Bahan.UsiaPakaiTahun
        };
        SimulasiEmisi.Proyeksikan(EmisiAwal, Bahan.FaktorEmisi);
    }
}
