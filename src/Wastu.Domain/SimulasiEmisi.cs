namespace Wastu.Domain;

/// <summary>
/// Hasil simulasi emisi horizon panjang untuk satu evaluasi (PRD §11.3).
/// </summary>
/// <remarks>
/// <see cref="JumlahPenggantian"/> adalah inti argumen Wastu: bahan yang tidak cocok
/// dengan iklim setempat memiliki <see cref="UsiaPakaiEfektifTahun"/> lebih pendek,
/// sehingga emisi produksi terulang lebih sering dalam horizon yang sama.
/// </remarks>
/// <param name="EvaluasiId">Evaluasi asal simulasi.</param>
/// <param name="HorizonTahun">Horizon simulasi, tahun.</param>
/// <param name="UsiaPakaiEfektifTahun">Usia pakai setelah penyesuaian iklim.</param>
/// <param name="JumlahPenggantian">Berapa kali bahan harus diganti dalam horizon.</param>
/// <param name="EmisiKumulatifKg">Emisi kumulatif horizon, kgCO2e.</param>
public sealed record SimulasiEmisi(
    Guid EvaluasiId,
    int HorizonTahun,
    double UsiaPakaiEfektifTahun,
    int JumlahPenggantian,
    double EmisiKumulatifKg);