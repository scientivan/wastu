using Wastu.Domain;
using Wastu.Domain.Services;

namespace Wastu.Domain.Tests.Fakes;

/// <summary>
/// Catat antagonistis climate analysis untuk memeriksa apakah pemicunya benar-benar
/// terjadi.
/// </summary>
/// <remarks>
/// <c>ProjectService.UpdateLocationAsync</c> wajib memanggil
/// <see cref="RefreshProfileAsync"/>, karena PRD §F1 menyatakan perubahan lokasi
/// memicu pengambilan ulang profil iklim. Kalau pemanggilan itu hilang, alur tetap
/// compiles dan test lain tetap hijau, hanya Valuable yang diam-diam jadi salah.
/// </remarks>
internal sealed class IklimServicePerekam : IClimateAnalysisService
{
    public int JumlahRefresh { get; private set; }

    public Guid? RefreshTerakhir { get; private set; }

    public ProfilIklim? HasilRefresh { get; set; }

    public Task<ProfilIklim> GetOrFetchProfileAsync(Guid proyekId, CancellationToken ct = default)
        => Task.FromResult(HasilRefresh ?? new ProfilIklim(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi));

    public Task<ProfilIklim> RefreshProfileAsync(Guid proyekId, CancellationToken ct = default)
    {
        JumlahRefresh++;
        RefreshTerakhir = proyekId;
        return Task.FromResult(HasilRefresh ?? new ProfilIklim(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi));
    }
}