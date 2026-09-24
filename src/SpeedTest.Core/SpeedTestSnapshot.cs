using System;

namespace SpeedTest.Core;

/// <summary>
/// Everything known about a measurement at one moment. Reported through IProgress during the run;
/// the final snapshot (Phase == Complete) is the result. Null means "not measured yet".
/// </summary>
public sealed record SpeedTestSnapshot
{
    public static SpeedTestSnapshot Idle { get; } = new();

    public SpeedTestPhase Phase { get; init; } = SpeedTestPhase.Idle;

    public ConnectionInfo Connection { get; init; } = ConnectionInfo.Empty;

    public double? LatencyMs { get; init; }

    public double? JitterMs { get; init; }

    /// <summary>Live value during the download phase, final value afterwards.</summary>
    public double? DownloadMbps { get; init; }

    /// <summary>Live value during the upload phase, final value afterwards.</summary>
    public double? UploadMbps { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>User-facing reason when Phase == Failed.</summary>
    public string? Error { get; init; }

    public bool IsRunning => Phase is SpeedTestPhase.Connecting or SpeedTestPhase.Latency or SpeedTestPhase.Download or SpeedTestPhase.Upload;
}
