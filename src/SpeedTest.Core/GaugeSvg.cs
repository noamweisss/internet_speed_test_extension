using System;
using System.Globalization;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Draws one speed as a semicircular speedometer dial (ADR-0017, restoring the gauge of ADR-0011). Pure function, so
/// the geometry is unit-tested; the meter embeds the result as a base64 data URI in its own markdown block. No text
/// inside: the numbers stay markdown so they follow the host theme, and Direct2D drops SVG text anyway. Colours are
/// theme-neutral for the same reason (an image cannot follow the theme, docs/CMDPAL-RENDERING.md §3).
/// Only the elements and attributes Direct2D draws (§4): svg, path, line; stroke, stroke-width, stroke-opacity,
/// stroke-linecap, fill.
/// </summary>
public static class GaugeSvg
{
    private const double CenterX = 100;
    private const double CenterY = 100;
    private const double Radius = 80;

    // Tick marks sit outside the track (radius 80, stroke 14, so its outer edge is at 87) at 0, 25, 50, 75 and 100 %.
    private const double TickInnerRadius = 90;
    private const double TickOuterRadius = 98;
    private const int TickIntervals = 4;

    // No xmlns attribute, so the string holds no namespace URL and rule R6 needs no exemption. The host does not need it:
    // its SVG sniff looks for "<svg" and its size probe reads the root's width and height with or without a namespace
    // (PowerToys ImageSourceFactory.cs:87-102, 137), and Direct2D's CreateSvgDocument draws an SVG with no namespace,
    // or a wrong one (rendered on the owner's laptop, 2026-09-29). Microsoft's Performance Monitor extension emits
    // its chart SVG without one (ChartHelper.cs). Numeric width and height of at most 256 plus a viewBox: without
    // numbers the host's rasterisation size overflows, and a data: image is capped at 256 DIP (§4). Single quotes
    // save escaping in the C# literal.
    private const string Header = "<svg viewBox='0 0 200 110' width='200' height='110'>";

    private const string Arc = "M20 100 A80 80 0 ";
    private const string Stroke = "' fill='none' stroke-width='14' stroke-linecap='round'/>";

    /// <summary>The gauge for <paramref name="value"/> on a scale ending at <paramref name="max"/>, clamped to [0, 1].</summary>
    public static string Render(double value, double max)
    {
        var fraction = max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
        var svg = new StringBuilder(Header);
        for (var tick = 0; tick <= TickIntervals; tick++)
        {
            var at = (double)tick / TickIntervals;
            svg.Append("<line x1='").Append(X(at, TickInnerRadius)).Append("' y1='").Append(Y(at, TickInnerRadius))
                .Append("' x2='").Append(X(at, TickOuterRadius)).Append("' y2='").Append(Y(at, TickOuterRadius))
                .Append("' stroke='#808080' stroke-opacity='0.5' stroke-width='2'/>");
        }

        // Track: a light mid gray reads on both the light and the dark theme. stroke-opacity rather than an 8-digit
        // hex colour, which Direct2D does not understand and draws black (Verified on the owner's laptop).
        svg.Append("<path d='").Append(Arc).Append("0 1 180 100' stroke='#808080' stroke-opacity='0.35").Append(Stroke);
        if (fraction > 0)
        {
            // Sweep from the left end (180°) over the top to the right end (0°), clockwise on screen. The sweep is
            // never more than a semicircle, so the large-arc flag stays 0: with 1 the arc would take the long way
            // round, below the baseline (CodeRabbit review). The blue is the Windows default accent: the host does
            // not tell an extension the user's accent.
            svg.Append("<path d='").Append(Arc).Append("0 1 ")
                .Append(X(fraction, Radius)).Append(' ').Append(Y(fraction, Radius))
                .Append("' stroke='#0078D4").Append(Stroke);
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary><see cref="Render"/> as a markdown-embeddable image source.</summary>
    public static string DataUri(double value, double max) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(value, max)));

    // A point on the dial at a share of the sweep: 0 is the left end, 1 the right end, over the top.
    private static string X(double fraction, double radius) => Number(CenterX + (radius * Math.Cos(Angle(fraction))));

    private static string Y(double fraction, double radius) => Number(CenterY - (radius * Math.Sin(Angle(fraction))));

    private static double Angle(double fraction) => Math.PI * (1 - fraction);

    private static string Number(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
}
