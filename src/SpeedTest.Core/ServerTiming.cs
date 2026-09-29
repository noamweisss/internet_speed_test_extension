using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Reads the time Cloudflare spent handling a request from its Server-Timing headers, so latency reflects the
/// network and not the server. The real probe response splits that time over several metrics and header values
/// (observed 2026-09-28: "cfSpeedEdge;dur=4, cfSpeedWorker;dur=18" plus a "cfL4;desc=..." value), so every
/// "dur" parameter is summed. Quoted descriptions are skipped before parsing, so a "dur=" inside one does not
/// count. A malformed, negative or implausibly large duration is ignored, and the sum is bounded the same way.
/// </summary>
public static class ServerTiming
{
    /// <summary>Longer than any probe can take (the HttpClient timeout is 30 s); a larger value is a bogus header.</summary>
    private const double MaxDurationMs = 60_000;

    public static double DurationMs(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Server-Timing", out var values))
        {
            return 0;
        }

        double total = 0;
        foreach (var value in values)
        {
            foreach (var metric in Unquoted(value).Split(','))
            {
                foreach (var parameter in metric.Split(';'))
                {
                    var trimmed = parameter.AsSpan().Trim();
                    if (trimmed.StartsWith("dur=", StringComparison.OrdinalIgnoreCase)
                        && double.TryParse(trimmed[4..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ms)
                        && ms >= 0 && ms <= MaxDurationMs)
                    {
                        total += ms;
                    }
                }
            }
        }

        return Math.Min(total, MaxDurationMs);
    }

    /// <summary>
    /// The header value without its quoted strings, which may hold anything, separators included. Inside quotes a
    /// backslash escapes the next character (an HTTP quoted-pair), so an escaped quote does not end the string.
    /// </summary>
    private static string Unquoted(string value)
    {
        var text = new StringBuilder(value.Length);
        var inQuotes = false;
        var escaped = false;
        foreach (var ch in value)
        {
            if (escaped)
            {
                escaped = false;
            }
            else if (inQuotes && ch == '\\')
            {
                escaped = true;
            }
            else if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                text.Append(ch);
            }
        }

        return text.ToString();
    }
}
