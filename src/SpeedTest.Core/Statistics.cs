using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeedTest.Core;

/// <summary>Pure maths used by the measurer. Kept separate so it is trivially unit-tested.</summary>
public static class Statistics
{
    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Need at least one value.", nameof(values));
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    /// <summary>
    /// Median of the absolute differences between consecutive samples. 0 for fewer than two samples.
    /// Median rather than mean (the Ookla definition, used until ADR-0010): one isolated slow sample, for example from
    /// process scheduling in the extension's windowless host, moves two consecutive differences. The mean reported
    /// those as network jitter; the median ignores them.
    /// </summary>
    public static double Jitter(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var differences = new double[values.Count - 1];
        for (var i = 1; i < values.Count; i++)
        {
            differences[i - 1] = Math.Abs(values[i] - values[i - 1]);
        }

        return Median(differences);
    }

    /// <summary>Megabits per second (decimal, as ISPs advertise). 0 when no time has elapsed.</summary>
    public static double Mbps(long bytes, TimeSpan elapsed) =>
        elapsed <= TimeSpan.Zero ? 0 : bytes * 8.0 / elapsed.TotalSeconds / 1_000_000.0;
}
