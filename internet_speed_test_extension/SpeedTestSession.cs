using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SpeedTest.Core;

namespace SpeedTest.Extension;

/// <summary>
/// The one running (or last finished) measurement, shared by both views. Starting again cancels the
/// previous run. <see cref="Changed"/> fires on every progress report; pages redraw from <see cref="Snapshot"/>.
/// </summary>
internal sealed partial class SpeedTestSession : IDisposable
{
    /// <summary>A result older than this is stale: opening a view starts a fresh test.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Redirects are never followed, so a response can not send us to a host outside scripts/allowed-hosts.txt.
    /// Buffered responses (latency probes, /meta status) are capped; transfers stream and are bounded by time.
    /// </summary>
    private readonly HttpClient _http = new(new SocketsHttpHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 64 * 1024,
    };

    private readonly SpeedMeasurer _measurer;
    private CancellationTokenSource? _current;

    public SpeedTestSession()
    {
        _measurer = new SpeedMeasurer(_http);
    }

    public event EventHandler? Changed;

    public SpeedTestSnapshot Snapshot { get; private set; } = SpeedTestSnapshot.Idle;

    /// <summary>Called by pages when they are shown: runs a test unless one is running or a fresh result exists.</summary>
    public void StartIfStale()
    {
        var snapshot = Snapshot;
        var stale = snapshot.Phase == SpeedTestPhase.Idle
            || (snapshot.Phase == SpeedTestPhase.Complete && DateTimeOffset.Now - snapshot.CompletedAt > StaleAfter);
        if (stale)
        {
            Start();
        }
    }

    public void Start()
    {
        var run = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _current, run);
        previous?.Cancel();
        previous?.Dispose();
        _ = RunAsync(run);
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _current, null)?.Cancel();
        _http.Dispose();
    }

    private bool IsCurrent(CancellationTokenSource run) => ReferenceEquals(Volatile.Read(ref _current), run);

    private async Task RunAsync(CancellationTokenSource run)
    {
        try
        {
            await _measurer.MeasureAsync(new RunProgress(this, run), run.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer Start() or disposed; the newer run publishes its own state.
        }
        catch (ObjectDisposedException)
        {
            // Dispose() ran mid-measurement; nothing left to show.
        }
#pragma warning disable CA1031 // A fire-and-forget task must never leave the UI stuck in a running phase.
        catch (Exception e) when (IsCurrent(run))
        {
            var message = e is SpeedTestException ? e.Message : "The speed test stopped unexpectedly. Try again.";
            Publish(Snapshot with { Phase = SpeedTestPhase.Failed, Error = message });
        }
#pragma warning restore CA1031
    }

    private void Publish(SpeedTestSnapshot snapshot)
    {
        Snapshot = snapshot;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Progress sink bound to one run: reports from a superseded run are dropped.</summary>
    private sealed class RunProgress(SpeedTestSession owner, CancellationTokenSource run) : IProgress<SpeedTestSnapshot>
    {
        public void Report(SpeedTestSnapshot value)
        {
            if (owner.IsCurrent(run))
            {
                owner.Publish(value);
            }
        }
    }
}
