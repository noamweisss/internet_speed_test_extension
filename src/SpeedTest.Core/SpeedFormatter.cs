using System;
using System.Globalization;

namespace SpeedTest.Core;

/// <summary>Turns numbers into the strings both views show. Invariant culture so tests are stable.</summary>
public static class SpeedFormatter
{
    public const string Unknown = "—";

    /// <summary>"980 Kbps", "245.3 Mbps", "1.25 Gbps".</summary>
    public static string Speed(double? mbps)
    {
        if (mbps is null)
        {
            return Unknown;
        }

        var value = mbps.Value;
        return value switch
        {
            >= 1000 => (value / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " Gbps",
            >= 1 => value.ToString("0.0", CultureInfo.InvariantCulture) + " Mbps",
            _ => (value * 1000).ToString("0", CultureInfo.InvariantCulture) + " Kbps",
        };
    }

    /// <summary>"12.3 ms".</summary>
    public static string Latency(double? ms) =>
        ms is null ? Unknown : ms.Value.ToString("0.0", CultureInfo.InvariantCulture) + " ms";

    /// <summary>The gauge maximum for a value: the next "round" scale above it, so the dial is always readable.</summary>
    public static double ScaleFor(double mbps)
    {
        foreach (var scale in new[] { 10.0, 25, 50, 100, 250, 500, 1000, 2500, 10000 })
        {
            if (mbps <= scale)
            {
                return scale;
            }
        }

        return Math.Ceiling(mbps / 10000) * 10000;
    }

    /// <summary>
    /// The gauge maximum for a value that must not fall below <paramref name="atLeast"/>, the scale already shown in
    /// this run: a scale that shrank would make the dial jump back when the value moves down across a step.
    /// </summary>
    public static double ScaleFor(double mbps, double atLeast) => Math.Max(atLeast, ScaleFor(mbps));
}
