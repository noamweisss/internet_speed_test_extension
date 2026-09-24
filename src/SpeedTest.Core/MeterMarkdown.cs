using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Renders the meter dashboard as markdown (ADR-0006). Pure function of the snapshot, so the layout is unit-tested
/// and the extension's MeterPage only has to hand the string to a MarkdownContent.
/// </summary>
public static class MeterMarkdown
{
    public static string Render(SpeedTestSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.Append("# Internet Speed Test\n\n");
        text.Append(StatusLine(snapshot)).Append("\n\n");
        AppendMeter(text, "⬇ Download", snapshot.DownloadMbps, active: snapshot.Phase == SpeedTestPhase.Download);
        AppendMeter(text, "⬆ Upload", snapshot.UploadMbps, active: snapshot.Phase == SpeedTestPhase.Upload);
        text.Append("## ⏱ Latency\n\n### ").Append(SpeedFormatter.Latency(snapshot.LatencyMs));
        if (snapshot.JitterMs is not null)
        {
            text.Append("  ·  jitter ").Append(SpeedFormatter.Latency(snapshot.JitterMs));
        }

        text.Append("\n\n---\n\n");
        var c = snapshot.Connection;
        text.Append("**ISP** ").Append(c.Isp ?? SpeedFormatter.Unknown)
            .Append("  ·  **IP** ").Append(c.Ip ?? SpeedFormatter.Unknown)
            .Append("  ·  **Location** ").Append(c.Location.Length > 0 ? c.Location : SpeedFormatter.Unknown)
            .Append("  ·  **Server** ").Append(c.Server ?? SpeedFormatter.Unknown)
            .Append('\n');
        return text.ToString();
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

    private static void AppendMeter(StringBuilder text, string title, double? mbps, bool active)
    {
        text.Append("## ").Append(title).Append(active ? " ●" : string.Empty).Append("\n\n");
        text.Append("### ").Append(SpeedFormatter.Speed(mbps)).Append("\n\n");
        var value = mbps ?? 0;
        text.Append('`').Append(SpeedFormatter.Bar(value, SpeedFormatter.ScaleFor(value))).Append("`\n\n");
    }
}
