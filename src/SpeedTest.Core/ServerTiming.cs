using System;
using System.Globalization;
using System.Net.Http.Headers;

namespace SpeedTest.Core;

/// <summary>
/// Reads the time Cloudflare spent handling a request from its Server-Timing header
/// (e.g. "cfRequestDuration;dur=12.3"), so latency reflects the network and not the server.
/// </summary>
public static class ServerTiming
{
    public static double DurationMs(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Server-Timing", out var values))
        {
            return 0;
        }

        foreach (var value in values)
        {
            var index = value.IndexOf("dur=", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var span = value.AsSpan(index + 4);
            var end = span.IndexOfAny(';', ',');
            if (end >= 0)
            {
                span = span[..end];
            }

            if (double.TryParse(span.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ms) && ms >= 0)
            {
                return ms;
            }
        }

        return 0;
    }
}
