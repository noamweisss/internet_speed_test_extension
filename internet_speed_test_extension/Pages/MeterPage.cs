using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Core;

namespace SpeedTest.Extension.Pages;

/// <summary>The meter dashboard: one markdown block rendered by <see cref="MeterMarkdown"/> (ADR-0006).</summary>
internal sealed partial class MeterPage : ContentPage
{
    private readonly SpeedTestSession _session;
    private readonly MarkdownContent _content = new();

    public MeterPage(SpeedTestSession session)
    {
        _session = session;
        Icon = new IconInfo("");
        Title = "Internet Speed Test";
        Name = "Meter dashboard";
        _session.Changed += (_, _) => Redraw();
    }

    public override IContent[] GetContent()
    {
        _session.StartIfStale();
        Redraw();
        return [_content];
    }

    private void Redraw()
    {
        var snapshot = _session.Snapshot;
        _content.Body = MeterMarkdown.Render(snapshot);
        IsLoading = snapshot.IsRunning;
        RaiseItemsChanged();
    }
}
