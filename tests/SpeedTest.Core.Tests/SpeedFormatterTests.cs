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
    [InlineData(0, "—")]
    [InlineData(10, "10 Mbps")]
    [InlineData(250, "250 Mbps")]
    [InlineData(1000, "1 Gbps")]
    [InlineData(2500, "2.5 Gbps")]
    [InlineData(30_000, "30 Gbps")]
    public void ScaleLabel_RoundScale_HasNoTrailingZeros(double mbps, string expected) =>
        Assert.Equal(expected, SpeedFormatter.ScaleLabel(mbps));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    [InlineData(11, 25)]
    [InlineData(240, 250)]
    [InlineData(900, 1000)]
    [InlineData(25_000, 30_000)]
    public void ScaleFor_PicksNextRoundScale(double mbps, double expected) => Assert.Equal(expected, SpeedFormatter.ScaleFor(mbps));

    [Fact]
    public void ScaleFor_ValueCrossesAStep_Grows() => Assert.Equal(250, SpeedFormatter.ScaleFor(120, atLeast: 100));

    [Theory]
    [InlineData(9.5, 100)]
    [InlineData(0, 250)]
    public void ScaleFor_ValueFallsBelowAStep_NeverShrinks(double mbps, double atLeast) =>
        Assert.Equal(atLeast, SpeedFormatter.ScaleFor(mbps, atLeast));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(40, 0)]
    [InlineData(240, -5)]
    public void ScaleFor_NoPreviousScale_MatchesSingleValueForm(double mbps, double atLeast) =>
        Assert.Equal(SpeedFormatter.ScaleFor(mbps), SpeedFormatter.ScaleFor(mbps, atLeast));
}
