using Npgsql;

namespace Wastu.Data;

public static class KoneksiSupabase
{
    public const string NamaVariabelLingkungan = "WASTU_CONNECTION_STRING";

    public static string Ambil()
        => Cari()
            ?? throw new InvalidOperationException(
                $"Isi {NamaVariabelLingkungan} atau host, port, database, user, dan password di .env.");

    public static string? Cari()
    {
        var nilai = KonfigurasiLingkungan.Baca();
        foreach (var nama in new[] { NamaVariabelLingkungan, "host", "port", "database", "user", "password" })
        {
            if (Environment.GetEnvironmentVariable(nama) is { Length: > 0 } lingkungan)
                nilai[nama] = lingkungan;
        }
        return Susun(nilai);
    }

    public static string? Susun(IReadOnlyDictionary<string, string> nilai)
    {
        if (nilai.TryGetValue(NamaVariabelLingkungan, out var lengkap) && !string.IsNullOrWhiteSpace(lengkap))
            return lengkap;

        var wajib = new[] { "host", "port", "database", "user", "password" };
        if (!wajib.Any(nama => nilai.TryGetValue(nama, out var isi) && !string.IsNullOrWhiteSpace(isi)))
            return null;

        var kurang = wajib.Where(nama => !nilai.TryGetValue(nama, out var isi) || string.IsNullOrWhiteSpace(isi)).ToArray();
        if (kurang.Length > 0)
            throw new InvalidOperationException("Konfigurasi database belum lengkap. Isi " + string.Join(", ", kurang) + ".");
        if (!int.TryParse(nilai["port"], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Port database harus berupa angka antara 1 dan 65535.");

        // Builder menjaga password dengan karakter khusus tetap utuh.
        return new NpgsqlConnectionStringBuilder
        {
            Host = nilai["host"],
            Port = port,
            Database = nilai["database"],
            Username = nilai["user"],
            Password = nilai["password"],
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
