namespace Wastu.Data;

public static class KoneksiSupabase
{
    public const string NamaVariabelLingkungan = "WASTU_CONNECTION_STRING";

    // Pakai session pooler Supabase (port 5432) atau koneksi langsung, bukan
    // transaction pooler 6543 — mode transaksi memutus transaksi EF Core.
    public static string Ambil()
        => Environment.GetEnvironmentVariable(NamaVariabelLingkungan)
            ?? throw new InvalidOperationException(
                $"Set {NamaVariabelLingkungan} ke connection string PostgreSQL (session pooler, port 5432).");
}
