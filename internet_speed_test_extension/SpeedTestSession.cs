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
internal sealed class SpeedTestSession : IProgress<SpeedTestSnapshot>, IDisposable
{
    /// <summary>A result older than this is stale: opening a view starts a fresh test.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(1);

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
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

    void IProgress<SpeedTestSnapshot>.Report(SpeedTestSnapshot value) => Publish(value);

    public void Dispose()
    {
        Interlocked.Exchange(ref _current, null)?.Cancel();
        _http.Dispose();
    }

    private async Task RunAsync(CancellationTokenSource run)
    {
        try
        {
            await _measurer.MeasureAsync(this, run.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer Start() or disposed; the newer run publishes its own state.
        }
        catch (SpeedTestException e)
        {
            if (ReferenceEquals(_current, run))
            {
                Publish(Snapshot with { Phase = SpeedTestPhase.Failed, Error = e.Message });
            }
        }
    }

    private void Publish(SpeedTestSnapshot snapshot)
    {
        Snapshot = snapshot;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
