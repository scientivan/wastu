namespace Wastu.Domain.Emissions;

/// <summary>
/// Titik koordinat pada grafik step simulasi emisi.
/// </summary>
public readonly record struct TitikSimulasi(double WaktuTahun, double EmisiKumulatifKg);

/// <summary>
/// Perhitungan murni simulasi emisi jangka panjang (PRD §11.3, PRD-R02).
/// </summary>
public static class PerhitunganSimulasi
{
    /// <summary>Konstanta fungsi penyesuaian: bahan berskor 0 diasumsikan hanya bertahan separuh usia nominalnya.</summary>
    public const double FaktorDasar = 0.5;

    /// <summary>Rentang penyesuaian; bahan berskor 100 mempertahankan usia nominal penuh.</summary>
    public const double RentangFaktor = 0.5;

    /// <summary>
    /// Fungsi penyesuaian f(skor) = a + (1 - a) * (skor / 100) dengan default a = 0,5 (PRD §11.3, PRD-R02).
    /// </summary>
    public static double FaktorPenyesuaian(double skorKesesuaian)
        => FaktorPenyesuaian(skorKesesuaian, KonfigurasiModel.Bawaan.FaktorPenuaanA);

    /// <summary>
    /// Fungsi penyesuaian f(skor, a) = a + (1 - a) * (skor / 100) dengan faktor penuaan a terkonfigurasi (PRD §11.3, PRD-R02, PRD-R11).
    /// </summary>
    public static double FaktorPenyesuaian(double skorKesesuaian, double faktorPenuaanA)
    {
        if (!double.IsFinite(skorKesesuaian) || skorKesesuaian is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skorKesesuaian), skorKesesuaian,
                "Skor kesesuaian harus bernilai finite antara 0 dan 100.");
        }

        if (!double.IsFinite(faktorPenuaanA) || faktorPenuaanA <= 0 || faktorPenuaanA > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(faktorPenuaanA), faktorPenuaanA,
                "Faktor penuaan a harus bernilai finite dan berada dalam rentang 0 < a <= 1.");
        }

        return faktorPenuaanA + (1.0 - faktorPenuaanA) * (skorKesesuaian / 100.0);
    }

    /// <summary>Usia pakai efektif = usia nominal * f(skor) dengan default a = 0,5 (PRD §11.3).</summary>
    public static double UsiaPakaiEfektif(double usiaPakaiNominal, double skorKesesuaian)
        => UsiaPakaiEfektif(usiaPakaiNominal, skorKesesuaian, KonfigurasiModel.Bawaan.FaktorPenuaanA);

    /// <summary>Usia pakai efektif = usia nominal * f(skor, a) (PRD §11.3, PRD-R02).</summary>
    public static double UsiaPakaiEfektif(double usiaPakaiNominal, double skorKesesuaian, double faktorPenuaanA)
    {
        if (!double.IsFinite(usiaPakaiNominal) || usiaPakaiNominal <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiNominal), usiaPakaiNominal,
                "Usia pakai nominal harus bernilai finite dan lebih dari nol.");
        }

        return usiaPakaiNominal * FaktorPenyesuaian(skorKesesuaian, faktorPenuaanA);
    }

    /// <summary>
    /// Banyaknya penggantian yang benar-benar jatuh di dalam horizon (0 < t < H):
    /// max(0, ceil(H / L) - 1) (PRD-R02).
    /// </summary>
    public static int JumlahPenggantian(int horizonTahun, double usiaPakaiEfektif)
        => JumlahPenggantian((double)horizonTahun, usiaPakaiEfektif);

    /// <summary>
    /// Banyaknya penggantian yang benar-benar jatuh di dalam horizon (0 < t < H):
    /// max(0, ceil(H / L) - 1) dengan presisi boundary (PRD-R02, T18, T19).
    /// Penggantian tepat pada batas akhir horizon t = H tidak dihitung.
    /// </summary>
    public static int JumlahPenggantian(double horizonTahun, double usiaPakaiEfektif)
    {
        if (!double.IsFinite(horizonTahun) || horizonTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonTahun), horizonTahun,
                "Horizon simulasi harus bernilai finite dan lebih dari nol tahun.");
        }

        if (!double.IsFinite(usiaPakaiEfektif) || usiaPakaiEfektif <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiEfektif), usiaPakaiEfektif,
                "Usia pakai efektif harus bernilai finite dan lebih dari nol.");
        }

        try
        {
            decimal h = (decimal)horizonTahun;
            decimal l = (decimal)usiaPakaiEfektif;
            decimal ratio = h / l;
            decimal ceil = Math.Ceiling(ratio);
            decimal count = ceil - 1m;
            return (int)Math.Max(0m, count);
        }
        catch (OverflowException)
        {
            double ratio = horizonTahun / usiaPakaiEfektif;
            double ceil = Math.Ceiling(ratio);
            double count = ceil - 1.0;
            return (int)Math.Max(0.0, count);
        }
    }

    /// <summary>Emisi kumulatif = emisi_awal * (1 + jumlah_penggantian) (PRD §11.3, PRD-R02).</summary>
    public static double EmisiKumulatif(double emisiAwal, int jumlahPenggantian)
    {
        if (!double.IsFinite(emisiAwal) || emisiAwal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emisiAwal), emisiAwal,
                "Emisi awal harus bernilai finite dan tidak boleh negatif.");
        }

        if (jumlahPenggantian < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jumlahPenggantian), jumlahPenggantian,
                "Jumlah penggantian tidak boleh negatif.");
        }

        return emisiAwal * (1 + jumlahPenggantian);
    }

    /// <summary>
    /// Waktu-waktu terjadinya penggantian material k * L di dalam interval interior 0 < k * L < H (PRD-R02, T19).
    /// </summary>
    public static IReadOnlyList<double> KejadianPenggantian(int horizonTahun, double usiaPakaiEfektif)
        => KejadianPenggantian((double)horizonTahun, usiaPakaiEfektif);

    /// <summary>
    /// Waktu-waktu terjadinya penggantian material k * L di dalam interval interior 0 < k * L < H (PRD-R02, T19).
    /// </summary>
    public static IReadOnlyList<double> KejadianPenggantian(double horizonTahun, double usiaPakaiEfektif)
    {
        if (!double.IsFinite(horizonTahun) || horizonTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonTahun), horizonTahun,
                "Horizon simulasi harus bernilai finite dan lebih dari nol tahun.");
        }

        if (!double.IsFinite(usiaPakaiEfektif) || usiaPakaiEfektif <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiEfektif), usiaPakaiEfektif,
                "Usia pakai efektif harus bernilai finite dan lebih dari nol.");
        }

        int jumlah = JumlahPenggantian(horizonTahun, usiaPakaiEfektif);
        var kejadian = new List<double>(jumlah);

        try
        {
            decimal h = (decimal)horizonTahun;
            decimal l = (decimal)usiaPakaiEfektif;
            for (int k = 1; k <= jumlah; k++)
            {
                decimal t = k * l;
                if (t < h)
                {
                    kejadian.Add((double)t);
                }
            }
        }
        catch (OverflowException)
        {
            for (int k = 1; k <= jumlah; k++)
            {
                double t = k * usiaPakaiEfektif;
                if (t < horizonTahun)
                {
                    kejadian.Add(t);
                }
            }
        }

        return kejadian;
    }

    /// <summary>
    /// Menghitung emisi kumulatif pada satu titik waktu t (0 <= t <= H) (PRD-R02, T19).
    /// - Pada t = 0: emisi awal (x1; instalasi awal bukan penggantian).
    /// - Pada interior 0 < t < H: mencakup seluruh penggantian yang terjadi pada atau sebelum t (<= t).
    /// - Pada endpoint t = H: sama dengan total emisi kumulatif emisiAwal * (1 + JumlahPenggantian(H, L)) tanpa lonjakan di batas akhir.
    /// </summary>
    public static double EmisiKumulatifPadaTitik(
        double emisiAwal, double waktuT, double horizonTahun, double usiaPakaiEfektif)
    {
        if (!double.IsFinite(emisiAwal) || emisiAwal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emisiAwal), emisiAwal, "Emisi awal harus bernilai finite dan tidak boleh negatif.");
        }

        if (!double.IsFinite(waktuT) || waktuT < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waktuT), waktuT, "Waktu t harus bernilai finite dan tidak boleh negatif.");
        }

        if (!double.IsFinite(horizonTahun) || horizonTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonTahun), horizonTahun, "Horizon simulasi harus bernilai finite dan lebih dari nol tahun.");
        }

        if (!double.IsFinite(usiaPakaiEfektif) || usiaPakaiEfektif <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiEfektif), usiaPakaiEfektif, "Usia pakai efektif harus bernilai finite dan lebih dari nol.");
        }

        if (waktuT > horizonTahun)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waktuT), waktuT, "Waktu t tidak boleh melebihi horizon simulasi.");
        }

        if (waktuT == 0)
        {
            return emisiAwal;
        }

        if (waktuT >= horizonTahun)
        {
            int penggantianAkhir = JumlahPenggantian(horizonTahun, usiaPakaiEfektif);
            return EmisiKumulatif(emisiAwal, penggantianAkhir);
        }

        try
        {
            decimal t = (decimal)waktuT;
            decimal l = (decimal)usiaPakaiEfektif;
            int penggantian = (int)Math.Max(0m, Math.Floor(t / l));
            return EmisiKumulatif(emisiAwal, penggantian);
        }
        catch (OverflowException)
        {
            int penggantian = (int)Math.Max(0.0, Math.Floor(waktuT / usiaPakaiEfektif));
            return EmisiKumulatif(emisiAwal, penggantian);
        }
    }

    /// <summary>
    /// Menghasilkan titik-titik kurva step untuk visualisasi grafik emisi (PRD-R02, §F5-04, T19).
    /// </summary>
    public static IReadOnlyList<TitikSimulasi> TitikKurvaStep(
        double emisiAwal, double horizonTahun, double usiaPakaiEfektif)
    {
        if (!double.IsFinite(emisiAwal) || emisiAwal < 0)
            throw new ArgumentOutOfRangeException(nameof(emisiAwal), emisiAwal, "Emisi awal harus bernilai finite dan tidak boleh negatif.");
        if (!double.IsFinite(horizonTahun) || horizonTahun <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizonTahun), horizonTahun, "Horizon simulasi harus bernilai finite dan lebih dari nol tahun.");
        if (!double.IsFinite(usiaPakaiEfektif) || usiaPakaiEfektif <= 0)
            throw new ArgumentOutOfRangeException(nameof(usiaPakaiEfektif), usiaPakaiEfektif, "Usia pakai efektif harus bernilai finite dan lebih dari nol.");

        var titik = new List<TitikSimulasi>();
        titik.Add(new TitikSimulasi(0.0, emisiAwal));

        var kejadian = KejadianPenggantian(horizonTahun, usiaPakaiEfektif);
        double levelSaatIni = emisiAwal;

        for (int i = 0; i < kejadian.Count; i++)
        {
            double t = kejadian[i];
            titik.Add(new TitikSimulasi(t, levelSaatIni));
            levelSaatIni = EmisiKumulatif(emisiAwal, i + 1);
            titik.Add(new TitikSimulasi(t, levelSaatIni));
        }

        titik.Add(new TitikSimulasi(horizonTahun, levelSaatIni));
        return titik;
    }

    /// <summary>
    /// Menjalankan ketiga langkah simulasi sekaligus dengan konfigurasi default bawaan.
    /// </summary>
    public static SimulasiEmisi Simulasikan(
        Guid evaluasiId, Evaluasi evaluasi, double usiaPakaiNominal, int horizonTahun)
        => Simulasikan(evaluasiId, evaluasi, usiaPakaiNominal, (double)horizonTahun, null, null);

    /// <summary>
    /// Menjalankan simulasi emisi dengan konfigurasi model dan timestamp opsional.
    /// </summary>
    public static SimulasiEmisi Simulasikan(
        Guid evaluasiId,
        Evaluasi evaluasi,
        double usiaPakaiNominal,
        int horizonTahun,
        KonfigurasiModel? konfigurasi = null,
        DateTimeOffset? dihitungPadaUtc = null)
        => Simulasikan(evaluasiId, evaluasi, usiaPakaiNominal, (double)horizonTahun, konfigurasi, dihitungPadaUtc);

    /// <summary>
    /// Menjalankan simulasi emisi dengan horizon double, konfigurasi model dan timestamp opsional.
    /// </summary>
    public static SimulasiEmisi Simulasikan(
        Guid evaluasiId,
        Evaluasi evaluasi,
        double usiaPakaiNominal,
        double horizonTahun,
        KonfigurasiModel? konfigurasi = null,
        DateTimeOffset? dihitungPadaUtc = null)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);

        var cfg = konfigurasi ?? KonfigurasiModel.Bawaan;
        double a = cfg.FaktorPenuaanA;
        double usiaEfektif = UsiaPakaiEfektif(usiaPakaiNominal, evaluasi.SkorKesesuaian, a);
        int penggantian = JumlahPenggantian(horizonTahun, usiaEfektif);
        double kumulatif = EmisiKumulatif(evaluasi.EmisiAwalKg, penggantian);

        return new SimulasiEmisi(
            EvaluasiId: evaluasiId,
            HorizonTahun: (int)Math.Round(horizonTahun, MidpointRounding.AwayFromZero),
            UsiaPakaiEfektifTahun: usiaEfektif,
            JumlahPenggantian: penggantian,
            EmisiKumulatifKg: kumulatif,
            VersiModel: cfg.VersiModel,
            FaktorPenuaanA: a,
            DihitungPadaUtc: dihitungPadaUtc);
    }
}