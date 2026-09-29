using System;
using System.Globalization;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Draws one speed as a horizontal bar gauge (ADR-0017): a rounded track, a rounded fill, and tick marks at the
/// quarters. Pure function, so the geometry is unit-tested; the meter embeds the result as a base64 data URI. No text
/// inside, because the host's SVG renderer drops it: the numbers stay markdown, which also follows the host theme.
/// Colours are theme-neutral for the same reason (an image cannot follow the theme).
/// </summary>
public static class GaugeSvg
{
    // The image's size in viewBox units, and the track, centred in its height with fully rounded ends.
    private const double Width = 240;
    private const double Height = 20;
    private const double TrackHeight = 12;
    private const double TrackY = (Height - TrackHeight) / 2;
    private const double CornerRadius = TrackHeight / 2;

    // A fill narrower than its height cannot fit both rounded ends: renderers shrink the corner radius and it draws
    // as a thin sliver, not a small pill. So a value above zero shows at least a full round dot.
    private const double MinimumFillWidth = TrackHeight;

    // The ticks divide the track into this many equal parts.
    private const int TickParts = 4;

    // A half-transparent mid gray reads on both the light and the dark theme; the track and the ticks share it.
    // Opacity attributes, never 8-digit hex colours: Direct2D draws those black.
    private const string TrackColour = "#808080";
    private const string FillColour = "#0078D4";

    // Numeric width and height, at most 256, and a viewBox: a data: image gets no size hints, is capped at 256 DIP,
    // and without numeric sizes the host's size probe overflows (docs/CMDPAL-RENDERING.md §4).
    // No xmlns: the host's SVG sniff and size probe ignore the namespace, and Direct2D draws an SVG without one (tested
    // on the owner's laptop and in the host in the test VM, 2026-09-29); Microsoft's own Performance Monitor charts
    // omit it too (ADR-0017). No namespace URL, so rule R6 needs no exception. Single quotes save escaping.
    private static readonly string Header =
        $"<svg viewBox='0 0 {Number(Width)} {Number(Height)}' width='{Number(Width)}' height='{Number(Height)}'>";

    private static readonly string Track = Bar(Width, $"fill='{TrackColour}' fill-opacity='0.35'");

    // Drawn after the fill, so they show over it.
    private static readonly string Ticks = DrawTicks();

    /// <summary>The bar for <paramref name="value"/> on a scale ending at <paramref name="max"/>, clamped to [0, 1].</summary>
    public static string Render(double value, double max)
    {
        var fraction = max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
        var svg = new StringBuilder(Header).Append(Track);
        if (fraction > 0)
        {
            svg.Append(Bar(Math.Max(fraction * Width, MinimumFillWidth), $"fill='{FillColour}'"));
        }

        return svg.Append(Ticks).Append("</svg>").ToString();
    }

    /// <summary><see cref="Render"/> as a markdown-embeddable image source.</summary>
    public static string DataUri(double value, double max) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(value, max)));

    // The track and the fill are the same rounded rectangle at different widths.
    private static string Bar(double width, string paint) =>
        $"<rect x='0' y='{Number(TrackY)}' width='{Number(width)}' height='{Number(TrackHeight)}'"
        + $" rx='{Number(CornerRadius)}' {paint}/>";

    private static string DrawTicks()
    {
        var ticks = new StringBuilder();
        for (var k = 1; k < TickParts; k++)
        {
            var x = Number(Width * k / TickParts);
            ticks.Append($"<line x1='{x}' y1='0' x2='{x}' y2='{Number(Height)}'")
                .Append($" stroke='{TrackColour}' stroke-opacity='0.5' stroke-width='1'/>");
        }

        return ticks.ToString();
    }

    private static string Number(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
}
