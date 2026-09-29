using System;
using System.Globalization;
using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Draws one speed as a semicircular speedometer arc (ADR-0011). Pure function, so the geometry is unit-tested; the
/// meter embeds the result as a base64 data URI. No text inside: the numbers stay markdown so they follow the
/// host theme. Colours are theme-neutral for the same reason (an image cannot follow the theme).
/// </summary>
public static class GaugeSvg
{
    private const double CenterX = 100;
    private const double CenterY = 100;
    private const double Radius = 80;

    // The xmlns value is the SVG namespace name, which a renderer compares and never fetches; rule R6 in
    // scripts/check.sh names it as the one allowed http:// string. Single quotes save escaping in the C# literal.
    private const string Header =
        "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 110' width='220' height='121'>";

    private const string Arc = "M20 100 A80 80 0 ";
    private const string Stroke = "' fill='none' stroke-width='14' stroke-linecap='round'/>";

    /// <summary>The gauge for <paramref name="value"/> on a scale ending at <paramref name="max"/>, clamped to [0, 1].</summary>
    public static string Render(double value, double max)
    {
        var fraction = max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
        var svg = new StringBuilder(Header);
        // Track: a half-transparent mid gray reads on both the light and the dark theme. stroke-opacity rather than
        // an 8-digit hex colour, which SVG 1.1 renderers such as Direct2D do not understand.
        svg.Append("<path d='").Append(Arc).Append("0 1 180 100' stroke='#808080' stroke-opacity='0.5").Append(Stroke);
        if (fraction > 0)
        {
            // Sweep from the left end (180°) over the top to the right end (0°), clockwise on screen. The sweep is
            // never more than a semicircle, so the large-arc flag stays 0: with 1 the arc would take the long way
            // round, below the baseline (CodeRabbit review).
            var angle = Math.PI * (1 - fraction);
            var x = CenterX + Radius * Math.Cos(angle);
            var y = CenterY - Radius * Math.Sin(angle);
            svg.Append("<path d='").Append(Arc).Append("0 1 ")
                .Append(Number(x)).Append(' ').Append(Number(y))
                .Append("' stroke='#0078D4").Append(Stroke);
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary><see cref="Render"/> as a markdown-embeddable image source.</summary>
    public static string DataUri(double value, double max) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(value, max)));

    private static string Number(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
}
