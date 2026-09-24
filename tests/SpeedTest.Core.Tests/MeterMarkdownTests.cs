using System;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class MeterMarkdownTests
{
    [Fact]
    public void Render_Idle_ShowsPlaceholdersAndNoActiveMarker()
    {
        var markdown = MeterMarkdown.Render(SpeedTestSnapshot.Idle);

        Assert.Contains("Ready.", markdown);
        Assert.Contains("### —", markdown);
        Assert.DoesNotContain("●", markdown);
    }

    [Fact]
    public void Render_DuringDownload_MarksDownloadActiveWithLiveValue()
    {
        var snapshot = new SpeedTestSnapshot { Phase = SpeedTestPhase.Download, DownloadMbps = 245.3, LatencyMs = 12 };

        var markdown = MeterMarkdown.Render(snapshot);

        Assert.Contains("Measuring download…", markdown);
        Assert.Contains("## ⬇ Download ●", markdown);
        Assert.Contains("### 245.3 Mbps", markdown);
        Assert.Contains("## ⬆ Upload\n", markdown);
    }

    [Fact]
    public void Render_Complete_ShowsEveryValueAndConnection()
    {
        var snapshot = new SpeedTestSnapshot
        {
            Phase = SpeedTestPhase.Complete,
            Connection = new ConnectionInfo("Example ISP", "203.0.113.7", "Tel Aviv", "IL", "TLV"),
            LatencyMs = 12.3,
            JitterMs = 1.5,
            DownloadMbps = 245.3,
            UploadMbps = 40.1,
            CompletedAt = new DateTimeOffset(2026, 9, 24, 14, 5, 0, TimeSpan.Zero),
        };

        var markdown = MeterMarkdown.Render(snapshot);

        Assert.Contains("Complete at 14:05.", markdown);
        Assert.Contains("### 40.1 Mbps", markdown);
        Assert.Contains("### 12.3 ms  ·  jitter 1.5 ms", markdown);
        Assert.Contains("**ISP** Example ISP  ·  **IP** 203.0.113.7  ·  **Location** Tel Aviv, IL  ·  **Server** TLV", markdown);
    }

    [Fact]
    public void Render_EscapesServerSuppliedConnectionFields()
    {
        var snapshot = new SpeedTestSnapshot
        {
            Phase = SpeedTestPhase.Complete,
            Connection = new ConnectionInfo("![x](//tracker.example/p.png)", "1.2.3.4", "<b>City</b>", "IL", "`code`"),
        };

        var markdown = MeterMarkdown.Render(snapshot);

        Assert.DoesNotContain("![x](", markdown);
        Assert.DoesNotContain("<b>", markdown);
        Assert.DoesNotContain("`code`", markdown);
        Assert.Contains("1.2.3.4", markdown);
    }

    [Fact]
    public void Render_Failed_ShowsError()
    {
        var markdown = MeterMarkdown.Render(new SpeedTestSnapshot { Phase = SpeedTestPhase.Failed, Error = "No network" });

        Assert.Contains("**Failed:** No network", markdown);
    }
}
