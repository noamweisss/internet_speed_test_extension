using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Core;

namespace SpeedTest.Extension.Pages;

/// <summary>
/// The meter dashboard (ADR-0017): four markdown blocks rendered by <see cref="MeterMarkdown"/>, returned as the same
/// instances every time. The host rebuilds a block whole when its Body changes and leaves the others alone
/// (docs/CMDPAL-RENDERING.md §5), so a live download value does not re-parse the heading or the connection line.
/// Never RaiseItemsChanged here: the host answers it by calling GetContent again on the same thread
/// (ContentPageViewModel.Model_ItemsChanged in PowerToys), which looped and froze the first real run.
/// <para>
/// A ticker draws every block, and the meters glide toward each measurement through <see cref="MeterEasing"/>
/// (§15 option 1). It is a one-shot timer re-armed after each tick, so two ticks never overlap and a late frame
/// can not overwrite a newer one. The session's Changed event only wakes it: Changed arrives on the measurement
/// thread, and a Body set blocks for two cross-process calls (§5 step 2, §16). After SpeedTestSession.Dispose() mid-run
/// the snapshot stays running and the ticker keeps re-arming until the process exits, which happens right after
/// Dispose (Program.cs).
/// </para>
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The page lives as long as the extension process; the ticker is stopped between runs, not disposed.")]
internal sealed partial class MeterPage : ContentPage
{
    private readonly SpeedTestSession _session;
    private readonly MarkdownContent _header = new();
    private readonly MarkdownContent _downloadMeter = new();
    private readonly MarkdownContent _uploadMeter = new();
    private readonly MarkdownContent _footer = new();
    private readonly IContent[] _blocks;
    private readonly Timer _ticker;

    // Changed and the ticker run on different threads; the fields below are read and written only under _gate.
    private readonly object _gate = new();
    private bool _tickScheduled;
    private SpeedTestSnapshot? _drawnSnapshot;
    private MeterFrame _download = MeterFrame.Empty;
    private MeterFrame _upload = MeterFrame.Empty;

    public MeterPage(SpeedTestSession session)
    {
        _session = session;
        _blocks = [_header, _downloadMeter, _uploadMeter, _footer];
        // Segoe Fluent Icons U+E9D9 (Diagnostic), the icon this page has on main. Written as an escape: the
        // raw private-use character is invisible in an editor, and one edit stripped it and left an empty icon.
        Icon = new IconInfo("\uE9D9");
        Title = "Internet Speed Test";
        Name = "Meter dashboard";
        _ticker = new Timer(_ => Tick());
        _session.Changed += (_, _) => ScheduleTick();
        ScheduleTick();
    }

    public override IContent[] GetContent()
    {
        _session.StartIfStale();
        return _blocks;
    }

    private void ScheduleTick()
    {
        lock (_gate)
        {
            if (!_tickScheduled)
            {
                _tickScheduled = true;
                _ticker.Change(0, Timeout.Infinite);
            }
        }
    }

    private void Tick()
    {
        try
        {
            Draw();
        }
        finally
        {
            // No catch: the toolkit already swallows failed calls to the host (§5 step 1), so whatever reaches here is
            // a bug and should surface. The finally only keeps the ticker consistent on the way out.
            lock (_gate)
            {
                // Tick on while the run is live. A change that arrived during this tick found it scheduled and did not
                // wake it, so it gets one more tick: that is how the final, exact frame of a finished run is drawn.
                var current = _session.Snapshot;
                if (current.IsRunning || !ReferenceEquals(current, _drawnSnapshot))
                {
                    _ticker.Change(MeterEasing.TickMilliseconds, Timeout.Infinite);
                }
                else
                {
                    _tickScheduled = false;
                }
            }
        }
    }

    private void Draw()
    {
        SpeedTestSnapshot snapshot;
        string? header = null;
        string? footer = null;
        string? downloadMeter;
        string? uploadMeter;
        lock (_gate)
        {
            snapshot = _session.Snapshot;
            var snapshotChanged = !ReferenceEquals(snapshot, _drawnSnapshot);
            if (snapshotChanged)
            {
                header = MeterMarkdown.Header(snapshot);
                footer = MeterMarkdown.Footer(snapshot);
            }

            downloadMeter = NextMeter(ref _download, MeterMarkdown.DownloadTitle, snapshot.DownloadMbps, snapshot.Phase == SpeedTestPhase.Download, snapshotChanged);
            uploadMeter = NextMeter(ref _upload, MeterMarkdown.UploadTitle, snapshot.UploadMbps, snapshot.Phase == SpeedTestPhase.Upload, snapshotChanged);
            _drawnSnapshot = snapshot;
        }

        // Outside the lock: each set raises the host's PropChanged synchronously (§5 step 1). An equal string is skipped.
        if (header is not null && footer is not null)
        {
            _header.Body = header;
            _footer.Body = footer;
            IsLoading = snapshot.IsRunning;
        }

        if (downloadMeter is not null)
        {
            _downloadMeter.Body = downloadMeter;
        }

        if (uploadMeter is not null)
        {
            _uploadMeter.Body = uploadMeter;
        }
    }

    /// <summary>
    /// Advances one meter's frame (called under _gate) and returns its new markdown, or null when neither the frame
    /// nor the snapshot changed: between measurements the eased value settles and the meter is not rendered again.
    /// </summary>
    private static string? NextMeter(ref MeterFrame frame, string title, double? measured, bool live, bool snapshotChanged)
    {
        var next = MeterEasing.Next(frame, measured, live);
        var changed = snapshotChanged || next != frame;
        frame = next;
        return changed ? MeterMarkdown.Meter(title, next, live) : null;
    }
}
