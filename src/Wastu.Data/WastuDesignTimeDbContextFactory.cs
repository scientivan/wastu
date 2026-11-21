using Microsoft.EntityFrameworkCore.Design;

namespace Wastu.Data;

public sealed class WastuDesignTimeDbContextFactory : IDesignTimeDbContextFactory<WastuDbContext>
{
    private const string ConnectionStringPengganti =
        "Host=localhost;Port=5432;Database=wastu;Username=postgres;Password=postgres";

    public WastuDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            KoneksiSupabase.Cari()
            ?? ConnectionStringPengganti;

        return new WastuDbContextFactory(connectionString).CreateDbContext();
    }
}
