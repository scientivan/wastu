namespace Wastu.Domain;

/// <summary>Jenis komponen bangunan yang dinilai; cerminan CHECK <c>komponen_jenis_enum</c>.</summary>
public enum JenisKomponen
{
    Dinding,
    Atap
}

/// <summary>Tingkat risiko korosi lokasi; cerminan CHECK <c>profil_iklim_tingkat_korosi_enum</c>.</summary>
public enum TingkatKorosi
{
    Rendah,
    Sedang,
    Tinggi
}