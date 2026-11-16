namespace Wastu.Domain.Emissions;

/// <summary>
/// Perhitungan murni simulasi emisi jangka panjang (PRD §11.3).
/// </summary>
/// <remarks>
/// <para><b>Asumsi mana yang terkonfirmasi dan mana yang tidak.</b></para>
/// <para>
/// Dua dari tiga rumus PRD §11.3 bisa diverifikasi silang terhadap
/// <c>database/03_seed_supabase.sql</c> dan keduanya benar:
/// penyesuaian usia <c>f(skor) = 0,5 + 0,5 × skor/100</c> dan akumulator
/// <c>emisi_awal × (1 + n)</c>.
/// </para>
/// <para><b>Rumus penggantian tidak konsisten di dalam PRD.</b> PRD menulis
/// <c>jumlah_penggantian = floor(horizon / usia_efektif)</c>, tetapi contoh
/// narasi di paragraf berikutnya hasilkan angka yang tidak bisa diperoleh dari
/// rumus itu:</para>
/// <list type="bullet">
/// <item><description>usia efektif 15 tahun, horizon 30 → narasi menyebut 1, rumus floor memberi 2.</description></item>
/// <item><description>usia efektif 9 tahun, horizon 30 → narasi menyebut 2, rumus floor memberi 3.</description></item>
/// </list>
/// <para>
/// Dua contoh itu saling bertentangan juga: tidak ada satu rumus yang
/// menghasilkan 1 dan 2 sekaligus dari <c>floor</c>. Yang menghasilkan
/// keduanya adalah <c>floor - 1</c>, tetapi itu bertentangan dengan kedua baris
/// seed yang sudah ada di repositori.</para>
/// <para><b>Yang dipakai di sini adalah <c>ceil(horizon / usia_efektif) - 1</c>.</b>
/// Alasannya, bukan karena 겸:</para>
/// <list type="number">
/// <item><description>Benar secara fisik. Bahan berumur 9 tahun dalam horizon 30 tahun
/// diganti pada tahun 9, 18, dan 27, jadi tiga kali. Narasi PRD yang menyebut dua
/// kali adalah kekeliruan aritmetika.</description></item>
/// <item><description>Sepakat dengan kedua baris seed yang sudah ada: baja galvanis
/// usia efektif 9,375 tahun memberi 3 penggantian, beton bertulang usia efektif
/// 43 tahun memberi 0. Seed itu milik skema §9 dan tidak boleh diubah dari sini,
/// jadi konsistensi dengannya paling penting.</description></item>
/// <item><description>Jumlah awal (pemasangan) bukan penggantian, sehingga horizon
/// yang tepat habis pada satu siklus tidak dihitung sebagai penggantian
/// tambahan. Ini yang membuat contoh narasi pertama menyebut 1.</description></item>
/// </list>
/// <para>
/// Kalau nanti PRD direvisi, seluruh perubahan cukup dilakukan di
/// <see cref="JumlahPenggantian"/> tanpa menyentuhservice maupun pemanggilnya.
/// </para>
/// </remarks>
public static class PerhitunganSimulasi
{
    /// <summary>Konstanta fungsi penyesuaian: bahan berskor 0 diasumsikan hanya bertahan separuh usia nominalnya.</summary>
    public const double FaktorDasar = 0.5;

    /// <summary>Rentang penyesuaian; bahan berskor 100 mempertahankan usia nominal penuh.</summary>
    public const double RentangFaktor = 0.5;

    /// <summary>
    /// Fungsi penyesuaian <c>f(skor) = 0,5 + 0,5 × (skor / 100)</c> (PRD §11.3).
    /// Mengembalikan nilai antara 0,5 dan 1.
    /// </summary>
    public static double FaktorPenyesuaian(double skorKesesuaian)
    {
        if (skorKesesuaian is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skorKesesuaian), skorKesesuaian,
                "Skor kesesuaian harus 0-100 (PRD §11.1 mendefinisikan rentangnya).");
        }

        return FaktorDasar + RentangFaktor * (skorKesesuaian / 100);
    }

    /// <summary>Usia pakai efektif = usia nominal × f(skor) (PRD §11.3).</summary>
    public static double UsiaPakaiEfektif(double usiaPakaiNominal, double skorKesesuaian)
    {
        if (usiaPakaiNominal <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiNominal), usiaPakaiNominal,
                "Usia pakai nominal harus lebih dari nol.");
        }

        return usiaPakaiNominal * FaktorPenyesuaian(skorKesesuaian);
    }

    /// <summary>
    /// Banyaknya penggantian yang benar-benar jatuh di dalam horizon, yaitu
    /// <c>ceil(horizon / usia_efektif) - 1</c>. Lihat catatan panjang pada
    /// <see cref="PerhitunganSimulasi"/> untuk alasan rumus ini dipakai dan
    /// ketidakkonsistenan PRD §11.3 yang melatari keputusan itu.
    /// </summary>
    public static int JumlahPenggantian(int horizonTahun, double usiaPakaiEfektif)
    {
        if (horizonTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonTahun), horizonTahun,
                "Horizon simulasi harus lebih dari nol tahun.");
        }

        if (usiaPakaiEfektif <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaPakaiEfektif), usiaPakaiEfektif,
                "Usia pakai efektif harus lebih dari nol.");
        }

        double jumlah = Math.Ceiling(horizonTahun / usiaPakaiEfektif) - 1;
        return (int)Math.Max(0, jumlah);
    }

    /// <summary>Emisi kumulatif = emisi_awal × (1 + jumlah_penggantian) (PRD §11.3).</summary>
    public static double EmisiKumulatif(double emisiAwal, int jumlahPenggantian)
    {
        if (emisiAwal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emisiAwal), emisiAwal, "Emisi awal tidak boleh negatif.");
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
    /// Menjalankan ketiga langkah simulasi sekaligus: penyesuaian usia, jumlah
    /// penggantian, lalu emisi kumulatif.
    /// </summary>
    /// <param name="evaluasiId">
    /// Id baris evaluasi asal. <see cref="Evaluasi"/> sendiri tidak membawa id-nya
    /// karena id baris adalah identitas tabel <c>evaluasi</c>, bukan bagian dari
    /// nilai yang dinilai.
    /// </param>
    public static SimulasiEmisi Simulasikan(
        Guid evaluasiId, Evaluasi evaluasi, double usiaPakaiNominal, int horizonTahun)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);

        double usiaEfektif = UsiaPakaiEfektif(usiaPakaiNominal,evaluasi.SkorKesesuaian);
        int penggantian = JumlahPenggantian(horizonTahun, usiaEfektif);

        return new SimulasiEmisi(
            EvaluasiId: evaluasiId,
            HorizonTahun: horizonTahun,
            UsiaPakaiEfektifTahun: usiaEfektif,
            JumlahPenggantian: penggantian,
            EmisiKumulatifKg: EmisiKumulatif(evaluasi.EmisiAwalKg, penggantian));
    }
}