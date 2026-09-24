using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class SpeedMeasurerTests
{
    /// <summary>Small and fast: two latency probes, 150 ms transfer phases, one stream, tiny payloads.</summary>
    private static readonly SpeedTestOptions FastOptions = new()
    {
        LatencySamples = 2,
        PhaseDuration = TimeSpan.FromMilliseconds(150),
        DownloadStreams = 1,
        UploadStreams = 1,
        DownloadRequestBytes = 4096,
        UploadRequestBytes = 4096,
        ProgressInterval = TimeSpan.FromMilliseconds(20),
    };

    private static (SpeedMeasurer Measurer, FakeCloudflareHandler Handler) Create(SpeedTestOptions? options = null)
    {
        var handler = new FakeCloudflareHandler();
        return (new SpeedMeasurer(new HttpClient(handler), options ?? FastOptions), handler);
    }

    [Fact]
    public async Task MeasureAsync_HappyPath_ReportsPhasesInOrderAndCompletes()
    {
        var (measurer, _) = Create();
        var reported = new List<SpeedTestSnapshot>();

        var result = await measurer.MeasureAsync(new Progress<SpeedTestSnapshot>(reported.Add), CancellationToken.None);
        await Task.Delay(50); // Progress<T> posts asynchronously; let the last reports land.

        Assert.Equal(SpeedTestPhase.Complete, result.Phase);
        Assert.NotNull(result.CompletedAt);
        Assert.True(result.LatencyMs >= 0);
        Assert.True(result.JitterMs >= 0);
        Assert.True(result.DownloadMbps > 0);
        Assert.True(result.UploadMbps > 0);

        var phases = reported.Select(s => s.Phase).Distinct().ToArray();
        Assert.Equal(
            new[] { SpeedTestPhase.Connecting, SpeedTestPhase.Latency, SpeedTestPhase.Download, SpeedTestPhase.Upload, SpeedTestPhase.Complete },
            phases);
    }

    [Fact]
    public async Task MeasureAsync_HappyPath_UsesMetaForConnectionInfo()
    {
        var (measurer, _) = Create();

        var result = await measurer.MeasureAsync(null, CancellationToken.None);

        Assert.Equal(new ConnectionInfo("Example ISP", "203.0.113.7", "Tel Aviv", "IL", "TLV"), result.Connection);
    }

    [Fact]
    public async Task MeasureAsync_MetaEmpty_FallsBackToResponseHeaders()
    {
        var (measurer, handler) = Create();
        handler.MetaJson = "{}";

        var result = await measurer.MeasureAsync(null, CancellationToken.None);

        Assert.Equal(new ConnectionInfo(null, "198.51.100.9", "Haifa", "IL", "HFA"), result.Connection);
    }

    [Fact]
    public async Task MeasureAsync_RespectsRequestBounds()
    {
        var (measurer, handler) = Create();

        await measurer.MeasureAsync(null, CancellationToken.None);

        Assert.Equal(FastOptions.LatencySamples, handler.Requests.Count(u => u.Query == "?bytes=0"));
        Assert.All(handler.Requests.Where(u => u.AbsolutePath == "/__down" && u.Query != "?bytes=0"), u => Assert.Equal("?bytes=4096", u.Query));
        Assert.All(handler.Requests, u => Assert.Equal("speed.cloudflare.com", u.Host));
    }

    [Fact]
    public async Task MeasureAsync_LiveProgress_ReportsDownloadDuringDownloadPhase()
    {
        var (measurer, _) = Create();
        var reported = new List<SpeedTestSnapshot>();

        await measurer.MeasureAsync(new Progress<SpeedTestSnapshot>(reported.Add), CancellationToken.None);
        await Task.Delay(50);

        Assert.Contains(reported, s => s.Phase == SpeedTestPhase.Download && s.DownloadMbps > 0);
        Assert.Contains(reported, s => s.Phase == SpeedTestPhase.Upload && s.UploadMbps > 0);
    }

    [Fact]
    public async Task MeasureAsync_NetworkDown_ThrowsSpeedTestExceptionWithUserMessage()
    {
        var (measurer, handler) = Create();
        handler.ThrowOnEveryRequest = true;

        var error = await Assert.ThrowsAsync<SpeedTestException>(() => measurer.MeasureAsync(null, CancellationToken.None));

        Assert.Contains("internet connection", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<HttpRequestException>(error.InnerException);
    }

    [Fact]
    public async Task MeasureAsync_ServerError_ThrowsSpeedTestException()
    {
        var (measurer, handler) = Create();
        handler.DownloadStatus = HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsAsync<SpeedTestException>(() => measurer.MeasureAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task MeasureAsync_CallerCancels_ThrowsOperationCanceled()
    {
        var (measurer, _) = Create(FastOptions with { PhaseDuration = TimeSpan.FromSeconds(30) });
        using var cts = new CancellationTokenSource();
        var progress = new Progress<SpeedTestSnapshot>(s =>
        {
            if (s.Phase == SpeedTestPhase.Download)
            {
                cts.Cancel();
            }
        });

        var task = measurer.MeasureAsync(progress, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task MeasureAsync_ServerTimingPresent_DoesNotProduceNegativeLatency()
    {
        var (measurer, handler) = Create();
        handler.ServerDurationMs = 10_000;

        var result = await measurer.MeasureAsync(null, CancellationToken.None);

        Assert.Equal(0, result.LatencyMs);
    }
}
