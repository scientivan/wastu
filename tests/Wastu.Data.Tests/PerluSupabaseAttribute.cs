namespace Wastu.Data.Tests;

// xUnit v2 tak punya skip dinamis, jadi dicek saat discovery.
[AttributeUsage(AttributeTargets.Method)]
public sealed class PerluSupabaseAttribute : FactAttribute
{
    public PerluSupabaseAttribute()
    {
        if (Environment.GetEnvironmentVariable("WASTU_ALLOW_SUPABASE_TESTS") != "1"
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WASTU_SUPABASE_TEST_SCHEMA"))
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(KoneksiSupabase.NamaVariabelLingkungan)))
        {
            Skip = "Uji Supabase memerlukan opt-in dan schema uji terisolasi melalui harness verifikasi.";
        }
    }
}
