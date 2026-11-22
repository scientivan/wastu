namespace Wastu.Domain.Scoring;

/// <summary>
/// Nilai acuan default yang dipakai perhitungan sub-skor. Sejak PRD-R11 seluruh
/// parameter dapat diubah per versi model; kelas ini hanya menyediakan alias baca
/// ke <see cref="KonfigurasiModel.Bawaan"/> supaya kode lama dan dokumentasi tetap
/// menunjuk satu sumber kebenaran.
/// </summary>
/// <remarks>
/// <para>
/// PRD §11.1 menetapkan arah tiap sub-skor tetapi tidak pernah memberi rumus
/// normalisasinya. Semua angka di bawah karena itu adalah <b>asumsi kerja, bukan
/// hasil pengukuran</b> — persis hal yang PRD §11.4 dan §14 R2 mewajibkan
/// dinyatakan terbuka, bukan dikubur sebagai konstanta misterius.
/// </para>
/// <para>
/// Konvensi yang dipakai: iklim diubah menjadi <i>demand</i>, yaitu tingkat
/// ketahanan yang dibutuhkan lokasi, lalu dibandingkan dengan kemampuan bahan.
/// Demand berada di skala yang sama dengan indeks ketahanan bahan (1-5) sehingga
/// perbandingannya tidak perlu konversi.
/// </para>
/// </remarks>
public static class KonfigurasiSkoring
{
    /// <summary>Batas bawah konduktivitas ideal, W/mK; di bawahnya bahan tetap dianggap unggul.</summary>
    public static double PitaTermalBatasBawah => KonfigurasiModel.Bawaan.PitaTermalBatasBawah;

    /// <summary>Batas atas konduktivitas ideal saat lokasi sejuk, W/mK.</summary>
    public static double PitaTermalBatasAtasSejuk => KonfigurasiModel.Bawaan.PitaTermalBatasAtasSejuk;

    /// <summary>Batas atas konduktivitas ideal saat lokasi panas, W/mK.</summary>
    public static double PitaTermalBatasAtasPanas => KonfigurasiModel.Bawaan.PitaTermalBatasAtasPanas;

    /// <summary>Suhu rata-rata di bawah mana pita dianggap sejuk, derajat Celsius.</summary>
    public static double SuhuAwalBatasAtasTinggi => KonfigurasiModel.Bawaan.SuhuSejukC;

    /// <summary>Suhu rata-rata di atas mana pita dianggap panas, derajat Celsius.</summary>
    public static double SuhuAkhirBatasAtasRendah => KonfigurasiModel.Bawaan.SuhuPanasC;

    /// <summary>Curah hujan tahunan, mm, pada saat komponen hujan dari beban lembap mencapai maksimum.</summary>
    public static double AcuanCurahHujan => KonfigurasiModel.Bawaan.AcuanCurahHujanMm;

    /// <summary>Jarak ke pantai, km, pada saat permintaan ketahanan korosi sudah minimum.</summary>
    public static double AcuanJarakPesisir => KonfigurasiModel.Bawaan.AcuanJarakPesisirKm;

    /// <summary>Bobot komposisi hujan terhadap kelembapan pada beban lembap (0-1).</summary>
    public static double BobotLembapHujanAlpha => KonfigurasiModel.Bawaan.BobotLembapHujanAlpha;
}
