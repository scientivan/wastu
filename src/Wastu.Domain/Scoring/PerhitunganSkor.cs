using System.Globalization;

namespace Wastu.Domain.Scoring;

/// <summary>
/// Perhitungan murni untuk skor kesesuaian iklim dan emisi awal (PRD §11.1, §11.2).
/// </summary>
/// <remarks>
/// Sengaja dipisah dari <see cref="SuitabilityScoringService"/> dan dibiarkan statis:
/// seluruh isi fungsi ini deterministik dari nilai masukannya, jadi tidak butuh
/// database maupun klien apa pun untuk diuji. Parameter model diterima lewat
/// <see cref="KonfigurasiModel"/> agar rumus dapat diverifikasi per versi (PRD-R11).
/// </remarks>
public static class PerhitunganSkor
{
    /// <summary>Ambang sub-skor yang masih dianggap memadai untuk tidak disebut sebagai kelemahan.</summary>
    private const double AmbangAdequat = 60;

    /// <summary>
    /// Sub-skor termal: jarak konduktivitas bahan terhadap pita ideal lokasi (PRD §11.1).
    /// </summary>
    /// <remarks>
    /// Pita ideal bergeser dengan suhu rata-rata lokasi, karena PRD §11.1 menulis
    /// "rentang ideal untuk suhu rata-rata lokasi" dan "konduktivitas terlalu tinggi
    /// pada suhu rata-rata tinggi membuat skor turun". Bahan isolator di bawah batas
    /// bawah tetap 100; bahan di dalam pita 100; di atas pita skor meluruh proporsional
    /// terhadap seberapa jauh dari tepi pita.
    /// </remarks>
    public static double SkorTermal(Bahan bahan, ProfilIklim iklim)
        => SkorTermal(bahan, iklim, KonfigurasiModel.Bawaan);

    /// <summary>Sub-skor termal dengan parameter model eksplisit.</summary>
    public static double SkorTermal(Bahan bahan, ProfilIklim iklim, KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(konfigurasi);

        PastikanBahanValid(bahan);
        PastikanIklimValid(iklim);
        PastikanKonfigurasiValid(konfigurasi);

        double batasBawah = konfigurasi.PitaTermalBatasBawah;
        double batasAtas = BatasAtasPitaTermal(iklim.SuhuRataC, konfigurasi);
        double lambda = bahan.KonduktivitasTermal;

        double skor = lambda < batasBawah || lambda <= batasAtas
            ? 100
            : 100 * batasAtas / lambda;

        return Kunci(skor);
    }

    /// <summary>
    /// Sub-skor lembap: ketahanan lembap bahan terhadap beban hujan dan kelembapan lokasi.
    /// </summary>
    /// <remarks>
    /// Beban adalah komposit terbobot dua normalisasi terpisah: curah hujan tahunan
    /// terhadap acuan hujan, dan kelembapan relatif terhadap 100 persen. Penjumlahan
    /// mentah keduanya (rumus lama) membuat beban jenuh terlalu dini sehingga hujan
    /// 700 dan 3.000 mm/tahun pada RH 80 persen memberi skor yang sama (PRD-R04).
    /// </remarks>
    public static double SkorLembap(Bahan bahan, ProfilIklim iklim)
        => SkorLembap(bahan, iklim, KonfigurasiModel.Bawaan);

    /// <summary>Sub-skor lembap dengan parameter model eksplisit.</summary>
    public static double SkorLembap(Bahan bahan, ProfilIklim iklim, KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(konfigurasi);

        PastikanBahanValid(bahan);
        PastikanIklimValid(iklim);
        PastikanKonfigurasiValid(konfigurasi);

        double r = Math.Clamp(iklim.CurahHujanTahunanMm / konfigurasi.AcuanCurahHujanMm, 0, 1);
        double h = Math.Clamp(iklim.KelembapanRataPct / 100.0, 0, 1);
        double beban = (konfigurasi.BobotLembapHujanAlpha * r)
                     + ((1.0 - konfigurasi.BobotLembapHujanAlpha) * h);
        double kebutuhan = 1.0 + (4.0 * beban);

        return Kunci(100.0 * bahan.KetahananLembap / kebutuhan);
    }

    /// <summary>
    /// Sub-skor korosi: ketahanan korosi bahan terhadap kedekatan lokasi ke pesisir.
    /// </summary>
    /// <remarks>
    /// Jarak ke laut dipakai sebagai proksi risiko udara bergaram, bukan pengukuran
    /// salinitas (PRD §14 R5). Rumus memakai jarak kontinu dan tidak membaca label
    /// tekstual <see cref="ProfilIklim.TingkatKorosi"/>; label itu tetap tersedia
    /// untuk tampilan, tetapi tidak boleh mengubah hasil skor.
    /// </remarks>
    public static double SkorKorosi(Bahan bahan, ProfilIklim iklim)
        => SkorKorosi(bahan, iklim, KonfigurasiModel.Bawaan);

    /// <summary>Sub-skor korosi dengan parameter model eksplisit.</summary>
    public static double SkorKorosi(Bahan bahan, ProfilIklim iklim, KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(konfigurasi);

        PastikanBahanValid(bahan);
        PastikanIklimValid(iklim);
        PastikanKonfigurasiValid(konfigurasi);

        double acuan = konfigurasi.AcuanJarakPesisirKm;
        double jarakEfektif = Math.Min(iklim.JarakPesisirKm, acuan);
        double kebutuhan = 1.0 + (4.0 * (1.0 - (jarakEfektif / acuan)));

        return Kunci(100.0 * bahan.KetahananKorosi / kebutuhan);
    }

    /// <summary>Normalisasi usia pakai nominal terhadap rentang usia bahan sejenis (PRD §11.1).</summary>
    /// <remarks>
    /// Rentang wajib berasal dari seluruh bahan valid pada kategori yang sama, bukan
    /// dari bahan yang sedang dipilih atau disaring di UI. Bila semua usia sama,
    /// seluruh bahan dianggap setara dan mendapat 100.
    /// </remarks>
    public static double SkorUsia(Bahan bahan, double usiaMin, double usiaMaks)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        PastikanBahanValid(bahan);

        if (!double.IsFinite(usiaMin) || usiaMin < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaMin), usiaMin, "Usia minimum kategori harus bernilai finite non-negatif.");
        }

        if (!double.IsFinite(usiaMaks) || usiaMaks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaMaks), usiaMaks, "Usia maksimum kategori harus bernilai finite non-negatif.");
        }

        if (usiaMaks < usiaMin)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usiaMaks), usiaMaks, "Usia maksimum kategori tidak boleh lebih kecil dari usia minimum.");
        }

        return usiaMin == usiaMaks
            ? 100
            : Kunci(100 * (bahan.UsiaPakaiNominalTahun - usiaMin) / (usiaMaks - usiaMin));
    }

    /// <summary>Menjumlahkan keempat sub-skor dengan bobot yang diberikan (PRD §11.1).</summary>
    /// <remarks>
    /// Bobot divalidasi ulang di titik pemakaian karena <c>with</c> pada record dapat
    /// melewati validasi konstruktor. Sub-skor juga harus finite 0-100 supaya hasil
    /// akhir tidak pernah menjadi NaN atau keluar skala tampilan.
    /// </remarks>
    public static double SkorKesesuaian(SubSkorKesesuaian subSkor, KriteriaBobot bobot)
    {
        ArgumentNullException.ThrowIfNull(subSkor);
        ArgumentNullException.ThrowIfNull(bobot);

        bobot.PastikanValid();
        PastikanSubSkorValid(subSkor);

        double skor = (bobot.Termal * subSkor.Termal)
                   + (bobot.Lembap * subSkor.Lembap)
                   + (bobot.Korosi * subSkor.Korosi)
                   + (bobot.Usia * subSkor.Usia);

        return Kunci(skor);
    }

    /// <summary>Emisi awal = faktor_emisi dikali luas_area, kgCO2e (PRD §11.2).</summary>
    public static double EmisiAwal(Bahan bahan, Komponen komponen)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(komponen);

        PastikanBahanValid(bahan);
        PastikanKomponenValid(komponen);

        double emisi = bahan.FaktorEmisi * komponen.LuasM2;
        if (!double.IsFinite(emisi) || emisi < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(komponen), emisi, "Emisi awal harus bernilai finite non-negatif.");
        }

        return emisi;
    }

    /// <summary>Keempat sub-skor sekaligus; dipakai orkestrasi dan ditampilkan ke pengguna.</summary>
    public static SubSkorKesesuaian HitungSubSkor(Bahan bahan, ProfilIklim iklim, double usiaMin, double usiaMaks)
        => HitungSubSkor(bahan, iklim, usiaMin, usiaMaks, KonfigurasiModel.Bawaan);

    /// <summary>Keempat sub-skor sekaligus dengan parameter model eksplisit.</summary>
    public static SubSkorKesesuaian HitungSubSkor(
        Bahan bahan,
        ProfilIklim iklim,
        double usiaMin,
        double usiaMaks,
        KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(konfigurasi);

        return new SubSkorKesesuaian(
            SkorTermal(bahan, iklim, konfigurasi),
            SkorLembap(bahan, iklim, konfigurasi),
            SkorKorosi(bahan, iklim, konfigurasi),
            SkorUsia(bahan, usiaMin, usiaMaks));
    }

    /// <summary>
    /// Menyusun kalimat alasan untuk persona non-teknis (PRD §11.1, §4.1).
    /// </summary>
    /// <remarks>
    /// Setiap bahan mendapat penjelasan, baik kekuatan maupun kelemahan, supaya
    /// pengguna tidak perlu memilih bahan lebih dulu untuk memahami hasilnya (F4-03).
    /// Istilah teknis diterjemahkan ke bahasa sehari-hari, dan alasan korosi selalu
    /// menyebut jarak pesisir sebagai proksi risiko udara bergaram.
    /// </remarks>
    public static string SusunAlasan(Bahan bahan, ProfilIklim iklim, SubSkorKesesuaian subSkor)
        => SusunAlasan(bahan, iklim, subSkor, KonfigurasiModel.Bawaan);

    /// <summary>Menyusun alasan dengan parameter model eksplisit.</summary>
    public static string SusunAlasan(
        Bahan bahan,
        ProfilIklim iklim,
        SubSkorKesesuaian subSkor,
        KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(subSkor);
        ArgumentNullException.ThrowIfNull(konfigurasi);

        PastikanBahanValid(bahan);
        PastikanIklimValid(iklim);
        PastikanSubSkorValid(subSkor);
        PastikanKonfigurasiValid(konfigurasi);

        CultureInfo budaya = CultureInfo.GetCultureInfo("id-ID");
        double batasAtas = BatasAtasPitaTermal(iklim.SuhuRataC, konfigurasi);
        var alasan = new List<string>(4);

        if (subSkor.Termal < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Konduktivitas termal {0} W/mK membuat bahan ini mudah menghantarkan panas pada suhu rata-rata {1} C di lokasi Anda",
                bahan.KonduktivitasTermal, iklim.SuhuRataC));
        }
        else if (bahan.KonduktivitasTermal > batasAtas)
        {
            alasan.Add(string.Format(budaya,
                "Konduktivitas termal {0} W/mK masih di atas pita ideal {1} W/mK untuk suhu rata-rata {2} C, tetapi selisihnya belum signifikan",
                bahan.KonduktivitasTermal, batasAtas, iklim.SuhuRataC));
        }
        else
        {
            alasan.Add(string.Format(budaya,
                "Konduktivitas termal {0} W/mK tergolong rendah sehingga bahan ini tidak mudah menghantarkan panas pada suhu rata-rata {1} C",
                bahan.KonduktivitasTermal, iklim.SuhuRataC));
        }

        if (subSkor.Lembap < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan lembap {0} kurang sesuai dengan curah hujan {1} mm per tahun dan kelembapan {2} persen di lokasi ini",
                bahan.KetahananLembap, iklim.CurahHujanTahunanMm, iklim.KelembapanRataPct));
        }
        else
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan lembap {0} memadai untuk curah hujan {1} mm per tahun dan kelembapan {2} persen",
                bahan.KetahananLembap, iklim.CurahHujanTahunanMm, iklim.KelembapanRataPct));
        }

        if (subSkor.Korosi < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan korosi {0} rendah untuk lokasi berjarak {1} km dari pantai; jarak ke pantai sebagai proksi risiko udara bergaram",
                bahan.KetahananKorosi, iklim.JarakPesisirKm));
        }
        else
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan korosi {0} memadai pada jarak {1} km dari pantai; jarak ke pantai sebagai proksi risiko udara bergaram",
                bahan.KetahananKorosi, iklim.JarakPesisirKm));
        }

        if (subSkor.Usia < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Usia pakai nominal {0} tahun relatif pendek untuk kategori bahan ini",
                bahan.UsiaPakaiNominalTahun));
        }
        else
        {
            alasan.Add(string.Format(budaya,
                "Usia pakai nominal {0} tahun tergolong panjang untuk kategori bahan ini",
                bahan.UsiaPakaiNominalTahun));
        }

        return string.Join("; ", alasan) + ".";
    }

    /// <summary>
    /// Batas atas pita konduktivitas untuk suhu tertentu, W/mK. Berinterpolasi linear
    /// di antara suhu sejuk dan panas, lalu dijepit di kedua ujungnya.
    /// </summary>
    private static double BatasAtasPitaTermal(double suhuRata, KonfigurasiModel konfigurasi)
    {
        if (suhuRata <= konfigurasi.SuhuSejukC)
        {
            return konfigurasi.PitaTermalBatasAtasSejuk;
        }

        if (suhuRata >= konfigurasi.SuhuPanasC)
        {
            return konfigurasi.PitaTermalBatasAtasPanas;
        }

        double rasio = (suhuRata - konfigurasi.SuhuSejukC)
                     / (konfigurasi.SuhuPanasC - konfigurasi.SuhuSejukC);
        return konfigurasi.PitaTermalBatasAtasSejuk
             + (rasio * (konfigurasi.PitaTermalBatasAtasPanas - konfigurasi.PitaTermalBatasAtasSejuk));
    }

    /// <summary>Memastikan bahan tetap valid meski dibuat ulang lewat <c>with</c>.</summary>
    internal static void PastikanBahanValid(Bahan bahan)
    {
        ArgumentNullException.ThrowIfNull(bahan);

        if (!double.IsFinite(bahan.KonduktivitasTermal) || bahan.KonduktivitasTermal <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bahan), bahan.KonduktivitasTermal,
                "Konduktivitas termal bahan harus bernilai finite positif.");
        }

        if (bahan.KetahananLembap is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bahan), bahan.KetahananLembap,
                "Indeks ketahanan lembap harus 1-5.");
        }

        if (bahan.KetahananKorosi is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bahan), bahan.KetahananKorosi,
                "Indeks ketahanan korosi harus 1-5.");
        }

        if (!double.IsFinite(bahan.UsiaPakaiNominalTahun) || bahan.UsiaPakaiNominalTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bahan), bahan.UsiaPakaiNominalTahun,
                "Usia pakai nominal bahan harus bernilai finite positif.");
        }

        if (!double.IsFinite(bahan.FaktorEmisi) || bahan.FaktorEmisi < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bahan), bahan.FaktorEmisi,
                "Faktor emisi bahan harus bernilai finite non-negatif.");
        }
    }

    /// <summary>Memastikan profil iklim tetap finite dan wajar meski dibuat ulang lewat <c>with</c>.</summary>
    internal static void PastikanIklimValid(ProfilIklim iklim)
    {
        ArgumentNullException.ThrowIfNull(iklim);

        if (!double.IsFinite(iklim.CurahHujanTahunanMm) || iklim.CurahHujanTahunanMm < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iklim), iklim.CurahHujanTahunanMm,
                "Curah hujan tahunan harus bernilai finite non-negatif.");
        }

        if (!double.IsFinite(iklim.KelembapanRataPct) || iklim.KelembapanRataPct is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iklim), iklim.KelembapanRataPct,
                "Kelembapan rata-rata harus bernilai finite 0-100 persen.");
        }

        if (!double.IsFinite(iklim.SuhuRataC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(iklim), iklim.SuhuRataC,
                "Suhu rata-rata harus bernilai finite.");
        }

        if (!double.IsFinite(iklim.JarakPesisirKm) || iklim.JarakPesisirKm < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iklim), iklim.JarakPesisirKm,
                "Jarak pesisir harus bernilai finite non-negatif.");
        }
    }

    /// <summary>Memastikan komponen tetap valid meski dibuat ulang lewat <c>with</c>.</summary>
    internal static void PastikanKomponenValid(Komponen komponen)
    {
        ArgumentNullException.ThrowIfNull(komponen);

        if (!double.IsFinite(komponen.LuasM2) || komponen.LuasM2 <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(komponen), komponen.LuasM2,
                "Luas komponen harus bernilai finite positif.");
        }
    }

    /// <summary>
    /// Memastikan parameter model tetap valid meski dibuat ulang lewat <c>with</c>.
    /// Tanpa ini, acuan nol atau alpha di luar 0-1 dapat menghasilkan skor NaN.
    /// </summary>
    internal static void PastikanKonfigurasiValid(KonfigurasiModel konfigurasi)
    {
        ArgumentNullException.ThrowIfNull(konfigurasi);

        if (!double.IsFinite(konfigurasi.AcuanCurahHujanMm) || konfigurasi.AcuanCurahHujanMm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.AcuanCurahHujanMm,
                "Acuan curah hujan harus bernilai finite positif.");
        }

        if (!double.IsFinite(konfigurasi.AcuanJarakPesisirKm) || konfigurasi.AcuanJarakPesisirKm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.AcuanJarakPesisirKm,
                "Acuan jarak pesisir harus bernilai finite positif.");
        }

        if (!double.IsFinite(konfigurasi.BobotLembapHujanAlpha)
            || konfigurasi.BobotLembapHujanAlpha is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.BobotLembapHujanAlpha,
                "Bobot lembap hujan alpha harus bernilai finite antara 0 dan 1.");
        }

        if (!double.IsFinite(konfigurasi.PitaTermalBatasBawah) || konfigurasi.PitaTermalBatasBawah < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.PitaTermalBatasBawah,
                "Pita termal batas bawah harus bernilai finite non-negatif.");
        }

        if (!double.IsFinite(konfigurasi.PitaTermalBatasAtasSejuk)
            || konfigurasi.PitaTermalBatasAtasSejuk < konfigurasi.PitaTermalBatasBawah)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.PitaTermalBatasAtasSejuk,
                "Pita termal batas atas sejuk harus >= batas bawah.");
        }

        if (!double.IsFinite(konfigurasi.PitaTermalBatasAtasPanas)
            || konfigurasi.PitaTermalBatasAtasPanas < konfigurasi.PitaTermalBatasBawah)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.PitaTermalBatasAtasPanas,
                "Pita termal batas atas panas harus >= batas bawah.");
        }

        if (!double.IsFinite(konfigurasi.SuhuSejukC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.SuhuSejukC,
                "Suhu sejuk harus bernilai finite.");
        }

        if (!double.IsFinite(konfigurasi.SuhuPanasC) || konfigurasi.SuhuPanasC <= konfigurasi.SuhuSejukC)
        {
            throw new ArgumentOutOfRangeException(
                nameof(konfigurasi), konfigurasi.SuhuPanasC,
                "Suhu panas harus bernilai finite dan lebih besar dari suhu sejuk.");
        }
    }

    private static void PastikanSubSkorValid(SubSkorKesesuaian subSkor)
    {
        ValidasiSubSkor(subSkor.Termal, nameof(subSkor.Termal));
        ValidasiSubSkor(subSkor.Lembap, nameof(subSkor.Lembap));
        ValidasiSubSkor(subSkor.Korosi, nameof(subSkor.Korosi));
        ValidasiSubSkor(subSkor.Usia, nameof(subSkor.Usia));
    }

    private static void ValidasiSubSkor(double nilai, string nama)
    {
        if (!double.IsFinite(nilai) || nilai is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nama, nilai, "Sub-skor harus bernilai finite antara 0 dan 100.");
        }
    }

    /// <summary>Menjepit nilai ke rentang 0-100 supaya skor tidak pernah keluar dari skala tampilan.</summary>
    private static double Kunci(double nilai) => Math.Clamp(nilai, 0, 100);
}
