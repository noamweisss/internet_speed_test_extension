using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class MeterEasingTests
{
    [Fact]
    public void Step_FromZero_FirstTickMovesAMeaningfulShare()
    {
        var shown = MeterEasing.Step(0, 100, live: true);

        Assert.InRange(shown!.Value, 25, 50);
    }

    [Fact]
    public void Step_NothingShownYet_StartsFromZero()
    {
        Assert.Equal(MeterEasing.Step(0, 100, live: true), MeterEasing.Step(null, 100, live: true));
    }

    [Fact]
    public void Step_OneSecondOfTicks_CoversMostOfTheDistance()
    {
        double? shown = 0;
        for (var elapsed = 0; elapsed < 1000; elapsed += MeterEasing.TickMilliseconds)
        {
            shown = MeterEasing.Step(shown, 100, live: true);
        }

        Assert.InRange(shown!.Value, 80, 95);
    }

    [Theory]
    [InlineData(0, 0.5, 4000)]
    [InlineData(0, 245.3, 4000)]
    [InlineData(0, 2500, 4000)]
    [InlineData(900, 12.5, 6000)] // a 70-fold drop takes longest: the snap distance shrinks with the target
    public void Step_Repeated_ReachesTargetExactlyInBoundedTime(double start, double target, int maxMilliseconds)
    {
        double? shown = start;
        var ticks = 0;
        while (shown != target && ticks < 1000)
        {
            shown = MeterEasing.Step(shown, target, live: true);
            ticks++;
        }

        Assert.Equal(target, shown);
        Assert.True(ticks * MeterEasing.TickMilliseconds <= maxMilliseconds, $"took {ticks} ticks");
    }

    [Theory]
    [InlineData(0, 245.3)]
    [InlineData(245.3, 3.2)]
    public void Step_Repeated_MovesOneWayAndNeverOvershoots(double start, double target)
    {
        var rising = target > start;
        var previous = start;
        for (var tick = 0; tick < 100; tick++)
        {
            var next = MeterEasing.Step(previous, target, live: true)!.Value;

            Assert.True(rising ? next >= previous && next <= target : next <= previous && next >= target, $"tick {tick}: {previous} -> {next}");
            previous = next;
        }
    }

    [Fact]
    public void Step_WithinReadoutPrecision_SnapsToTarget()
    {
        Assert.Equal(245.3, MeterEasing.Step(245.1, 245.3, live: true));
    }

    [Fact]
    public void Step_AtTarget_KeepsIt()
    {
        Assert.Equal(245.3, MeterEasing.Step(245.3, 245.3, live: true));
    }

    [Fact]
    public void Step_NotLive_ShowsTargetExactly()
    {
        Assert.Equal(245.3, MeterEasing.Step(10, 245.3, live: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Step_NotMeasuredYet_ShowsNothing(bool live)
    {
        Assert.Null(MeterEasing.Step(245.3, null, live));
    }
}
