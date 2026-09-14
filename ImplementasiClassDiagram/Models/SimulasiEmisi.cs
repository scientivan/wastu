namespace ImplementasiClassDiagram.Models;

public class SimulasiEmisi
{
    private int _horizonTahun;
    private double _usiaPakaiEfektif;
    private int _jumlahPenggantian;
    private double _emisiKumulatif;

    public int HorizonTahun { get => _horizonTahun; set => _horizonTahun = value; }
    public double UsiaPakaiEfektif { get => _usiaPakaiEfektif; set => _usiaPakaiEfektif = value; }
    public int JumlahPenggantian { get => _jumlahPenggantian; private set => _jumlahPenggantian = value; }
    public double EmisiKumulatif { get => _emisiKumulatif; private set => _emisiKumulatif = value; }

    public double Proyeksikan() => EmisiKumulatif;

    public double Proyeksikan(double emisiAwal, double faktorEmisi)
    {
        if (UsiaPakaiEfektif <= 0)
            throw new InvalidOperationException("Usia pakai efektif harus lebih dari nol.");

        JumlahPenggantian = Math.Max(0, (int)Math.Ceiling(HorizonTahun / UsiaPakaiEfektif) - 1);
        EmisiKumulatif = emisiAwal + JumlahPenggantian * faktorEmisi;
        return EmisiKumulatif;
    }
}
