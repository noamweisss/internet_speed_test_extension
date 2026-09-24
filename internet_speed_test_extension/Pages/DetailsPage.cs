using System.Globalization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpeedTest.Core;

namespace SpeedTest.Extension.Pages;

/// <summary>Every measured value as a list row. Enter copies the value; Ctrl+L / Ctrl+R work on any row.</summary>
internal sealed partial class DetailsPage : ListPage
{
    private readonly SpeedTestSession _session;

    public DetailsPage(SpeedTestSession session)
    {
        _session = session;
        Icon = new IconInfo("");
        Title = "Internet Speed Test";
        Name = "Detailed results";
        PlaceholderText = "Filter results";
        _session.Changed += (_, _) =>
        {
            IsLoading = _session.Snapshot.IsRunning;
            RaiseItemsChanged();
        };
    }

    /// <summary>Set by the provider once both pages exist (the switch command needs the other page).</summary>
    public IContextItem[] ItemCommands { get; set; } = [];

    public override IListItem[] GetItems()
    {
        _session.StartIfStale();
        var s = _session.Snapshot;
        IsLoading = s.IsRunning;
        var c = s.Connection;
        return [
            Row("Status", MeterMarkdown.StatusLine(s).Replace("**", string.Empty), ""),
            Row("Download", SpeedFormatter.Speed(s.DownloadMbps), ""),
            Row("Upload", SpeedFormatter.Speed(s.UploadMbps), ""),
            Row("Latency", SpeedFormatter.Latency(s.LatencyMs), ""),
            Row("Jitter", SpeedFormatter.Latency(s.JitterMs), ""),
            Row("ISP", c.Isp ?? SpeedFormatter.Unknown, ""),
            Row("IP address", c.Ip ?? SpeedFormatter.Unknown, ""),
            Row("Location", c.Location.Length > 0 ? c.Location : SpeedFormatter.Unknown, ""),
            Row("Server", c.Server ?? SpeedFormatter.Unknown, ""),
            Row("Tested at", s.CompletedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? SpeedFormatter.Unknown, ""),
        ];
    }

    private ListItem Row(string label, string value, string glyph) =>
        new(new CopyTextCommand(value) { Name = "Copy" })
        {
            Title = value,
            Subtitle = label,
            Icon = new IconInfo(glyph),
            MoreCommands = ItemCommands,
        };
}
