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
    public void DurationMs_NoHeader_IsZero()
    {
        using var response = new HttpResponseMessage();

        Assert.Equal(0, ServerTiming.DurationMs(response.Headers));
    }
}
