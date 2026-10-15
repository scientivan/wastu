using System.Globalization;

namespace Wastu.Domain.Scoring;

/// <summary>
/// Perhitungan murni untuk skor kesesuaian iklim dan emisi awal (PRD §11.1, §11.2).
/// </summary>
/// <remarks>
/// Sengaja dipisah dari <see cref="SuitabilityScoringService"/> dan dibiarkan statis:
/// seluruh isi fungsi ini deterministik dari nilai masukannya, jadi tidak butuh
/// database maupun klien apa pun untuk diuji.
/// </remarks>
public static class PerhitunganSkor
{
    /// <summary>Ambang sub-skor yang masih dianggap memadai untuk tidak disebut sebagai alasan.</summary>
    private const double AmbangAdequat = 60;

    /// <summary>
    /// Sub-skor termal: jarak konduktivitas bahan terhadap pita ideal lokasi (PRD §11.1).
    /// </summary>
    /// <remarks>
    /// Pita ideal bergeser dengan suhu rata-rata lokasi, karena PRD §11.1 menulis
    /// "rentang ideal untuk suhu rata-rata lokasi" dan "konduktivitas terlalu tinggi
    /// pada suhu rata-rata tinggi membuat skor turun". Bahan di dalam pita mendapat
    /// 100; di luar pita skor meluruh proporsional terhadap seberapa jauh dari tepi pita.
    /// </remarks>
    public static double SkorTermal(Bahan bahan, ProfilIklim iklim)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);

        double batasAtas = BatasAtasPitaTermal(iklim.SuhuRataC);
        double batasBawah = KonfigurasiSkoring.PitaTermalBatasBawah;
        double lambda = bahan.KonduktivitasTermal;

        double skor;
        if (lambda < batasBawah)
        {
            skor = 100 * batasBawah / lambda;
        }
        else if (lambda > batasAtas)
        {
            skor = 100 * batasAtas / lambda;
        }
        else
        {
            skor = 100;
        }

        return Kunci(skor);
    }

    /// <summary>
    /// Sub-skor lembap: ketahanan lembap bahan terhadap beban hujan dan kelembapan lokasi.
    /// </summary>
    /// <remarks>
    /// Beban iklim dijumlahkan dari dua sumber lalu dipetakan ke tingkat ketahanan
    /// yang dibutuhkan. Beban tidak dijumlahkan mentah ke indeks 1-5 karena curah
    /// hujan dan kelembapan memakai satuan berbeda, dan penjumlahan keduanya
    /// menghasilkan angka yang tidak bermakna tanpa acuan.
    /// </remarks>
    public static double SkorLembap(Bahan bahan, ProfilIklim iklim)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);

        double beban = KunciNolSampaiSatu(
            iklim.CurahHujanTahunanMm / KonfigurasiSkoring.AcuanCurahHujan +
            iklim.KelembapanRataPct / 100);

        double demand = 1 + 4 * beban;
        return Kunci(100 * bahan.KetahananLembap / demand);
    }

    /// <summary>
    /// Sub-skor korosi: ketahanan korosi bahan terhadap kedekatan lokasi ke pesisir.
    /// </summary>
    /// <remarks>
    /// Jarak ke laut dipakai sebagai proksi risiko udara bergaram, bukan pengukuran
    /// salinitas (PRD §14 R5). Menempel di laut berarti demand maksimum; jauh di
    /// inland berarti ketahanan korosi tidak lagi membedakan bahan, sesuai arah
    /// PRD §11.1 bahwa semakin dekat pesisir semakin dominan bobotnya.
    /// </remarks>
    public static double SkorKorosi(Bahan bahan, ProfilIklim iklim)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);

        double jarakEfektif = Math.Min(iklim.JarakPesisirKm, KonfigurasiSkoring.AcuanJarakPesisir);
        double demand = 1 + 4 * (1 - jarakEfektif / KonfigurasiSkoring.AcuanJarakPesisir);
        return Kunci(100 * bahan.KetahananKorosi / demand);
    }

    /// <summary>
    /// Sub-skor usia: usia pakai nominal dinormalkan terhadap acuan 60 tahun.
    /// </summary>
    /// <remarks>
    /// Alasan memakai acuan absolut dan bukan min-max dijelaskan panjang pada
    /// <see cref="KonfigurasiSkoring.AcuanUsiaPakai"/>.
    /// </remarks>
    public static double SkorUsia(Bahan bahan)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        return Kunci(100 * bahan.UsiaPakaiNominalTahun / KonfigurasiSkoring.AcuanUsiaPakai);
    }

    /// <summary>Menjumlahkan keempat sub-skor dengan bobot yang diberikan (PRD §11.1).</summary>
    public static double SkorKesesuaian(SubSkorKesesuaian subSkor, KriteriaBobot bobot)
    {
        ArgumentNullException.ThrowIfNull(subSkor);
        ArgumentNullException.ThrowIfNull(bobot);

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
        return bahan.FaktorEmisi * komponen.LuasM2;
    }

    /// <summary>Keempat sub-skor sekaligus; dipakai orkestrasi dan ditampilkan ke pengguna.</summary>
    public static SubSkorKesesuaian HitungSubSkor(Bahan bahan, ProfilIklim iklim)
    {
        return new SubSkorKesesuaian(
            SkorTermal(bahan, iklim),
            SkorLembap(bahan, iklim),
            SkorKorosi(bahan, iklim),
            SkorUsia(bahan));
    }

    /// <summary>
    /// Menyusun kalimat alasan untuk persona non-teknis (PRD §11.1, §4.1).
    /// </summary>
    /// <remarks>
    /// Hanya sub-skor di bawah <see cref="AmbangAdequat"/> yang disebut, supaya alasan
    /// menjelaskan kelemahan bahan dan bukan sekadar mengulang angka yang sudah tampil.
    /// Penulisan memakai kultur id-ID agar angka desimal memakai koma, konsisten
    /// dengan dokumen proyek.
    /// </remarks>
    public static string SusunAlasan(Bahan bahan, ProfilIklim iklim, SubSkorKesesuaian subSkor)
    {
        ArgumentNullException.ThrowIfNull(bahan);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(subSkor);

        CultureInfo budaya = CultureInfo.GetCultureInfo("id-ID");
        var alasan = new List<string>();

        if (subSkor.Termal < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Konduktivitas termal {0} W/mK termasuk tinggi untuk suhu rata-rata {1} C di lokasi Anda",
                bahan.KonduktivitasTermal, iklim.SuhuRataC));
        }

        if (subSkor.Lembap < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan lembap {0} di bawah kebutuhan lokasi yang curah hujannya {1} mm per tahun dengan kelembapan {2} persen",
                bahan.KetahananLembap, iklim.CurahHujanTahunanMm, iklim.KelembapanRataPct));
        }

        if (subSkor.Korosi < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Ketahanan korosi {0} rendah untuk lokasi yang berjarak {1} km dari garis pantai sehingga terkena udara bergaram",
                bahan.KetahananKorosi, iklim.JarakPesisirKm));
        }

        if (subSkor.Usia < AmbangAdequat)
        {
            alasan.Add(string.Format(budaya,
                "Usia pakai nominal {0} tahun lebih pendek dari acuan {1} tahun sehingga bahan perlu diganti lebih sering",
                bahan.UsiaPakaiNominalTahun, KonfigurasiSkoring.AcuanUsiaPakai));
        }

        if (alasan.Count == 0)
        {
            return "Sifat termal, ketahanan lembap dan korosi, serta usia pakainya sudah sesuai dengan iklim lokasi.";
        }

        return string.Join("; ", alasan) + ".";
    }

    /// <summary>
    /// Batas atas pita konduktivitas untuk suhu tertentu, W/mK. Berinterpolasi linear
    /// di antara suhu sejuk dan panas, lalu dijepit di kedua ujungnya.
    /// </summary>
    private static double BatasAtasPitaTermal(double suhuRata)
    {
        double awal = KonfigurasiSkoring.SuhuAwalBatasAtasTinggi;
        double akhir = KonfigurasiSkoring.SuhuAkhirBatasAtasRendah;

        if (suhuRata <= awal)
        {
            return KonfigurasiSkoring.PitaTermalBatasAtasSejuk;
        }

        if (suhuRata >= akhir)
        {
            return KonfigurasiSkoring.PitaTermalBatasAtasPanas;
        }

        double rasio = (suhuRata - awal) / (akhir - awal);
        return KonfigurasiSkoring.PitaTermalBatasAtasSejuk
             + (rasio * (KonfigurasiSkoring.PitaTermalBatasAtasPanas - KonfigurasiSkoring.PitaTermalBatasAtasSejuk));
    }

    /// <summary>Menjepit nilai ke rentang 0-100 supaya skor tidak pernah keluar dari skala tampilan.</summary>
    private static double Kunci(double nilai) => Math.Clamp(nilai, 0, 100);

    /// <summary>
    /// Menjepit nilai ke rentang 0-1. Terpisah dari <see cref="Kunci"/> karena keduanya
    /// jenuh 100: memakai <see cref="Kunci"/> untuk besaran 0-1 akan membiarkan 1,62
    /// lolos tanpa terpotong dan merusak perhitungan demand dihosokan.
    /// </summary>
    private static double KunciNolSampaiSatu(double nilai) => Math.Clamp(nilai, 0, 1);
}