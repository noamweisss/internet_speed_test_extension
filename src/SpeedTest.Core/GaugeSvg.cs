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
    // The track's width and height in the viewBox below.
    private const double Width = 240;
    private const double TrackHeight = 12;

    // Numeric width and height, at most 256, and a viewBox: a data: image gets no size hints, is capped at 256 DIP,
    // and without numeric sizes the host's size probe overflows (docs/CMDPAL-RENDERING.md §4).
    // No xmlns: the host's SVG sniff and size probe ignore the namespace, Direct2D draws an SVG without one (tested on
    // the owner's laptop, 2026-09-29), and Microsoft's own Performance Monitor charts omit it (ADR-0017). Without the
    // namespace name there is no http:// string, so rule R6 needs no exception. Single quotes save escaping.
    private const string Header = "<svg viewBox='0 0 240 20' width='240' height='20'>";

    // A half-transparent mid gray reads on both the light and the dark theme. Opacity attributes, never 8-digit hex
    // colours: Direct2D draws those black.
    private const string Track = "<rect x='0' y='4' width='240' height='12' rx='6' fill='#808080' fill-opacity='0.35'/>";

    // Drawn after the fill, so they show over it.
    private const string Ticks =
        "<line x1='60' y1='0' x2='60' y2='20' stroke='#808080' stroke-opacity='0.5' stroke-width='1'/>"
        + "<line x1='120' y1='0' x2='120' y2='20' stroke='#808080' stroke-opacity='0.5' stroke-width='1'/>"
        + "<line x1='180' y1='0' x2='180' y2='20' stroke='#808080' stroke-opacity='0.5' stroke-width='1'/>";

    /// <summary>The bar for <paramref name="value"/> on a scale ending at <paramref name="max"/>, clamped to [0, 1].</summary>
    public static string Render(double value, double max)
    {
        var fraction = max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
        var svg = new StringBuilder(Header).Append(Track);
        if (fraction > 0)
        {
            // A fill narrower than its height cannot fit both rounded ends: renderers shrink the corner radius and
            // it draws as a thin sliver, not a small pill. So a value above zero shows at least a full round dot.
            var width = Math.Max(fraction * Width, TrackHeight);
            svg.Append("<rect x='0' y='4' width='").Append(Number(width))
                .Append("' height='12' rx='6' fill='#0078D4'/>");
        }

        return svg.Append(Ticks).Append("</svg>").ToString();
    }

    /// <summary><see cref="Render"/> as a markdown-embeddable image source.</summary>
    public static string DataUri(double value, double max) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(value, max)));

    private static string Number(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
}
