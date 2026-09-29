using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Core;

namespace SpeedTest.Extension.Pages;

/// <summary>
/// The meter dashboard (ADR-0006): four markdown blocks rendered by <see cref="MeterMarkdown"/>, returned as the same
/// instances every time. The host rebuilds a block whole when its Body changes and leaves the others alone
/// (docs/CMDPAL-RENDERING.md §5), so a live download value does not re-parse the heading or the connection line.
/// Never RaiseItemsChanged here: the host answers it by calling GetContent again on the same thread
/// (ContentPageViewModel.Model_ItemsChanged in PowerToys), which looped and froze the first real run.
/// <para>
/// A ticker draws every block, and the meters glide toward each measurement through <see cref="MeterEasing"/>
/// (§15 option 1). It is a one-shot timer re-armed after each tick, so two ticks never overlap and a late frame
/// can not overwrite a newer one. The session's Changed event only wakes it: Changed arrives on the measurement
/// thread, and a Body set blocks for two cross-process calls (§5 step 2, §16).
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
    private double? _shownDownloadMbps;
    private double? _shownUploadMbps;

    public MeterPage(SpeedTestSession session)
    {
        _session = session;
        _blocks = [_header, _downloadMeter, _uploadMeter, _footer];
        Icon = new IconInfo("");
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
        SpeedTestSnapshot snapshot;
        string? header = null;
        string? footer = null;
        string? downloadMeter = null;
        string? uploadMeter = null;
        lock (_gate)
        {
            snapshot = _session.Snapshot;
            var downloading = snapshot.Phase == SpeedTestPhase.Download;
            var uploading = snapshot.Phase == SpeedTestPhase.Upload;
            var shownDownload = MeterEasing.Step(_shownDownloadMbps, snapshot.DownloadMbps, live: downloading);
            var shownUpload = MeterEasing.Step(_shownUploadMbps, snapshot.UploadMbps, live: uploading);
            var snapshotChanged = !ReferenceEquals(snapshot, _drawnSnapshot);
            if (snapshotChanged)
            {
                header = MeterMarkdown.Header(snapshot);
                footer = MeterMarkdown.Footer(snapshot);
            }

            // Between measurements the eased values settle; skip rendering a meter whose input did not change.
            if (snapshotChanged || shownDownload != _shownDownloadMbps)
            {
                downloadMeter = MeterMarkdown.Meter(MeterMarkdown.DownloadTitle, shownDownload, downloading);
            }

            if (snapshotChanged || shownUpload != _shownUploadMbps)
            {
                uploadMeter = MeterMarkdown.Meter(MeterMarkdown.UploadTitle, shownUpload, uploading);
            }

            _drawnSnapshot = snapshot;
            _shownDownloadMbps = shownDownload;
            _shownUploadMbps = shownUpload;
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
