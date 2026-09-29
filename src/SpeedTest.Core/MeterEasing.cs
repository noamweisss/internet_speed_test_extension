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
    /// How often the meter is redrawn while a test runs: 4 Hz, the top of the 2 to 4 Hz that docs/CMDPAL-RENDERING.md
    /// §15 option 4 gives an image meter. Every redraw recreates the bar image empty until it is decoded again (§5
    /// step 5), so fewer redraws mean fewer blank frames. 100 ms would be the floor for a text meter (§15 option 1).
    /// </summary>
    public const int TickMilliseconds = 250;

    /// <summary>
    /// Twice the tick, so each tick covers 39 % of the remaining distance (1 − e^−0.5): small enough steps to read as
    /// a glide at 4 Hz, and a new measurement is 86 % reached after one second. A 250 ms constant at this tick would
    /// cover 63 % per tick, which looks like the jumps this class exists to remove.
    /// </summary>
    private const double TimeConstantMilliseconds = 500;

    /// <summary>Below this remainder the readout cannot show the difference: 1 Kbps is its finest unit.</summary>
    private const double SnapAbsoluteMbps = 0.001;

    /// <summary>
    /// Below this share of the value the remainder is invisible on the bar (under 0.1 % of it). The readout may still
    /// see it: between 100 and 999 Mbps the last frame can move the readout's last two digits, up to 1 Mbps.
    /// </summary>
    private const double SnapRelative = 0.001;

    private static readonly double ShareOfDistancePerTick = 1 - Math.Exp(-TickMilliseconds / TimeConstantMilliseconds);

    /// <summary>
    /// The frame to show after one more tick. <paramref name="measured"/> is the meter's last measurement, null when
    /// not measured yet; <paramref name="live"/> is true while it is still being measured.
    /// <para>
    /// A null measurement gives <see cref="MeterFrame.Empty"/>. Every run starts with both values null, and a value
    /// never goes back to null within a run, so this is the reset between runs without tracking phases.
    /// </para>
    /// <para>
    /// Otherwise the shown value eases toward the measurement while live and equals it exactly when not, so a
    /// finished phase never lags. The scale comes from the measured value, not the eased one, and never shrinks
    /// (<see cref="SpeedFormatter.ScaleFor(double, double)"/>): an eased value crossing a step does not move it.
    /// </para>
    /// </summary>
    public static MeterFrame Next(MeterFrame previous, double? measured, bool live)
    {
        if (measured is not { } value)
        {
            return MeterFrame.Empty;
        }

        return new MeterFrame(Step(previous.Shown, value, live), SpeedFormatter.ScaleFor(value, previous.Scale));
    }

    /// <summary>The value to show after one more tick. A null <paramref name="shown"/> starts from 0.</summary>
    private static double Step(double? shown, double target, bool live)
    {
        if (!live)
        {
            return target;
        }

        var from = shown ?? 0;
        var next = from + ((target - from) * ShareOfDistancePerTick);
        var snapDistance = Math.Max(SnapAbsoluteMbps, SnapRelative * Math.Abs(target));
        return Math.Abs(target - next) <= snapDistance ? target : next;
    }
}
