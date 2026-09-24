using System;

namespace SpeedTest.Core;

/// <summary>
/// Tunables for one measurement. Defaults are the production values; tests use smaller ones.
/// Every value bounds either time or bytes, so a run can never exceed
/// LatencySamples requests + 2 × PhaseDuration of transfer.
/// </summary>
public sealed record SpeedTestOptions
{
    public static SpeedTestOptions Default { get; } = new();

    /// <summary>Number of zero-byte requests used to estimate latency and jitter.</summary>
    public int LatencySamples { get; init; } = 10;

    /// <summary>Wall-clock length of the download phase and of the upload phase.</summary>
    public TimeSpan PhaseDuration { get; init; } = TimeSpan.FromSeconds(8);

    /// <summary>Parallel HTTP streams during download. More streams saturate fast links better.</summary>
    public int DownloadStreams { get; init; } = 4;

    /// <summary>Parallel HTTP streams during upload.</summary>
    public int UploadStreams { get; init; } = 3;

    /// <summary>Bytes requested per download request. Requests repeat until the phase ends.</summary>
    public long DownloadRequestBytes { get; init; } = 50_000_000;

    /// <summary>Bytes sent per upload request. Requests repeat until the phase ends.</summary>
    public long UploadRequestBytes { get; init; } = 10_000_000;

    /// <summary>How often the live throughput is reported during a transfer phase.</summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(200);
}
