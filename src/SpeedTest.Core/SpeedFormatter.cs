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

    /// <summary>
    /// Cells in the meter bar and its scale line. Constant, so the line never re-wraps as the value moves.
    /// </summary>
    public const int BarCells = 24;

    /// <summary>
    /// The meter bar, <see cref="BarCells"/> cells in half-cell steps: full cells "█", one "▌" when the remainder is
    /// at least half a cell, "░" for the rest, for example "███████████▌░░░░░░░░░░░░". The meter shows it in a fenced
    /// code block, which the host draws in Consolas (docs/CMDPAL-RENDERING.md §3). Consolas has these three block
    /// glyphs but not the eighth blocks "▏▎▍▋▊▉", which fall back to a narrower font and break the grid (ADR-0017).
    /// Rounds down, so the bar is full only at the scale. A value at or below 0, or no scale, draws an empty bar.
    /// </summary>
    public static string Bar(double value, double max)
    {
        // Multiply before dividing: 0.5 of 24 cells is exactly one half cell, not 0.999… of one.
        var halfCells = value > 0 && max > 0 ? (int)Math.Floor(Math.Min(value * BarCells * 2 / max, BarCells * 2)) : 0;
        var fullCells = halfCells / 2;
        var halfCell = halfCells % 2 == 1 ? "▌" : string.Empty;
        return new string('█', fullCells) + halfCell + new string('░', BarCells - fullCells - halfCell.Length);
    }

    /// <summary>
    /// The line under the bar, as wide as it: "0" at the left, the scale's end at the right, so the reader sees what a
    /// full bar means ("0               250 Mbps"). No scale yet (0) shows the "0" alone.
    /// </summary>
    public static string BarScale(double max)
    {
        var end = max <= 0 ? string.Empty
            : max >= 1000 ? (max / 1000).ToString("0.##", CultureInfo.InvariantCulture) + " Gbps"
            : max.ToString("0.##", CultureInfo.InvariantCulture) + " Mbps";
        return "0" + end.PadLeft(BarCells - 1);
    }

    /// <summary>The gauge maximum for a value: the next "round" scale above it, so the bar is always readable.</summary>
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
    /// this run: a scale that shrank would make the bar jump back when the value moves down across a step.
    /// </summary>
    public static double ScaleFor(double mbps, double atLeast) => Math.Max(atLeast, ScaleFor(mbps));
}
