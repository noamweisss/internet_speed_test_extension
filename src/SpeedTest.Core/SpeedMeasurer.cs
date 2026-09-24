using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest.Core;

/// <summary>
/// Runs one measurement: connection info → latency → download → upload (docs/ARCHITECTURE.md).
/// Reports a <see cref="SpeedTestSnapshot"/> after every phase and every ProgressInterval during transfers.
/// The HttpClient is injected so tests can fake the network; callers own its lifetime and timeout.
/// </summary>
public sealed class SpeedMeasurer
{
    private const int ReadBufferBytes = 64 * 1024;
    private readonly HttpClient _http;
    private readonly SpeedTestOptions _options;

    public SpeedMeasurer(HttpClient http, SpeedTestOptions? options = null)
    {
        _http = http;
        _options = options ?? SpeedTestOptions.Default;
    }

    /// <exception cref="SpeedTestException">The server could not be reached or answered with an error.</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled.</exception>
    public async Task<SpeedTestSnapshot> MeasureAsync(IProgress<SpeedTestSnapshot>? progress, CancellationToken cancellationToken)
    {
        var snapshot = new SpeedTestSnapshot { Phase = SpeedTestPhase.Connecting };
        try
        {
            progress?.Report(snapshot);
            var connection = await FetchConnectionInfoAsync(cancellationToken).ConfigureAwait(false);
            snapshot = snapshot with { Connection = connection, Phase = SpeedTestPhase.Latency };
            progress?.Report(snapshot);

            var (latency, jitter, headerInfo) = await MeasureLatencyAsync(cancellationToken).ConfigureAwait(false);
            snapshot = snapshot with
            {
                Connection = connection.FillFrom(headerInfo),
                LatencyMs = latency,
                JitterMs = jitter,
                Phase = SpeedTestPhase.Download,
            };
            progress?.Report(snapshot);

            var download = await MeasureTransferAsync(
                upload: false, mbps => progress?.Report(snapshot with { DownloadMbps = mbps }), cancellationToken).ConfigureAwait(false);
            snapshot = snapshot with { DownloadMbps = download, Phase = SpeedTestPhase.Upload };
            progress?.Report(snapshot);

            var upload = await MeasureTransferAsync(
                upload: true, mbps => progress?.Report(snapshot with { UploadMbps = mbps }), cancellationToken).ConfigureAwait(false);
            snapshot = snapshot with { UploadMbps = upload, Phase = SpeedTestPhase.Complete, CompletedAt = DateTimeOffset.Now };
            progress?.Report(snapshot);
            return snapshot;
        }
        catch (HttpRequestException e)
        {
            throw new SpeedTestException("Could not reach the speed test server. Check your internet connection.", e);
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SpeedTestException("The speed test server did not answer in time.", e);
        }
    }

    private async Task<ConnectionInfo> FetchConnectionInfoAsync(CancellationToken cancellationToken)
    {
        var meta = await _http.GetFromJsonAsync(CloudflareEndpoints.Meta, CloudflareJsonContext.Default.CloudflareMeta, cancellationToken)
            .ConfigureAwait(false);
        return meta?.ToConnectionInfo() ?? ConnectionInfo.Empty;
    }

    private async Task<(double LatencyMs, double JitterMs, ConnectionInfo HeaderInfo)> MeasureLatencyAsync(CancellationToken cancellationToken)
    {
        var samples = new List<double>(_options.LatencySamples);
        var headerInfo = ConnectionInfo.Empty;
        for (var i = 0; i < _options.LatencySamples; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            using var response = await _http.GetAsync(CloudflareEndpoints.Download(0), cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            response.EnsureSuccessStatusCode();
            samples.Add(Math.Max(0, stopwatch.Elapsed.TotalMilliseconds - ServerTiming.DurationMs(response.Headers)));
            if (i == 0)
            {
                headerInfo = ConnectionInfo.FromHeaders(response.Headers);
            }
        }

        return (Statistics.Median(samples), Statistics.Jitter(samples), headerInfo);
    }

    /// <summary>
    /// Runs N parallel request loops for PhaseDuration, counting bytes, and samples throughput every ProgressInterval.
    /// The phase timer cancels the loops; the caller's token cancels everything.
    /// </summary>
    private async Task<double> MeasureTransferAsync(bool upload, Action<double> onSample, CancellationToken cancellationToken)
    {
        using var phase = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        phase.CancelAfter(_options.PhaseDuration);
        long total = 0;
        void Count(int bytes) => Interlocked.Add(ref total, bytes);

        var stopwatch = Stopwatch.StartNew();
        var streams = upload ? _options.UploadStreams : _options.DownloadStreams;
        // Task.Run keeps the loops off the caller's thread even when responses complete synchronously.
        var workers = Task.WhenAll(Enumerable.Range(0, streams).Select(_ => Task.Run(
            () => upload ? UploadLoopAsync(Count, phase.Token) : DownloadLoopAsync(Count, phase.Token), CancellationToken.None)));

        while (!workers.IsCompleted)
        {
            try
            {
                await Task.Delay(_options.ProgressInterval, phase.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            onSample(Statistics.Mbps(Interlocked.Read(ref total), stopwatch.Elapsed));
        }

        await workers.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return Statistics.Mbps(Interlocked.Read(ref total), stopwatch.Elapsed);
    }

    private async Task DownloadLoopAsync(Action<int> count, CancellationToken phaseToken)
    {
        var buffer = new byte[ReadBufferBytes];
        try
        {
            while (!phaseToken.IsCancellationRequested)
            {
                using var response = await _http.GetAsync(
                    CloudflareEndpoints.Download(_options.DownloadRequestBytes), HttpCompletionOption.ResponseHeadersRead, phaseToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync(phaseToken).ConfigureAwait(false);
                int read;
                while ((read = await stream.ReadAsync(buffer, phaseToken).ConfigureAwait(false)) > 0)
                {
                    count(read);
                }
            }
        }
        catch (OperationCanceledException) when (phaseToken.IsCancellationRequested)
        {
            // The phase timer or the caller ended the loop; the caller's token is re-checked by MeasureTransferAsync.
        }
    }

    private async Task UploadLoopAsync(Action<int> count, CancellationToken phaseToken)
    {
        try
        {
            while (!phaseToken.IsCancellationRequested)
            {
                using var content = new ZeroContent(_options.UploadRequestBytes, count);
                using var response = await _http.PostAsync(CloudflareEndpoints.Upload, content, phaseToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (OperationCanceledException) when (phaseToken.IsCancellationRequested)
        {
        }
    }
}
