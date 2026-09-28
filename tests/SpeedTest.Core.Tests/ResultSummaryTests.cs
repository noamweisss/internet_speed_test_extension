using System;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class ResultSummaryTests
{
    private static readonly SpeedTestSnapshot Complete = new()
    {
        Phase = SpeedTestPhase.Complete,
        Connection = new ConnectionInfo("Example ISP", "203.0.113.7", "Tel Aviv", "IL", "TLV"),
        LatencyMs = 12.3,
        JitterMs = 1.5,
        DownloadMbps = 245.3,
        UploadMbps = 40.1,
        CompletedAt = new DateTimeOffset(2026, 9, 28, 14, 5, 0, TimeSpan.Zero),
    };

    [Fact]
    public void PlainText_Complete_RendersEveryLine()
    {
        var expected =
            "Internet Speed Test — 2026-09-28 14:05\n" +
            "Download: 245.3 Mbps\n" +
            "Upload: 40.1 Mbps\n" +
            "Latency: 12.3 ms, jitter 1.5 ms\n" +
            "ISP: Example ISP\n" +
            "Location: Tel Aviv, IL\n" +
            "Server: TLV\n";

        Assert.Equal(expected, ResultSummary.PlainText(Complete));
    }

    [Fact]
    public void Markdown_Complete_RendersTableWithBoldLabels()
    {
        var expected =
            "| Internet Speed Test | 2026-09-28 14:05 |\n" +
            "|---|---|\n" +
            "| **Download** | 245.3 Mbps |\n" +
            "| **Upload** | 40.1 Mbps |\n" +
            "| **Latency** | 12.3 ms, jitter 1.5 ms |\n" +
            "| **ISP** | Example ISP |\n" +
            "| **Location** | Tel Aviv, IL |\n" +
            "| **Server** | TLV |\n";

        Assert.Equal(expected, ResultSummary.Markdown(Complete));
    }

    [Fact]
    public void PlainText_Idle_ShowsStatusAndPlaceholders()
    {
        var expected =
            "Internet Speed Test — Ready.\n" +
            "Download: —\n" +
            "Upload: —\n" +
            "Latency: —\n" +
            "ISP: —\n" +
            "Location: —\n" +
            "Server: —\n";

        Assert.Equal(expected, ResultSummary.PlainText(SpeedTestSnapshot.Idle));
    }

    [Fact]
    public void PlainText_NotCompleted_UsesStatusLineWithoutMarkdownBold()
    {
        var running = new SpeedTestSnapshot { Phase = SpeedTestPhase.Download, DownloadMbps = 245.3 };
        var failed = new SpeedTestSnapshot { Phase = SpeedTestPhase.Failed, Error = "No network" };

        Assert.StartsWith("Internet Speed Test — Measuring download…\nDownload: 245.3 Mbps\n", ResultSummary.PlainText(running), StringComparison.Ordinal);
        Assert.StartsWith("Internet Speed Test — Failed: No network\n", ResultSummary.PlainText(failed), StringComparison.Ordinal);
        Assert.StartsWith("| Internet Speed Test | Failed: No network |\n", ResultSummary.Markdown(failed), StringComparison.Ordinal);
    }

    [Fact]
    public void PlainTextAndMarkdown_NeverIncludeIpAddress()
    {
        Assert.DoesNotContain("203.0.113.7", ResultSummary.PlainText(Complete), StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113.7", ResultSummary.Markdown(Complete), StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_EscapesServerSuppliedConnectionFields()
    {
        var snapshot = new SpeedTestSnapshot
        {
            Phase = SpeedTestPhase.Complete,
            Connection = new ConnectionInfo("![x](//tracker.example/p.png)", "1.2.3.4", "<b>City</b>", "IL", "`code` | x"),
        };

        var markdown = ResultSummary.Markdown(snapshot);

        Assert.DoesNotContain("![x](", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("`code`", markdown, StringComparison.Ordinal);
        Assert.Contains("\\`code\\` \\| x", markdown, StringComparison.Ordinal);
    }
}
