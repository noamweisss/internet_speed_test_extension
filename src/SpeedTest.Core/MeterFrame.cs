namespace SpeedTest.Core;

/// <summary>
/// What one speed meter shows: <paramref name="Shown"/> is the eased value (null when not measured yet) and
/// <paramref name="Scale"/> the dial's maximum (0 when not measured yet). Built by <see cref="MeterEasing.Next"/>;
/// two equal frames draw the same markdown, so the page redraws a meter only when its frame changed.
/// </summary>
public sealed record MeterFrame(double? Shown, double Scale)
{
    public static MeterFrame Empty { get; } = new(null, 0);
}
