using Wastu.Models;

namespace Wastu.Services;

public sealed record ItemKatalog(Bahan Bahan, JenisKomponen Jenis, string Gambar)
{
    public string Kode => "m" + Bahan.Id;
}

public sealed record KotaBawaan(double Lintang, double Bujur, double CurahHujan, double Kelembapan, double Suhu, double JarakPesisir);

public static class Katalog
{
    public const string LokasiLain = "Lokasi lain";

    public static readonly IReadOnlyDictionary<string, KotaBawaan> Kota = new Dictionary<string, KotaBawaan>
    {
        ["Yogyakarta"] = new(-7.7956, 110.3695, 2310, 81, 27.1, 26),
        ["Jakarta"] = new(-6.2088, 106.8456, 2200, 79, 28.4, 9),
        ["Bandung"] = new(-6.9175, 107.6191, 2050, 82, 23.6, 84),
        ["Semarang"] = new(-6.9667, 110.4167, 2400, 78, 28.2, 4),
        ["Surabaya"] = new(-7.2575, 112.7521, 1650, 76, 28.6, 8),
        ["Denpasar"] = new(-8.65, 115.2167, 1700, 79, 27.5, 6),
        ["Makassar"] = new(-5.1477, 119.4327, 2900, 81, 27.8, 4),
    };

    public static readonly string[] NamaKota = ["Yogyakarta", "Jakarta", "Bandung", "Semarang", "Surabaya", "Denpasar", "Makassar"];

    public static readonly IReadOnlyList<ItemKatalog> Semua = Bangun();

    public static ItemKatalog? Cari(string kode) =>
        kode.Length > 1 && int.TryParse(kode.AsSpan(1), out var i) && (uint)i < (uint)Semua.Count ? Semua[i] : null;

    public static ItemKatalog Dari(Bahan bahan) => Semua[bahan.Id];

    public static ProfilIklim ProfilBawaan(string kota, double jarakPesisir)
    {
        Kota.TryGetValue(kota, out var k);
        return new ProfilIklim
        {
            CurahHujanTahunan = k?.CurahHujan ?? 2200,
            KelembapanRata = k?.Kelembapan ?? 80,
            SuhuRata = k?.Suhu ?? 28,
            JarakPesisirKm = jarakPesisir,
            Sumber = "Profil lokasi"
        };
    }

    private static List<ItemKatalog> Bangun()
    {
        // nama, jenis, konduktivitas, lembap, korosi, usia, faktor emisi, gambar, sumber
        (string, JenisKomponen, double, int, int, int, double, string, string)[] data =
        [
            ("Bata Ringan AAC", JenisKomponen.Dinding, .16, 4, 5, 40, 112, "aac", "Referensi literatur"),
            ("Bata Merah", JenisKomponen.Dinding, .7, 3, 4, 40, 180, "brick", "Referensi literatur"),
            ("Batako Beton", JenisKomponen.Dinding, 1.1, 3, 4, 30, 160, "concrete", "Referensi literatur"),
            ("Panel Semen Fiber", JenisKomponen.Dinding, .35, 5, 5, 35, 140, "fiber", "Referensi literatur"),
            ("Bata Hebel Presisi", JenisKomponen.Dinding, .17, 4, 5, 40, 130, "aac", "Estimasi awal"),
            ("Bata Ekspos", JenisKomponen.Dinding, .75, 3, 4, 45, 170, "brick", "Estimasi awal"),
            ("Bata Keramik Berongga", JenisKomponen.Dinding, .35, 4, 4, 45, 120, "brick", "Estimasi awal"),
            ("Bata Kapur Pasir", JenisKomponen.Dinding, .9, 3, 4, 38, 145, "concrete", "Estimasi awal"),
            ("Panel GRC", JenisKomponen.Dinding, .4, 4, 5, 32, 120, "fiber", "Estimasi awal"),
            ("Blok Beton Ringan", JenisKomponen.Dinding, .3, 4, 4, 36, 145, "aac", "Estimasi awal"),
            ("Panel Beton Pracetak", JenisKomponen.Dinding, 1.4, 5, 5, 60, 260, "concrete", "Estimasi awal"),
            ("Dinding Tanah Padat", JenisKomponen.Dinding, .65, 2, 4, 38, 70, "brick", "Estimasi awal"),
            ("Bata Interlock", JenisKomponen.Dinding, .55, 3, 4, 40, 100, "brick", "Estimasi awal"),
            ("Panel Insulasi Sandwich", JenisKomponen.Dinding, .08, 5, 3, 30, 170, "fiber", "Estimasi awal"),
            ("Bata Beton Berpori", JenisKomponen.Dinding, .22, 4, 4, 35, 138, "aac", "Estimasi awal"),
            ("Genteng Tanah Liat", JenisKomponen.Atap, .7, 5, 5, 50, 95, "tile", "Referensi literatur"),
            ("Atap Metal Berlapis", JenisKomponen.Atap, 45, 5, 3, 35, 105, "metal", "Referensi literatur"),
            ("Genteng Beton", JenisKomponen.Atap, 1.5, 4, 5, 40, 180, "concrete", "Referensi literatur"),
            ("Atap Fiber Semen", JenisKomponen.Atap, .45, 4, 4, 35, 130, "fiber", "Referensi literatur"),
            ("Genteng Keramik Glasir", JenisKomponen.Atap, .9, 5, 5, 55, 155, "tile", "Estimasi awal"),
            ("Atap Zincalume", JenisKomponen.Atap, 50, 5, 3, 30, 120, "metal", "Estimasi awal"),
            ("Atap Aluminium", JenisKomponen.Atap, 205, 5, 4, 40, 140, "metal", "Estimasi awal"),
            ("Sirap Kayu Terlindungi", JenisKomponen.Atap, .14, 2, 2, 25, 70, "timber", "Estimasi awal"),
            ("Genteng Bitumen", JenisKomponen.Atap, .19, 4, 4, 30, 90, "metal", "Estimasi awal"),
            ("Panel Atap Sandwich", JenisKomponen.Atap, .07, 5, 4, 30, 175, "metal", "Estimasi awal"),
        ];

        var hasil = new List<ItemKatalog>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            var (nama, jenis, termal, lembap, korosi, usia, faktor, gambar, sumber) = data[i];
            hasil.Add(new ItemKatalog(new Bahan
            {
                Id = i,
                Nama = nama,
                KonduktivitasTermal = termal,
                KetahananLembap = lembap,
                KetahananKorosi = korosi,
                UsiaPakaiTahun = usia,
                FaktorEmisi = faktor,
                DataSource = sumber
            }, jenis, gambar));
        }
        return hasil;
    }
}
