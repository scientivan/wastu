namespace Wastu.Domain.Scoring;

/// <summary>
/// Konstanta acuan yang dipakai <see cref="SuitabilityScoringService"/> untuk
/// menormalisasi sub-skor ke rentang 0-100.
/// </summary>
/// <remarks>
/// <para>
/// PRD §11.1 menetapkan arah tiap sub-skor tetapi tidak pernah memberi rumus
/// normalisasinya, dan menyebut "rentang ideal" tanpa angka. Semua angka di bawah
/// karena itu adalah <b>asumsi kerja, bukan hasil pengukuran</b> — persis hal yang
/// PRD §11.4 dan §14 R2 mewajibkan dinyatakan terbuka, bukan dikubur sebagai
/// konstanta misterius.
/// </para>
/// <para>
/// Konvensi yang dipakai di sini: iklim diubah menjadi <i>demand</i>, yaitu
/// tingkat ketahanan yang dibutuhkan lokasi, lalu dibandingkan dengan kemampuan
/// bahan. Demand berada di skala yang sama dengan indeks ketahanan bahan (1-5)
/// sehingga perbandingannya tidak perlu konversi.
/// </para>
/// <para>
/// Konvensi ini menghasilkan rentang yang bisa dibaca: bahan dengan ketahanan
/// yang memenuhi permintaan lokasi mendapat 100, dan bahan setengah memenuhi
/// mendapatSeparuh nilai.
/// </para>
/// </remarks>
public static class KonfigurasiSkoring
{
    /// <summary>
    /// Batas bawah konduktivitas ideal, W/mK. Konstan karena konduktivitas yang
    /// terlalu rendah bukan kegagalan; bahan berisolasi lebih baik tetap layak.
    /// </summary>
    public const double PitaTermalBatasBawah = 0.10;

    /// <summary>
    /// Batas atas konduktivitas ideal saat lokasi sejuk, W/mK. Asumsi berbasis
    /// praktik dinding di iklim tropis, bukan hasil pengukuran.
    /// </summary>
    public const double PitaTermalBatasAtasSejuk = 0.60;

    /// <summary>
    /// Batas atas konduktivitas ideal saat lokasi panas, W/mK. Turun dari
    /// <see cref="PitaTermalBatasAtasSejuk"/> karena lokasi panas memaafkan
    /// konduktivitas lebih tinggi, sesuai arah PRD §11.1: konduktivitas terlalu
    /// tinggi pada suhu rata-rata tinggi membuat skor turun.
    /// </summary>
    public const double PitaTermalBatasAtasPanas = 0.30;

    /// <summary>
    /// Suhu rata-rata di bawah mana pita dianggap sejuk, derajat Celsius.
    /// </summary>
    public const double SuhuAwalBatasAtasTinggi = 26.0;

    /// <summary>
    /// Suhu rata-rata di atas mana pita dianggap panas, derajat Celsius. Di antara
    /// kedua nilai ini batas atas pita berinterpolasi linear.
    /// </summary>
    public const double SuhuAkhirBatasAtasRendah = 32.0;

    /// <summary>
    /// Curah hujan tahunan, mm, pada saat permintaan ketahanan lembap sudah maksimum.
    /// Asumsi: 3000 mm per tahun adalah batas atas yang wajar untuk wilayah dengan
    /// hujan sangat tinggi. Di atas angka ini demand tidak bertambah lagi karena
    /// indeks bahan sudah berhenti di 1-5 dan tidak bisa membedakan lebih jauh.
    /// </summary>
    public const double AcuanCurahHujan = 3000;

    /// <summary>
    /// Jarak ke pantai, km, pada saat permintaan ketahanan korosi sudah minimum.
    /// Di inland lebih jauh dari ini risiko udara bergaram dianggap tidak relevan
    /// lagi. Ini proksi geometris, bukan pengukuran salinitas (PRD §14 R5).
    /// </summary>
    public const double AcuanJarakPesisir = 50;

    /// <summary>
    /// Usia pakai, tahun, yang dianggap penuh; dipakai sebagai pembagi sub-skor usia.
    /// </summary>
    /// <remarks>
    /// PRD §11.1 menulis "dinormalisasi relatif terhadap bahan lain di kategori yang
    /// sama", yang dibaca sebagai min-max. Min-max ditolak karena skornya jadi
    /// bergantung pada isi katalog: bahan terburuk selalu tepat 0 dan terbaik
    /// selalu tepat 100, sehingga menambah satu bahan baru menggeser skor semua bahan
    /// lain. Karena skor usia ikut masuk ke usia pakai efektif di §11.3, itu membuat
    /// angka karbon yang jadi sorotan utama aplikasi bergantung pada katalog, bukan
    /// pada bahannya, dan merusak argumen inti Wastu. Nilai hasil tetap monoton dan
    /// sebanding dengan rentang 0-100 yang ditampilkan ke pengguna.
    /// </remarks>
    public const double AcuanUsiaPakai = 60;
}