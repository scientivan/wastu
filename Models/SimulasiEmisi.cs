using Wastu.Domain.Emissions;

namespace Wastu.Models;

/// <summary>
/// Proyeksi emisi jangka panjang untuk UI yang mendelegasikan seluruh perhitungan ke Domain (PRD §11.3, PRD-R02).
/// </summary>
public class SimulasiEmisi
{
    private int _horizonTahun;
    private double _usiaPakaiEfektif;
    private int _jumlahPenggantian;
    private double _emisiKumulatif;
    private double _faktorPenuaanA = 0.5;

    public int HorizonTahun { get => _horizonTahun; set => _horizonTahun = value; }
    public double UsiaPakaiEfektif { get => _usiaPakaiEfektif; set => _usiaPakaiEfektif = value; }
    public int JumlahPenggantian { get => _jumlahPenggantian; private set => _jumlahPenggantian = value; }
    public double EmisiKumulatif { get => _emisiKumulatif; private set => _emisiKumulatif = value; }
    public double FaktorPenuaanA { get => _faktorPenuaanA; set => _faktorPenuaanA = value; }
    public double UsiaPakaiNominal { get; set; }
    public double SkorKesesuaian { get; set; }
    public double EmisiAwal { get; set; }
    public string VersiModel { get; set; } = "v2.0";

    public double Proyeksikan() => EmisiKumulatif;

    // Setiap penggantian mengulang emisi produksi bahan dari awal (PRD §11.3, PRD-R02).
    public double Proyeksikan(double emisiAwal)
    {
        EmisiAwal = emisiAwal;
        JumlahPenggantian = PerhitunganSimulasi.JumlahPenggantian(HorizonTahun, UsiaPakaiEfektif);
        EmisiKumulatif = PerhitunganSimulasi.EmisiKumulatif(emisiAwal, JumlahPenggantian);
        return EmisiKumulatif;
    }

    public int PenggantianHingga(int tahun)
    {
        if (UsiaPakaiEfektif <= 0)
            throw new InvalidOperationException("Usia pakai efektif harus lebih dari nol.");

        if (tahun <= 0)
            return 0;

        return PerhitunganSimulasi.JumlahPenggantian(tahun, UsiaPakaiEfektif);
    }

    public double EmisiPada(double waktuT)
    {
        return PerhitunganSimulasi.EmisiKumulatifPadaTitik(EmisiAwal, waktuT, HorizonTahun, UsiaPakaiEfektif);
    }

    public IReadOnlyList<TitikSimulasi> TitikKurvaStep()
    {
        return PerhitunganSimulasi.TitikKurvaStep(EmisiAwal, HorizonTahun, UsiaPakaiEfektif);
    }

    public IReadOnlyList<double> KejadianPenggantian()
    {
        return PerhitunganSimulasi.KejadianPenggantian(HorizonTahun, UsiaPakaiEfektif);
    }
}
