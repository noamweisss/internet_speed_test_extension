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

    /// <summary>Mean absolute difference between consecutive samples (the definition Ookla uses). 0 for fewer than two samples.</summary>
    public static double Jitter(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        double sum = 0;
        for (var i = 1; i < values.Count; i++)
        {
            sum += Math.Abs(values[i] - values[i - 1]);
        }

        return sum / (values.Count - 1);
    }

    /// <summary>Megabits per second (decimal, as ISPs advertise). 0 when no time has elapsed.</summary>
    public static double Mbps(long bytes, TimeSpan elapsed) =>
        elapsed <= TimeSpan.Zero ? 0 : bytes * 8.0 / elapsed.TotalSeconds / 1_000_000.0;
}
