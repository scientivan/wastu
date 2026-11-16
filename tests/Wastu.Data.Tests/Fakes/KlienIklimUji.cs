using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.Data.Tests.Fakes;

internal sealed class KlienIklimUji : IHistoricalClimateClient
{
    public Task<ProfilIklim> GetAnnualProfileAsync(
        double lat, double lon, CancellationToken ct = default)
        => Task.FromResult(new ProfilIklim(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi));
}
