namespace Wastu.Data.Tests;

// xUnit v2 tak punya skip dinamis, jadi dicek saat discovery.
[AttributeUsage(AttributeTargets.Method)]
public sealed class PerluSupabaseAttribute : FactAttribute
{
    public PerluSupabaseAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(KoneksiSupabase.NamaVariabelLingkungan)))
        {
            Skip = $"Uji integrasi butuh PostgreSQL. Set {KoneksiSupabase.NamaVariabelLingkungan} untuk menjalankannya.";
        }
    }
}
