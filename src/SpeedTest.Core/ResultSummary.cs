using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// The result as text the user pastes to someone else: plain lines for the clipboard, or a markdown table.
/// Pure functions of the snapshot, invariant culture. The IP address is deliberately left out: a summary is
/// what the user shares, and the IP stays copyable on its own row in the details view.
/// </summary>
public static class ResultSummary
{
    private const string Title = "Internet Speed Test";

    /// <summary>One "Label: value" line per value, "\n" line endings, final newline.</summary>
    public static string PlainText(SpeedTestSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.Append(Title).Append(" — ").Append(Heading(snapshot)).Append('\n');
        foreach (var (label, value) in Rows(snapshot))
        {
            text.Append(label).Append(": ").Append(value).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>A two-column table headed by the title and the date; server-supplied text is escaped.</summary>
    public static string Markdown(SpeedTestSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.Append("| ").Append(Title).Append(" | ").Append(MarkdownText.Escape(Heading(snapshot))).Append(" |\n|---|---|\n");
        foreach (var (label, value) in Rows(snapshot))
        {
            text.Append("| **").Append(label).Append("** | ").Append(MarkdownText.Escape(value)).Append(" |\n");
        }

        return text.ToString();
    }

    /// <summary>The test time once there is one, otherwise the status line as plain text.</summary>
    private static string Heading(SpeedTestSnapshot snapshot) =>
        snapshot.CompletedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        ?? MeterMarkdown.StatusLine(snapshot).Replace("**", string.Empty, StringComparison.Ordinal);

    private static IEnumerable<(string Label, string Value)> Rows(SpeedTestSnapshot snapshot)
    {
        var c = snapshot.Connection;
        yield return ("Download", SpeedFormatter.Speed(snapshot.DownloadMbps));
        yield return ("Upload", SpeedFormatter.Speed(snapshot.UploadMbps));
        yield return ("Latency", SpeedFormatter.Latency(snapshot.LatencyMs)
            + (snapshot.JitterMs is null ? string.Empty : ", jitter " + SpeedFormatter.Latency(snapshot.JitterMs)));
        yield return ("ISP", Field(c.Isp));
        yield return ("Location", Field(c.Location));
        yield return ("Server", Field(c.Server));
    }

    private static string Field(string? value) => string.IsNullOrEmpty(value) ? SpeedFormatter.Unknown : value;
}
