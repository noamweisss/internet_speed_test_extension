using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class MeterEasingTests
{
    [Fact]
    public void Next_FromZero_FirstTickMovesAMeaningfulShare()
    {
        var shown = ShownAfterOneTick(0, 100, live: true);

        Assert.InRange(shown!.Value, 25, 50);
    }

    [Fact]
    public void Next_NothingShownYet_StartsFromZero()
    {
        Assert.Equal(ShownAfterOneTick(0, 100, live: true), ShownAfterOneTick(null, 100, live: true));
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(0, 245.3)]
    [InlineData(0, 2500)]
    [InlineData(900, 12.5)]
    public void Next_Repeated_ReachesTargetExactlyWithinThreeSeconds(double start, double target)
    {
        double? shown = start;
        var ticks = 0;
        while (shown != target && ticks < 1000)
        {
            shown = ShownAfterOneTick(shown, target, live: true);
            ticks++;
        }

        Assert.Equal(target, shown);
        Assert.True(ticks * MeterEasing.TickMilliseconds <= 3000, $"took {ticks} ticks");
    }

    [Theory]
    [InlineData(0, 245.3)]
    [InlineData(245.3, 3.2)]
    public void Next_Repeated_MovesOneWayAndNeverOvershoots(double start, double target)
    {
        var rising = target > start;
        var previous = start;
        for (var tick = 0; tick < 100; tick++)
        {
            var next = ShownAfterOneTick(previous, target, live: true)!.Value;

            Assert.True(rising ? next >= previous && next <= target : next <= previous && next >= target, $"tick {tick}: {previous} -> {next}");
            previous = next;
        }
    }

    [Fact]
    public void Next_WithinOneThousandthOfTarget_SnapsToTarget()
    {
        // 0.2 short of 245.3 is under 0.1 % of it.
        Assert.Equal(245.3, ShownAfterOneTick(245.1, 245.3, live: true));
    }

    [Fact]
    public void Next_AtTarget_KeepsIt()
    {
        Assert.Equal(245.3, ShownAfterOneTick(245.3, 245.3, live: true));
    }

    [Fact]
    public void Next_NotLive_ShowsMeasurementExactly()
    {
        Assert.Equal(245.3, ShownAfterOneTick(10, 245.3, live: false));
    }

    [Fact]
    public void Next_Live_ShowsEasedValueBelowMeasurement()
    {
        var frame = MeterEasing.Next(new MeterFrame(10, 250), 245.3, live: true);

        Assert.InRange(frame.Shown!.Value, 10.001, 245.2);
    }

    [Fact]
    public void Next_PhaseEnds_ShowsFinalMeasurementExactly()
    {
        var frame = MeterFrame.Empty;
        for (var tick = 0; tick < 3; tick++)
        {
            frame = MeterEasing.Next(frame, 245.3, live: true);
        }

        Assert.NotEqual(245.3, frame.Shown);
        Assert.Equal(new MeterFrame(245.3, 250), MeterEasing.Next(frame, 245.3, live: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Next_NotMeasured_ResetsFrame(bool live)
    {
        Assert.Equal(MeterFrame.Empty, MeterEasing.Next(new MeterFrame(245.3, 250), null, live));
    }

    [Fact]
    public void Next_NewRunAfterReset_ScaleStartsAgainFromMeasurement()
    {
        var reset = MeterEasing.Next(new MeterFrame(900, 1000), null, live: false);

        Assert.Equal(10, MeterEasing.Next(reset, 9.5, live: true).Scale);
    }

    [Fact]
    public void Next_EasedValueBelowStep_ScaleFollowsMeasurement()
    {
        // Eased from 9 toward 30 the shown value stays under 25, but the 30 measured picks the 50 scale at once.
        var frame = MeterEasing.Next(new MeterFrame(9, 10), 30, live: true);

        Assert.True(frame.Shown < 25, $"shown {frame.Shown}");
        Assert.Equal(50, frame.Scale);
    }

    [Fact]
    public void Next_MeasurementFallsAcrossSteps_ScaleNeverShrinks()
    {
        var frame = MeterFrame.Empty;
        foreach (var measured in new[] { 120.0, 80, 9, 30 })
        {
            frame = MeterEasing.Next(frame, measured, live: true);

            Assert.Equal(250, frame.Scale);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Next_Settled_ReturnsEqualFrame(bool live)
    {
        // Equal frames draw the same markdown, so the page skips the redraw.
        var settled = new MeterFrame(245.3, 250);

        Assert.Equal(settled, MeterEasing.Next(settled, 245.3, live));
    }

    [Fact]
    public void Next_StillEasing_ReturnsDifferentFrame()
    {
        var easing = new MeterFrame(100, 250);

        Assert.NotEqual(easing, MeterEasing.Next(easing, 245.3, live: true));
    }

    private static double? ShownAfterOneTick(double? shown, double measured, bool live) =>
        MeterEasing.Next(new MeterFrame(shown, 0), measured, live).Shown;
}
