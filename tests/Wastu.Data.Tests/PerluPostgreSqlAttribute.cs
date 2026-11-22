namespace Wastu.Data.Tests;

/// <summary>
/// Menandai uji integrasi yang membutuhkan basis data PostgreSQL riil terisolasi.
/// Membaca koneksi HANYA dari variabel lingkungan WASTU_TEST_CONNECTION_STRING (PRD §16, AC WP01b).
/// Jika variabel lingkungan tidak diatur, tes diskip secara anggun.
/// Dilarang keras membaca KoneksiSupabase.Ambil() atau berkas .env pengguna!
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PerluPostgreSqlAttribute : FactAttribute
{
    public const string NamaVariabelLingkungan = "WASTU_TEST_CONNECTION_STRING";

    public PerluPostgreSqlAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(NamaVariabelLingkungan)))
        {
            Skip = $"Uji integrasi PostgreSQL butuh koneksi. Set {NamaVariabelLingkungan} untuk menjalankannya.";
        }
    }
}
