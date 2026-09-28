using System.Net.Http;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class ServerTimingTests
{
    [Theory]
    [InlineData("cfRequestDuration;dur=12.5", 12.5)]
    [InlineData("cfRequestDuration;dur=7", 7)]
    [InlineData("cfL4;desc=\"?proto=TCP\", cfRequestDuration;dur=3.25;desc=x", 3.25)]
    [InlineData("nothing-here", 0)]
    [InlineData("cfRequestDuration;dur=-4", 0)]
    public void DurationMs_ParsesDurToken(string header, double expected)
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Server-Timing", header);

        Assert.Equal(expected, ServerTiming.DurationMs(response.Headers));
    }

    [Fact]
    public void DurationMs_MultipleMetricsInOneValue_SumsEveryDur()
    {
        // The real probe response (observed 2026-09-28) lists edge and worker time as separate metrics.
        using var response = new HttpResponseMessage();
        response.Headers.Add("Server-Timing", "cfSpeedEdge;dur=4, cfSpeedWorker;dur=18");

        Assert.Equal(22, ServerTiming.DurationMs(response.Headers));
    }

    [Fact]
    public void DurationMs_SecondHeaderValue_IsIncluded()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Server-Timing", "cfL4;desc=\"?proto=TCP&rtt=1234\"");
        response.Headers.Add("Server-Timing", "cfSpeedEdge;dur=4");
        response.Headers.Add("Server-Timing", "cfSpeedWorker;dur=18.5");

        Assert.Equal(22.5, ServerTiming.DurationMs(response.Headers));
    }

    [Theory]
    [InlineData("cfSpeedEdge;dur=abc, cfSpeedWorker;dur=18", 18)]
    [InlineData("cfSpeedEdge;dur=-4, cfSpeedWorker;dur=18", 18)]
    [InlineData("cfSpeedEdge;dur=, cfSpeedWorker;dur=18", 18)]
    [InlineData("cfSpeedEdge;dur=1e400, cfSpeedWorker;dur=18", 18)]
    public void DurationMs_MalformedEntry_IsIgnored(string header, double expected)
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("Server-Timing", header);

        Assert.Equal(expected, ServerTiming.DurationMs(response.Headers));
    }

    [Fact]
    public void DurationMs_NoHeader_IsZero()
    {
        using var response = new HttpResponseMessage();

        Assert.Equal(0, ServerTiming.DurationMs(response.Headers));
    }
}
