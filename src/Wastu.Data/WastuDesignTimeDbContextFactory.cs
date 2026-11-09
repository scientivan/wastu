using Microsoft.EntityFrameworkCore.Design;

namespace Wastu.Data;

public sealed class WastuDesignTimeDbContextFactory : IDesignTimeDbContextFactory<WastuDbContext>
{
    // migrations add/script tidak butuh koneksi; database update butuh WASTU_CONNECTION_STRING.
    private const string ConnectionStringPengganti =
        "Host=localhost;Port=5432;Database=wastu;Username=postgres;Password=postgres";

    public WastuDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable(KoneksiSupabase.NamaVariabelLingkungan)
            ?? ConnectionStringPengganti;

        return new WastuDbContextFactory(connectionString).CreateDbContext();
    }
}
