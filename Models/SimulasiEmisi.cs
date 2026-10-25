namespace Wastu.Models;

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

    // Setiap penggantian mengulang emisi produksi bahan dari awal (PRD §11.3).
    public double Proyeksikan(double emisiAwal)
    {
        JumlahPenggantian = PenggantianHingga(HorizonTahun);
        EmisiKumulatif = emisiAwal * (1 + JumlahPenggantian);
        return EmisiKumulatif;
    }

    public int PenggantianHingga(int tahun)
    {
        if (UsiaPakaiEfektif <= 0)
            throw new InvalidOperationException("Usia pakai efektif harus lebih dari nol.");

        return Math.Max(0, (int)Math.Ceiling(tahun / UsiaPakaiEfektif - 1e-9) - 1);
    }
}
