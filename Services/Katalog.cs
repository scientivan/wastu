using Wastu.Models;

namespace Wastu.Services;

public sealed record ItemKatalog(Bahan Bahan, JenisKomponen Jenis, string Gambar, Guid? DatabaseId = null)
{
    public string Kode => DatabaseId?.ToString("D") ?? "m" + Bahan.Id;

    internal Domain.Bahan DataDomain { get; } = new(DatabaseId ?? Guid.NewGuid(), Bahan.Nama,
        (Domain.JenisKomponen)Jenis, Bahan.KonduktivitasTermal, Bahan.KetahananLembap,
        Bahan.KetahananKorosi, Bahan.UsiaPakaiTahun, Bahan.FaktorEmisi, Bahan.DataSource,
        Bahan.SusunanKetebalan, 1, Bahan.SumberProperti);

    internal static ItemKatalog DariDomain(Domain.Bahan data, int index = 0)
    {
        var jenis = (JenisKomponen)data.Jenis;
        return new ItemKatalog(new Bahan
        {
            Id = index,
            Nama = data.Nama,
            KonduktivitasTermal = data.KonduktivitasTermal,
            KetahananLembap = data.KetahananLembap,
            KetahananKorosi = data.KetahananKorosi,
            UsiaPakaiTahun = data.UsiaPakaiNominalTahun,
            FaktorEmisi = data.FaktorEmisi,
            DataSource = data.SumberData,
            SusunanKetebalan = data.SusunanKetebalan,
            SumberProperti = data.SumberProperti ?? []
        }, jenis, TentukanGambar(data), data.Id);
    }

    internal static string TentukanGambar(Domain.Bahan data)
    {
        var nama = data.Nama.ToLowerInvariant();
        if (data.Jenis == Domain.JenisKomponen.Atap)
        {
            if (nama.Contains("beton")) return "concrete";
            if (nama.Contains("keramik") || nama.Contains("tanah liat") || nama.Contains("aspal")) return "tile";
            if (nama.Contains("baja") || nama.Contains("seng") || nama.Contains("sandwich") || nama.Contains("metal") || nama.Contains("zincalume") || nama.Contains("aluminium")) return "metal";
            if (nama.Contains("fiber") || nama.Contains("constantra")) return "fiber";
            if (nama.Contains("kayu") || nama.Contains("sirap")) return "timber";
            return "tile";
        }
        else
        {
            if (nama.Contains("ringan") || nama.Contains("hebel") || nama.Contains("silikat") || nama.Contains("aac")) return "aac";
            if (nama.Contains("bata merah") || nama.Contains("bata ekspos") || nama.Contains("interlock")) return "brick";
            if (nama.Contains("beton") || nama.Contains("batako") || nama.Contains("andesit") || nama.Contains("batu")) return "concrete";
            if (nama.Contains("gipsum") || nama.Contains("fiber") || nama.Contains("eps") || nama.Contains("semen")) return "fiber";
            if (nama.Contains("kayu") || nama.Contains("papan")) return "timber";
            if (nama.Contains("acp") || nama.Contains("aluminium") || nama.Contains("metal")) return "metal";
            return "concrete";
        }
    }
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

    public static ProfilIklim ProfilBawaan(string kota, double jarakPesisir)
    {
        Kota.TryGetValue(kota, out var k);
        return new ProfilIklim
        {
            CurahHujanTahunan = k?.CurahHujan ?? 2200,
            KelembapanRata = k?.Kelembapan ?? 80,
            SuhuRata = k?.Suhu ?? 28,
            JarakPesisirKm = jarakPesisir,
            Sumber = "Estimasi iklim bawaan"
        };
    }

    private static List<ItemKatalog> Bangun()
    {
        var sumber = Wastu.Data.KatalogSeed.BahanDomain;
        var hasil = new List<ItemKatalog>(sumber.Count);
        for (var i = 0; i < sumber.Count; i++)
        {
            hasil.Add(ItemKatalog.DariDomain(sumber[i], i));
        }
        return hasil;
    }
}
