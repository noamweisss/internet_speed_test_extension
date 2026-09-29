using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class GaugeSvgTests
{
    private const string Track = "M20 100 A80 80 0 0 1 180 100";

    [Fact]
    public void Render_Any_HasNumericSizeViewBoxAndNoNamespace()
    {
        var svg = GaugeSvg.Render(65, 100);

        // Numeric width and height of at most 256 and a viewBox (docs/CMDPAL-RENDERING.md §4); no xmlns (ADR-0017).
        Assert.StartsWith("<svg viewBox='0 0 200 110' width='200' height='110'>", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("xmlns", svg);
        Assert.DoesNotContain("http", svg);
    }

    [Fact]
    public void Render_Any_UsesOnlyElementsAndAttributesDirect2DDraws()
    {
        var svg = GaugeSvg.Render(65, 100);

        var elements = Regex.Matches(svg, "<([a-zA-Z]+)").Select(m => m.Groups[1].Value).Distinct();
        var attributes = Regex.Matches(svg, " ([a-zA-Z-]+)=").Select(m => m.Groups[1].Value).Distinct();
        Assert.All(elements, e => Assert.Contains(e, new[] { "svg", "path", "line" }));
        Assert.All(attributes, a => Assert.Contains(a, new[]
        {
            "viewBox", "width", "height", "d", "x1", "y1", "x2", "y2",
            "stroke", "stroke-width", "stroke-opacity", "stroke-linecap", "fill",
        }));
    }

    [Fact]
    public void Render_Any_HasNoTextAndNoEightDigitColour()
    {
        var svg = GaugeSvg.Render(65, 100);

        Assert.DoesNotContain("<text", svg);
        Assert.DoesNotMatch("#[0-9A-Fa-f]{8}", svg);
    }

    [Fact]
    public void Render_Zero_HasTrackAndNoProgress()
    {
        var svg = GaugeSvg.Render(0, 100);

        Assert.Contains(Track, svg);
        Assert.Contains("stroke='#808080' stroke-opacity='0.35'", svg);
        Assert.DoesNotContain("#0078D4", svg);
        Assert.True(svg.Length < 1000, $"svg is {svg.Length} bytes");
    }

    [Fact]
    public void Render_Any_HasFiveTicksOutsideTheTrack()
    {
        var svg = GaugeSvg.Render(0, 100);

        Assert.Equal(5, Regex.Count(svg, "<line "));
        // 0, 50 and 100 %: the left end, the top and the right end, from radius 90 to 98.
        Assert.Contains("<line x1='10' y1='100' x2='2' y2='100' stroke='#808080' stroke-opacity='0.5' stroke-width='2'/>", svg);
        Assert.Contains("<line x1='100' y1='10' x2='100' y2='2'", svg);
        Assert.Contains("<line x1='190' y1='100' x2='198' y2='100'", svg);
        // 25 %: 135° from the right, (100 + r·cos 135°, 100 − r·sin 135°).
        Assert.Contains("<line x1='36.4' y1='36.4' x2='30.7' y2='30.7'", svg);
    }

    [Fact]
    public void Render_Half_EndsAtTop()
    {
        var svg = GaugeSvg.Render(50, 100);

        Assert.Contains("M20 100 A80 80 0 0 1 100 20' stroke='#0078D4'", svg);
    }

    [Fact]
    public void Render_Full_EndsAtRightEnd()
    {
        var svg = GaugeSvg.Render(100, 100);

        Assert.Contains("M20 100 A80 80 0 0 1 180 100' stroke='#0078D4'", svg);
    }

    [Fact]
    public void Render_AboveMax_IsClamped()
    {
        Assert.Equal(GaugeSvg.Render(100, 100), GaugeSvg.Render(150, 100));
    }

    [Fact]
    public void Render_ZeroMax_DrawsNoProgress()
    {
        Assert.Equal(GaugeSvg.Render(0, 100), GaugeSvg.Render(40, 0));
    }

    [Fact]
    public void Render_Any_UsesInvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var svg = GaugeSvg.Render(65, 100);

            // 65 % is 117° past the left end, the short way round (large-arc flag 0): (100 + 80·cos 63°, 100 − 80·sin 63°).
            Assert.Contains("A80 80 0 0 1 136.3 28.7", svg);
            Assert.DoesNotContain(",", svg);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void DataUri_Any_HasSvgPrefixAndDecodesToTheSvg()
    {
        const string prefix = "data:image/svg+xml;base64,";

        var uri = GaugeSvg.DataUri(65, 100);

        Assert.StartsWith(prefix, uri, StringComparison.Ordinal);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(uri.Substring(prefix.Length)));
        Assert.Equal(GaugeSvg.Render(65, 100), decoded);
    }
}
