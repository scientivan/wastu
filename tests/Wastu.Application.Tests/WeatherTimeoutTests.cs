using System.Net.Http;
using Wastu.Services;
using Xunit;

namespace Wastu.Application.Tests;

public sealed class WeatherTimeoutTests
{
    [Fact]
    public async Task HistoricalAndForecastTimeoutsAreBoundedAndReportedAsTimeouts()
    {
        using var http = new HttpClient(new WaitingHandler());
        var weather = new OpenMeteo(http, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAsync<TimeoutException>(() => weather.GetAnnualProfileAsync(-6.2, 106.8));
        await Assert.ThrowsAsync<TimeoutException>(() => weather.GetForecastWithMetadataAsync(-6.2, 106.8, 14));
    }

    [Fact]
    public async Task UserCancellationIsPreservedInsteadOfBeingConvertedToTimeout()
    {
        using var http = new HttpClient(new WaitingHandler());
        var weather = new OpenMeteo(http);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            weather.GetAnnualProfileAsync(-6.2, 106.8, cancellation.Token));
    }

    private sealed class WaitingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Expected cancellation.");
        }
    }
}
