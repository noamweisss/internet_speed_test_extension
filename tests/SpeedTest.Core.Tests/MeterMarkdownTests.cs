using System;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class MeterMarkdownTests
{
    private static readonly SpeedTestSnapshot Completed = new()
    {
        Phase = SpeedTestPhase.Complete,
        Connection = new ConnectionInfo("Example ISP", "203.0.113.7", "Tel Aviv", "IL", "TLV"),
        LatencyMs = 12.3,
        JitterMs = 1.5,
        DownloadMbps = 245.3,
        UploadMbps = 40.1,
        CompletedAt = new DateTimeOffset(2026, 9, 24, 14, 5, 0, TimeSpan.Zero),
    };

    [Fact]
    public void Header_Idle_ShowsPlaceholderAndNoActiveMarker()
    {
        var markdown = MeterMarkdown.Header(SpeedTestSnapshot.Idle);

        Assert.Contains("Ready.", markdown);
        Assert.Contains("\n## —\n", markdown);
        Assert.DoesNotContain("●", markdown);
    }

    [Fact]
    public void Header_Any_ListsTitleStatusThenLatency()
    {
        var markdown = MeterMarkdown.Header(SpeedTestSnapshot.Idle);

        var title = markdown.IndexOf("# Internet Speed Test", StringComparison.Ordinal);
        var status = markdown.IndexOf("Ready.", StringComparison.Ordinal);
        var latency = markdown.IndexOf("## ⏱ Latency", StringComparison.Ordinal);
        Assert.True(title == 0 && title < status && status < latency, "expected title, status, Latency");
    }

    [Fact]
    public void Header_DuringLatency_MarksLatencyActive()
    {
        var markdown = MeterMarkdown.Header(new SpeedTestSnapshot { Phase = SpeedTestPhase.Latency });

        Assert.Contains("## ⏱ Latency ●", markdown);
    }

    [Fact]
    public void Header_DuringDownload_ShowsPhaseAndLeavesLatencyUnmarked()
    {
        var markdown = MeterMarkdown.Header(new SpeedTestSnapshot { Phase = SpeedTestPhase.Download, LatencyMs = 12 });

        Assert.Contains("Measuring download…", markdown);
        Assert.Contains("## ⏱ Latency\n", markdown);
    }

    [Fact]
    public void Header_Complete_ShowsTimeLatencyAndJitter()
    {
        var markdown = MeterMarkdown.Header(Completed);

        Assert.Contains("Complete at 14:05.", markdown);
        Assert.Contains("\n## 12.3 ms  ·  jitter 1.5 ms\n", markdown);
    }

    [Fact]
    public void Header_Failed_ShowsError()
    {
        var markdown = MeterMarkdown.Header(new SpeedTestSnapshot { Phase = SpeedTestPhase.Failed, Error = "No network" });

        Assert.Contains("**Failed:** No network", markdown);
    }

    [Fact]
    public void Meter_Active_MarksTitleAndShowsValue()
    {
        var markdown = MeterMarkdown.Meter(MeterMarkdown.DownloadTitle, 245.3, 250, active: true);

        Assert.Contains("## ⬇ Download ●", markdown);
        Assert.Contains("\n## 245.3 Mbps\n", markdown);
    }

    [Fact]
    public void Meter_Inactive_HasNoActiveMarker()
    {
        var markdown = MeterMarkdown.Meter(MeterMarkdown.UploadTitle, 40.1, 50, active: false);

        Assert.StartsWith("## ⬆ Upload\n", markdown, StringComparison.Ordinal);
        Assert.Contains("\n## 40.1 Mbps\n", markdown);
    }

    [Fact]
    public void Meter_WithValue_EndsWithBarImageThenScaleLine()
    {
        var markdown = MeterMarkdown.Meter(MeterMarkdown.DownloadTitle, 245.3, 250, active: true);

        // The bar is an image on its own paragraph and the scale is body text under it (ADR-0017).
        Assert.EndsWith("\n\n![](" + GaugeSvg.DataUri(245.3, 250) + ")\n\nscale 250 Mbps\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Meter_WithCallerScale_DrawsBarAgainstThatScale()
    {
        // 9.5 alone would pick the 10 scale and nearly fill the bar; the caller's 100 keeps it where the run left it.
        var markdown = MeterMarkdown.Meter(MeterMarkdown.DownloadTitle, 9.5, 100, active: true);

        Assert.Contains("![](" + GaugeSvg.DataUri(9.5, 100) + ")", markdown);
        Assert.EndsWith("scale 100 Mbps\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Meter_NotMeasured_ShowsPlaceholderEmptyBarAndNoScale()
    {
        var markdown = MeterMarkdown.Meter(MeterMarkdown.UploadTitle, null, 0, active: false);

        Assert.Contains("\n## —\n", markdown);
        Assert.Contains("![](" + GaugeSvg.DataUri(0, 0) + ")", markdown);
        Assert.EndsWith("scale —\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Footer_Complete_ShowsConnection()
    {
        var markdown = MeterMarkdown.Footer(Completed);

        Assert.Contains("**ISP** Example ISP  ·  **IP** 203.0.113.7  ·  **Location** Tel Aviv, IL  ·  **Server** TLV", markdown);
    }

    [Fact]
    public void Footer_Idle_ShowsPlaceholders()
    {
        var markdown = MeterMarkdown.Footer(SpeedTestSnapshot.Idle);

        Assert.Contains("**ISP** —  ·  **IP** —  ·  **Location** —  ·  **Server** —", markdown);
    }

    [Fact]
    public void Footer_EscapesServerSuppliedConnectionFields()
    {
        var snapshot = new SpeedTestSnapshot
        {
            Phase = SpeedTestPhase.Complete,
            Connection = new ConnectionInfo("![x](//tracker.example/p.png)", "1.2.3.4", "<b>City</b>", "IL", "`code`"),
        };

        var markdown = MeterMarkdown.Footer(snapshot);

        Assert.DoesNotContain("![x](", markdown);
        Assert.DoesNotContain("<b>", markdown);
        Assert.DoesNotContain("`code`", markdown);
        Assert.Contains("1.2.3.4", markdown);
    }
}
