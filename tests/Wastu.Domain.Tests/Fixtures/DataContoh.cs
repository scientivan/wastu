using Wastu.Domain;

namespace Wastu.Domain.Tests.Fixtures;

/// <summary>
/// Nilai contoh yang disalin apa adanya dari <c>database/03_seed_supabase.sql</c>.
/// </summary>
/// <remarks>
/// Dipakai sebagai jangkar regresi: bila skema data berubah, test ini ikut gagal dan
/// memberi tahu bahwa katalog serta data contoh perlu diselaraskan. Sengaja tidak
/// membaca berkas SQL langsung supaya lapisan pengujian tetap bebas dari parsing.
/// </remarks>
public static class DataContoh
{
    public static readonly Guid ProyekId = new("b0000000-0000-4000-8000-000000000001");
    public static readonly Guid KomponenDindingId = new("c0000000-0000-4000-8000-000000000001");
    public static readonly Guid KomponenAtapId = new("c0000000-0000-4000-8000-000000000002");

    /// <summary>Bahan dinding beton bertulang, misal seed baris pertama.</summary>
    public static readonly Guid IdBetonBertulang = new("d0000000-0000-4000-8000-000000000001");

    /// <summary>Bahan atap genting keramik.</summary>
    public static readonly Guid IdGentingKeramik = new("d0000000-0000-4000-8000-000000000018");

    /// <summary>Luas komponen dinding pada data contoh, m2.</summary>
    public const double LuasDinding = 120;

    /// <summary>Luas komponen atap pada data contoh, m2.</summary>
    public const double LuasAtap = 90;

    public static Bahan BetonBertulang() => new(
        id: IdBetonBertulang,
        nama: "Beton bertulang (in-situ)",
        jenis: JenisKomponen.Dinding,
        konduktivitasTermal: 1.40,
        ketahananLembap: 4,
        ketahananKorosi: 5,
        usiaPakaiNominalTahun: 50,
        faktorEmisi: 120,
        sumberData: "ICE Database v3 (Bath) — perkiraan literatur (PRD §11.4)");

    public static Bahan BetonRingan() => new(
        id: new Guid("d0000000-0000-4000-8000-000000000002"),
        nama: "Beton ringan (hebel autoclave)",
        jenis: JenisKomponen.Dinding,
        konduktivitasTermal: 0.12,
        ketahananLembap: 5,
        ketahananKorosi: 3,
        usiaPakaiNominalTahun: 30,
        faktorEmisi: 60,
        sumberData: "ICE Database v3 (Bath) — perkiraan literatur (PRD §11.4)");

    public static Bahan PanelGipsum() => new(
        id: new Guid("d0000000-0000-4000-8000-000000000009"),
        nama: "Panel gipsum (dry wall)",
        jenis: JenisKomponen.Dinding,
        konduktivitasTermal: 0.25,
        ketahananLembap: 1,
        ketahananKorosi: 1,
        usiaPakaiNominalTahun: 20,
        faktorEmisi: 28,
        sumberData: "ICE Database v3 (Bath) — perkiraan literatur (PRD §11.4)");

    public static Bahan GentingKeramik() => new(
        id: IdGentingKeramik,
        nama: "Genting keramik",
        jenis: JenisKomponen.Atap,
        konduktivitasTermal: 0.30,
        ketahananLembap: 5,
        ketahananKorosi: 5,
        usiaPakaiNominalTahun: 35,
        faktorEmisi: 110,
        sumberData: "ICE Database v3 (Bath) — perkiraan literatur (PRD §11.4)");

    /// <summary>Bahan atap baja galvanis, kasus pembanding pada seed simulasi emisi.</summary>
    public static readonly Guid IdBajaGalvanis = new("d0000000-0000-4000-8000-000000000021");

    public static Bahan BajaGalvanis() => new(
        id: IdBajaGalvanis,
        nama: "Baja galvanis (trapezium)",
        jenis: JenisKomponen.Atap,
        konduktivitasTermal: 0.05,
        ketahananLembap: 3,
        ketahananKorosi: 2,
        usiaPakaiNominalTahun: 15,
        faktorEmisi: 90,
        sumberData: "ICE Database v3 (Bath) — perkiraan literatur (PRD §11.4)");

    /// <summary>
    /// Skor kesesuaian yang tersimpan pada baris seed untuk beton bertulang. Seed
    /// memakai angka ini untuk menurunkan usia efektif 50 menjadi 43 tahun.
    /// </summary>
    public const double SkorSeedBetonBertulang = 72;

    /// <summary>Skor kesesuaian seed untuk baja galvanis, menurunkan 15 tahun menjadi 9,375.</summary>
    public const double SkorSeedBajaGalvanis = 25;

    public static Komponen Dinding() =>
        new(KomponenDindingId, ProyekId, JenisKomponen.Dinding, LuasDinding);

    public static Komponen Atap() =>
        new(KomponenAtapId, ProyekId, JenisKomponen.Atap, LuasAtap);

    /// <summary>
    /// Profil iklim demo: curah 2400 mm, kelembapan 82 persen, suhu 27,5 C,
    /// 3 km dari laut. Nilai ini membuat demand ketahanan lembap dan korosi sama-sama
    /// mencapai maksimum, jadi kasus yang baik untuk menguji kedua sub-skor.
    /// </summary>
    public static ProfilIklim IklimPesisirBandarLampung() => new(
        proyekId: ProyekId,
        curahHujanTahunanMm: 2400,
        kelembapanRataPct: 82,
        suhuRataC: 27.5,
        jarakPesisirKm: 3.0,
        tingkatKorosi: TingkatKorosi.Tinggi);

    /// <summary>Lokasi sejuk dan jauh dari laut, untuk menguji arah perubahan skor.</summary>
    public static ProfilIklim IklimDinginInland() => new(
        proyekId: ProyekId,
        curahHujanTahunanMm: 800,
        kelembapanRataPct: 55,
        suhuRataC: 18,
        jarakPesisirKm: 120,
        tingkatKorosi: TingkatKorosi.Rendah);

/// <summary>
    /// Varian profil iklim pesisir dengan semua iklim lain sama, dipakai untuk menguji
    /// pengaruh suhu terhadap pita termal tanpa variabel lain yang bergerak.
    /// </summary>
    public static ProfilIklim DenganSuhu(double suhuRataC)
    {
        ProfilIklim asal = IklimPesisirBandarLampung();
        return new ProfilIklim(
            asal.ProyekId,
            asal.CurahHujanTahunanMm,
            asal.KelembapanRataPct,
            suhuRataC,
            asal.JarakPesisirKm,
            asal.TingkatKorosi);
    }
}