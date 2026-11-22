using System.Net.Http;
using Wastu.Services;
using Xunit;

namespace Wastu.Application.Tests;

public sealed class WeatherTimeoutTests
{
    [Theory]
    [InlineData("2026-01-01T00:00:00Z", "2015-01-01", "2024-12-31")]
    [InlineData("2026-01-08T00:00:00Z", "2016-01-01", "2025-12-31")]
    public async Task HistoricalRequestUsesTenFullCalendarYearsAndExplicitArchiveDelay(string now, string start, string end)
    {
        var handler = new CaptureHandler();
        using var http = new HttpClient(handler);
        var client = new OpenMeteo(http, clock: new FixedClock(DateTimeOffset.Parse(now)));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAnnualProfileAsync(-6.2, 106.8));
        Assert.Contains("start_date=" + start, handler.Uri!.Query);
        Assert.Contains("end_date=" + end, handler.Uri.Query);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public Uri? Uri;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
        }
    }

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
