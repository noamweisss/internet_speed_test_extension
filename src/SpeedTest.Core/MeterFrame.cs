namespace SpeedTest.Core;

/// <summary>
/// What one speed meter shows: <paramref name="Shown"/> is the eased value (null when not measured yet) and
/// <paramref name="Scale"/> the bar's maximum (0 when not measured yet). Built by <see cref="MeterEasing.Next"/>;
/// two equal frames draw the same bar and readout, so the page skips a meter whose frame is unchanged unless a
/// new snapshot arrived (the active marker follows the phase).
/// </summary>
public sealed record MeterFrame(double? Shown, double Scale)
{
    public static MeterFrame Empty { get; } = new(null, 0);
}
