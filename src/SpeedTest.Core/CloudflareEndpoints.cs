using System;

namespace SpeedTest.Core;

/// <summary>
/// Cloudflare's public speed-test endpoints (ADR-0002). This is the only host the extension talks to;
/// it must stay listed in scripts/allowed-hosts.txt (rule R7).
/// </summary>
public static class CloudflareEndpoints
{
    public static readonly Uri Base = new("https://speed.cloudflare.com/");

    /// <summary>JSON with the client's IP, ISP (asOrganization), city, country and serving data centre (colo).</summary>
    public static Uri Meta { get; } = new(Base, "meta");

    /// <summary>Returns <paramref name="bytes"/> bytes of filler. bytes=0 is used for latency probes.</summary>
    public static Uri Download(long bytes) => new(Base, $"__down?bytes={bytes}");

    /// <summary>Accepts a POST body of any size and discards it.</summary>
    public static Uri Upload { get; } = new(Base, "__up");
}
