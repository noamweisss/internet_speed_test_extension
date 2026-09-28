using System;
using System.Globalization;
using System.Net.Http.Headers;

namespace SpeedTest.Core;

/// <summary>
/// Reads the time Cloudflare spent handling a request from its Server-Timing headers, so latency reflects the
/// network and not the server. The real probe response splits that time over several metrics and header values
/// (observed 2026-09-28: "cfSpeedEdge;dur=4, cfSpeedWorker;dur=18" plus a "cfL4;desc=..." value), so every
/// "dur=" is summed. A malformed, negative or non-finite duration is ignored.
/// </summary>
public static class ServerTiming
{
    public static double DurationMs(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Server-Timing", out var values))
        {
            return 0;
        }

        double total = 0;
        foreach (var value in values)
        {
            var rest = value.AsSpan();
            int index;
            while ((index = rest.IndexOf("dur=", StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                rest = rest[(index + 4)..];
                var end = rest.IndexOfAny(';', ',');
                var token = end >= 0 ? rest[..end] : rest;
                if (double.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ms) && double.IsFinite(ms) && ms >= 0)
                {
                    total += ms;
                }
            }
        }

        return total;
    }
}
