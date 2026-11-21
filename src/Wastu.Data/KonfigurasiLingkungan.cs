namespace Wastu.Data;

public static class KonfigurasiLingkungan
{
    // Cari dari direktori kerja, lalu direktori executable, hingga akar masing-masing.
    // File rahasia tidak disalin ke output build atau publish.
    public static Dictionary<string, string> Baca()
    {
        foreach (var awal in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var folder = new DirectoryInfo(awal); folder is not null; folder = folder.Parent)
            {
                var path = Path.Combine(folder.FullName, ".env");
                if (File.Exists(path))
                    return Parse(File.ReadLines(path));
            }
        }
        return new(StringComparer.Ordinal);
    }

    public static Dictionary<string, string> Parse(IEnumerable<string> baris)
    {
        var hasil = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mentah in baris)
        {
            var teks = mentah.Trim();
            if (teks.Length == 0 || teks.StartsWith('#')) continue;
            if (teks.StartsWith("export ", StringComparison.Ordinal)) teks = teks[7..].TrimStart();
            var pemisah = teks.IndexOf('=');
            if (pemisah <= 0)
                throw new InvalidOperationException("Format .env tidak valid. Gunakan NAMA=nilai pada setiap baris.");
            var nama = teks[..pemisah].Trim();
            var nilai = teks[(pemisah + 1)..].Trim();
            if (nilai.Length > 0 && nilai[0] is '\'' or '"')
            {
                var akhir = nilai.IndexOf(nilai[0], 1);
                if (akhir < 0 || (nilai[(akhir + 1)..].Trim() is { Length: > 0 } sisa && !sisa.StartsWith('#')))
                    throw new InvalidOperationException("Tanda kutip atau komentar pada .env tidak valid.");
                nilai = nilai[1..akhir];
            }
            else
            {
                var komentar = nilai.IndexOf(" #", StringComparison.Ordinal);
                if (komentar >= 0) nilai = nilai[..komentar].TrimEnd();
            }
            hasil[nama] = nilai;
        }
        return hasil;
    }

    public static void MuatPreferensi()
    {
        var nilai = Baca();
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WASTU_DATA_DIR")) &&
            nilai.TryGetValue("WASTU_DATA_DIR", out var folder) && !string.IsNullOrWhiteSpace(folder))
            Environment.SetEnvironmentVariable("WASTU_DATA_DIR", folder);
    }
}
