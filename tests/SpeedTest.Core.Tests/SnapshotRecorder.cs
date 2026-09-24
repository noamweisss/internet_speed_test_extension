using System;
using System.Collections.Generic;

namespace SpeedTest.Core.Tests;

/// <summary>
/// Records progress reports inline and in order. Unlike <see cref="Progress{T}"/>, it does not post to a
/// SynchronizationContext or the thread pool, so tests see every report before MeasureAsync returns.
/// </summary>
internal sealed class SnapshotRecorder : IProgress<SpeedTestSnapshot>
{
    private readonly List<SpeedTestSnapshot> _snapshots = new();
    private readonly Action<SpeedTestSnapshot>? _onReport;

    public SnapshotRecorder(Action<SpeedTestSnapshot>? onReport = null)
    {
        _onReport = onReport;
    }

    public IReadOnlyList<SpeedTestSnapshot> Snapshots
    {
        get
        {
            lock (_snapshots)
            {
                return _snapshots.ToArray();
            }
        }
    }

    public void Report(SpeedTestSnapshot value)
    {
        lock (_snapshots)
        {
            _snapshots.Add(value);
        }

        _onReport?.Invoke(value);
    }
}
