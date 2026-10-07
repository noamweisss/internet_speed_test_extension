using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Renders the markdown blocks of the meter dashboard (ADR-0017, ADR-0020), one string per block of the extension's
/// MeterPage. The page keeps each block as its own content because the host rebuilds a markdown block whole whenever
/// its Body changes (docs/CMDPAL-RENDERING.md §5): a live value must not re-parse the static text.
/// Pure functions, so the layout is unit-tested here.
/// </summary>
public static class MeterMarkdown
{
    public const string DownloadTitle = "⬇ Download";

    public const string UploadTitle = "⬆ Upload";

    /// <summary>
    /// Title, status, and latency: they change per phase, not per sample. The latency readout is H2, not H3: the host
    /// renders H3 at 12 px normal weight, smaller than body text (docs/CMDPAL-RENDERING.md §3).
    /// </summary>
    public static string Header(SpeedTestSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.Append("# Internet Speed Test\n\n");
        text.Append(StatusLine(snapshot)).Append("\n\n");
        text.Append("## ⏱ Latency").Append(ActiveMarker(snapshot.Phase == SpeedTestPhase.Latency))
            .Append("\n\n## ").Append(SpeedFormatter.Latency(snapshot.LatencyMs));
        if (snapshot.JitterMs is not null)
        {
            text.Append("  ·  jitter ").Append(SpeedFormatter.Latency(snapshot.JitterMs));
        }

        return text.Append('\n').ToString();
    }

    /// <summary>
    /// One speed meter's title (with the active marker) and readout, drawn from <paramref name="frame"/>
    /// (<see cref="MeterEasing.Next"/>), whose shown value may be eased. The readout is H2, not H3, for the reason
    /// given on <see cref="Header"/>. The bar is not here: it sits in its own plain-text block under this one, drawn by
    /// <see cref="SpeedFormatter.BarWithScale"/>, because only that block draws in Cascadia Mono, which has the eighth
    /// blocks; fenced code is Consolas, which does not (ADR-0020).
    /// </summary>
    public static string Readout(string title, MeterFrame frame, bool active) =>
        "## " + title + ActiveMarker(active) + "\n\n"
        + "## " + SpeedFormatter.Speed(frame.Shown) + "\n";

    /// <summary>The connection line. Its fields come from the server and are escaped before they touch the markdown.</summary>
    public static string Footer(SpeedTestSnapshot snapshot)
    {
        var c = snapshot.Connection;
        return "---\n\n"
            + "**ISP** " + Field(c.Isp)
            + "  ·  **IP** " + Field(c.Ip)
            + "  ·  **Location** " + Field(c.Location)
            + "  ·  **Server** " + Field(c.Server)
            + "\n";
    }

    public static string StatusLine(SpeedTestSnapshot snapshot) => snapshot.Phase switch
    {
        SpeedTestPhase.Idle => "Ready.",
        SpeedTestPhase.Connecting => "Connecting…",
        SpeedTestPhase.Latency => "Measuring latency…",
        SpeedTestPhase.Download => "Measuring download…",
        SpeedTestPhase.Upload => "Measuring upload…",
        SpeedTestPhase.Complete => "Complete" + (snapshot.CompletedAt is { } at ? " at " + at.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) : string.Empty) + ".",
        SpeedTestPhase.Failed => "**Failed:** " + (snapshot.Error ?? "unknown error"),
        _ => string.Empty,
    };

    private static string ActiveMarker(bool active) => active ? " ●" : string.Empty;

    private static string Field(string? value) =>
        string.IsNullOrEmpty(value) ? SpeedFormatter.Unknown : MarkdownText.Escape(value);
}
