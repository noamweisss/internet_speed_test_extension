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
    [InlineData(-5, 250)]
    [InlineData(0, 250)]
    [InlineData(0.3, 250)]
    [InlineData(123.4, 250)]
    [InlineData(250, 250)]
    [InlineData(9000, 250)]
    [InlineData(40, 0)]
    [InlineData(double.NaN, 250)]
    public void Bar_AnyValue_IsAlwaysBarCellsWide(double value, double max) =>
        Assert.Equal(SpeedFormatter.BarCells, SpeedFormatter.Bar(value, max).Length);

    [Theory]
    [InlineData(-5, 250)]
    [InlineData(0, 250)]
    [InlineData(40, 0)]
    public void Bar_NothingToShow_IsEmptyCells(double value, double max) =>
        Assert.Equal(new string('░', 24), SpeedFormatter.Bar(value, max));

    [Theory]
    [InlineData(250)]
    [InlineData(9000)]
    public void Bar_AtOrAboveScale_IsAllFullCells(double value) =>
        Assert.Equal(new string('█', 24), SpeedFormatter.Bar(value, 250));

    [Theory]
    [InlineData(1, "▏")]
    [InlineData(2, "▎")]
    [InlineData(3, "▍")]
    [InlineData(4, "▌")]
    [InlineData(5, "▋")]
    [InlineData(6, "▊")]
    [InlineData(7, "▉")]
    public void Bar_EighthsOfACell_EndWithMatchingPartialBlock(int eighths, string partialCell) =>
        Assert.Equal(partialCell + new string('░', 23), SpeedFormatter.Bar(eighths / 8.0, 24));

    [Fact]
    public void Bar_LessThanAnEighth_RoundsDownToNothing() =>
        Assert.Equal(new string('░', 24), SpeedFormatter.Bar(0.1, 24));

    [Theory]
    [InlineData(125, "████████████░░░░░░░░░░░░")]
    [InlineData(135, "████████████▉░░░░░░░░░░░")]
    [InlineData(245.3, "███████████████████████▌")]
    public void Bar_PartOfScale_FillsInEighthCells(double value, string expected) =>
        Assert.Equal(expected, SpeedFormatter.Bar(value, 250));

    [Fact]
    public void Bar_AnyValue_UsesOnlyBlockGlyphsCascadiaMonoHas()
    {
        // Cascadia Mono, the host's plain-text monospace font, has all of these at one width; Consolas has only
        // "█▌░" of them, so this bar must never be drawn in fenced code (ADR-0020).
        for (var value = 0.0; value <= 250; value += 0.7)
        {
            Assert.All(SpeedFormatter.Bar(value, 250), cell => Assert.Contains(cell, "█▉▊▋▌▍▎▏░"));
        }
    }

    // 9.5 alone would pick the 10 scale and fill 22.8 cells; the frame's 100 keeps the bar where the run left it.
    [Fact]
    public void BarWithScale_Measured_FillsAgainstFrameScaleWithScaleLineUnder() =>
        Assert.Equal("██▎░░░░░░░░░░░░░░░░░░░░░\n0               100 Mbps", SpeedFormatter.BarWithScale(new MeterFrame(9.5, 100)));

    [Fact]
    public void BarWithScale_NotMeasured_IsEmptyBarAndZero() =>
        Assert.Equal(new string('░', 24) + "\n0" + new string(' ', 23), SpeedFormatter.BarWithScale(MeterFrame.Empty));

    [Theory]
    [InlineData(10, "0                10 Mbps")]
    [InlineData(250, "0               250 Mbps")]
    [InlineData(1000, "0                 1 Gbps")]
    [InlineData(2500, "0               2.5 Gbps")]
    [InlineData(30_000, "0                30 Gbps")]
    public void BarScale_RightAlignsScaleEndUnderBar(double max, string expected)
    {
        Assert.Equal(expected, SpeedFormatter.BarScale(max));
        Assert.Equal(SpeedFormatter.BarCells, expected.Length);
    }

    [Fact]
    public void BarScale_NoScaleYet_ShowsZeroAlone() => Assert.Equal("0" + new string(' ', 23), SpeedFormatter.BarScale(0));

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
