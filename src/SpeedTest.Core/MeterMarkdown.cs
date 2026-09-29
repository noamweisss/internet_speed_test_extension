using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Renders the meter dashboard as markdown (ADR-0017), one string per content block of the
/// extension's MeterPage. The page keeps each block as its own MarkdownContent because the host rebuilds a block
/// whole whenever its Body changes (docs/CMDPAL-RENDERING.md §5): a live value must not re-parse the static text.
/// Pure functions, so the layout is unit-tested here.
/// </summary>
public static class MeterMarkdown
{
    public const string DownloadTitle = "⬇ Download";

    public const string UploadTitle = "⬆ Upload";

    // Readouts are H2, not H3: the host renders H3 at 12 px normal weight, smaller than body text
    // (docs/CMDPAL-RENDERING.md §3).

    /// <summary>Title, status, and latency: they change per phase, not per sample.</summary>
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
    /// One speed section. <paramref name="mbps"/> is the value to show, not necessarily the last measurement, so the
    /// extension can pass an eased value; null means not measured yet. <paramref name="scale"/> is the bar's maximum,
    /// chosen by the caller from the measured value (<see cref="SpeedFormatter.ScaleFor(double, double)"/>), so an eased
    /// value crossing a step does not change it. The bar is an image on its own paragraph; the scale line under it
    /// says what a full bar means, since the image cannot carry text.
    /// </summary>
    public static string Meter(string title, double? mbps, double scale, bool active) =>
        "## " + title + ActiveMarker(active) + "\n\n"
        + "## " + SpeedFormatter.Speed(mbps) + "\n\n"
        + "![](" + GaugeSvg.DataUri(mbps ?? 0, scale) + ")\n\n"
        + "scale " + SpeedFormatter.ScaleLabel(scale) + "\n";

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
