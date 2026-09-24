using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class SpeedFormatterTests
{
    [Theory]
    [InlineData(null, "—")]
    [InlineData(0.5, "500 Kbps")]
    [InlineData(1.0, "1.0 Mbps")]
    [InlineData(245.34, "245.3 Mbps")]
    [InlineData(1250.0, "1.25 Gbps")]
    public void Speed_FormatsByMagnitude(double? mbps, string expected) => Assert.Equal(expected, SpeedFormatter.Speed(mbps));

    [Theory]
    [InlineData(null, "—")]
    [InlineData(12.34, "12.3 ms")]
    public void Latency_FormatsWithOneDecimal(double? ms, string expected) => Assert.Equal(expected, SpeedFormatter.Latency(ms));

    [Theory]
    [InlineData(0, 100, 10, "▱▱▱▱▱▱▱▱▱▱")]
    [InlineData(50, 100, 10, "▰▰▰▰▰▱▱▱▱▱")]
    [InlineData(100, 100, 10, "▰▰▰▰▰▰▰▰▰▰")]
    [InlineData(500, 100, 10, "▰▰▰▰▰▰▰▰▰▰")]
    [InlineData(-5, 100, 10, "▱▱▱▱▱▱▱▱▱▱")]
    [InlineData(5, 0, 10, "▱▱▱▱▱▱▱▱▱▱")]
    public void Bar_FillsProportionallyAndClamps(double value, double max, int width, string expected) =>
        Assert.Equal(expected, SpeedFormatter.Bar(value, max, width));

    [Fact]
    public void Bar_ZeroWidth_IsEmpty() => Assert.Equal(string.Empty, SpeedFormatter.Bar(5, 10, 0));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    [InlineData(11, 25)]
    [InlineData(240, 250)]
    [InlineData(900, 1000)]
    [InlineData(25_000, 30_000)]
    public void ScaleFor_PicksNextRoundScale(double mbps, double expected) => Assert.Equal(expected, SpeedFormatter.ScaleFor(mbps));
}
