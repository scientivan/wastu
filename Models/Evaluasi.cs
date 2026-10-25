namespace Wastu.Models;

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

    public double HitungSkor(ProfilIklim iklim) =>
        HitungSkor(iklim, Bahan.UsiaPakaiTahun, Bahan.UsiaPakaiTahun);

    // Rata-rata empat sub-skor 0–100: termal, lembap, korosi, dan usia pakai relatif
    // terhadap bahan sejenis (PRD §11.1).
    public double HitungSkor(ProfilIklim iklim, int usiaMinKelompok, int usiaMaksKelompok)
    {
        ArgumentNullException.ThrowIfNull(iklim);
        var kebutuhanLembap = iklim.CurahHujanTahunan > 2200 || iklim.KelembapanRata > 80 ? 5 : 3;
        var paparanPesisir = iklim.JarakPesisirKm < 10 ? 5 : iklim.JarakPesisirKm < 35 ? 4 : 2;
        var batasTermal = iklim.SuhuRata > 27 ? 0.7 : 1.25;

        var termal = Math.Max(0, 100 - Math.Max(0, Bahan.KonduktivitasTermal - batasTermal) * 16);
        var lembap = Math.Max(0, 100 - Math.Abs(kebutuhanLembap - Bahan.KetahananLembap) * 21);
        var korosi = Math.Max(0, 100 - Math.Max(0, paparanPesisir - Bahan.KetahananKorosi) * 24);
        var usia = usiaMaksKelompok == usiaMinKelompok
            ? 100
            : 50 + (double)(Bahan.UsiaPakaiTahun - usiaMinKelompok) / (usiaMaksKelompok - usiaMinKelompok) * 50;

        SkorKesesuaian = Math.Clamp(Math.Floor((termal + lembap + korosi + usia) / 4 + 0.5), 0, 100);
        Alasan = SusunAlasan(iklim);
        return SkorKesesuaian;
    }

    public void ProyeksikanEmisi(int horizonTahun)
    {
        // Bahan yang kurang cocok dengan iklim bertahan lebih singkat (PRD §11.3).
        SimulasiEmisi = new SimulasiEmisi
        {
            HorizonTahun = horizonTahun,
            UsiaPakaiEfektif = Bahan.UsiaPakaiTahun * (0.5 + 0.5 * SkorKesesuaian / 100)
        };
        SimulasiEmisi.Proyeksikan(EmisiAwal);
    }

    private string SusunAlasan(ProfilIklim iklim)
    {
        var alasan = iklim.CurahHujanTahunan > 2200 && Bahan.KetahananLembap < 4
            ? "Ketahanan lembap material perlu perhatian pada curah hujan tinggi."
            : "Ketahanan lembap relatif baik untuk profil hujan yang digunakan.";
        if (iklim.JarakPesisirKm < 15 && Bahan.KetahananKorosi < 4)
            alasan += " Lokasi dekat pesisir meningkatkan risiko korosi; detail perlindungan penting.";
        if (Bahan.KonduktivitasTermal > 1 && iklim.SuhuRata > 27)
            alasan += " Konduktivitas panas tinggi berpotensi meningkatkan panas permukaan.";
        return alasan;
    }
}
