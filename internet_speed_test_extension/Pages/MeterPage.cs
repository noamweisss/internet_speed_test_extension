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
/// </summary>
internal sealed partial class MeterPage : ContentPage
{
    private readonly SpeedTestSession _session;
    private readonly MarkdownContent _header = new();
    private readonly MarkdownContent _downloadMeter = new();
    private readonly MarkdownContent _uploadMeter = new();
    private readonly MarkdownContent _footer = new();
    private readonly IContent[] _blocks;

    public MeterPage(SpeedTestSession session)
    {
        _session = session;
        _blocks = [_header, _downloadMeter, _uploadMeter, _footer];
        Icon = new IconInfo("");
        Title = "Internet Speed Test";
        Name = "Meter dashboard";
        _session.Changed += (_, _) => Redraw();
    }

    public override IContent[] GetContent()
    {
        _session.StartIfStale();
        Redraw();
        return _blocks;
    }

    private void Redraw()
    {
        var snapshot = _session.Snapshot;
        _header.Body = MeterMarkdown.Header(snapshot);
        _downloadMeter.Body = MeterMarkdown.Meter(MeterMarkdown.DownloadTitle, snapshot.DownloadMbps, snapshot.Phase == SpeedTestPhase.Download);
        _uploadMeter.Body = MeterMarkdown.Meter(MeterMarkdown.UploadTitle, snapshot.UploadMbps, snapshot.Phase == SpeedTestPhase.Upload);
        _footer.Body = MeterMarkdown.Footer(snapshot);
        IsLoading = snapshot.IsRunning;
    }
}
