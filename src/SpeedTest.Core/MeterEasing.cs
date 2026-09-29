using System;

namespace SpeedTest.Core;

/// <summary>
/// Moves the value a meter shows toward the last measurement, one display tick at a time, so the meter glides
/// between the 200 ms measurements instead of jumping (docs/CMDPAL-RENDERING.md §15 option 1): nothing in the host
/// interpolates. Exponential approach: every tick covers a fixed share of the remaining distance, which never
/// overshoots, and the last small remainder snaps so the meter reaches the value exactly after a finite number of ticks.
/// </summary>
public static class MeterEasing
{
    /// <summary>
    /// How often the meter is redrawn while a test runs: 250 ms, 4 Hz. The dial is an image, and the host recreates
    /// an image empty and fills it asynchronously on every rebuild of its block (docs/CMDPAL-RENDERING.md §5 step 5),
    /// so fewer rebuilds mean fewer blank frames; §15 option 4 gives 2 to 4 Hz for an image. 100 ms stays the floor
    /// for any design: the host shows only the last of two updates closer than its 40 ms batch window.
    /// </summary>
    public const int TickMilliseconds = 250;

    /// <summary>
    /// Two ticks. Each 250 ms frame covers 1 - e^-0.5, about 39 %, of the remaining distance, so the dial moves in
    /// small steps and is within 5 % of a new value after 1.5 s (three time constants). At 250 ms, one tick, a frame
    /// would cover 63 %, close to the jump easing is there to remove.
    /// </summary>
    private const double TimeConstantMilliseconds = 500;

    /// <summary>Below this remainder the readout cannot show the difference: 1 Kbps is its finest unit.</summary>
    private const double SnapAbsoluteMbps = 0.001;

    /// <summary>Below this share of the value the remainder is less than the readout's last digit.</summary>
    private const double SnapRelative = 0.001;

    private static readonly double ShareOfDistancePerTick = 1 - Math.Exp(-TickMilliseconds / TimeConstantMilliseconds);

    /// <summary>
    /// The value to show after one more tick. <paramref name="live"/> is true while <paramref name="target"/> is
    /// still being measured; otherwise the meter shows the target exactly, so a finished or new phase never lags.
    /// A null target (not measured yet) shows null; a null <paramref name="shown"/> starts from 0.
    /// </summary>
    public static double? Step(double? shown, double? target, bool live)
    {
        if (target is not { } goal || !live)
        {
            return target;
        }

        var from = shown ?? 0;
        var next = from + ((goal - from) * ShareOfDistancePerTick);
        var snapDistance = Math.Max(SnapAbsoluteMbps, SnapRelative * Math.Abs(goal));
        return Math.Abs(goal - next) <= snapDistance ? goal : next;
    }
}
