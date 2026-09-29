using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class GaugeSvgTests
{
    private const string Fill = "fill='#0078D4'";

    [Fact]
    public void Render_Zero_HasTrackAndTicksAndNoFill()
    {
        var svg = GaugeSvg.Render(0, 100);

        Assert.StartsWith("<svg viewBox='0 0 240 20' width='240' height='20'>", svg, StringComparison.Ordinal);
        Assert.Contains("<rect x='0' y='4' width='240' height='12' rx='6' fill='#808080' fill-opacity='0.35'/>", svg);
        Assert.Equal(3, Regex.Count(svg, "<line "));
        Assert.DoesNotContain(Fill, svg);
        Assert.True(svg.Length < 600, $"svg is {svg.Length} bytes");
    }

    [Fact]
    public void Render_Full_FillsWholeTrack()
    {
        Assert.Contains("width='240' height='12' rx='6' " + Fill, GaugeSvg.Render(250, 250));
    }

    [Theory]
    [InlineData(150, 100, 100)]
    [InlineData(-5, 100, 0)]
    public void Render_OutsideScale_IsClamped(double value, double max, double clampedValue)
    {
        Assert.Equal(GaugeSvg.Render(clampedValue, max), GaugeSvg.Render(value, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Render_MaxNotPositive_IsEmptyBar(double max)
    {
        Assert.DoesNotContain(Fill, GaugeSvg.Render(40, max));
    }

    [Fact]
    public void Render_TinyValue_DrawsAtLeastTheBarHeight()
    {
        // 0.1 of 250 is 0.1 units wide; narrower than 12 the rounded ends would not fit.
        Assert.Contains("width='12' height='12' rx='6' " + Fill, GaugeSvg.Render(0.1, 250));
    }

    [Fact]
    public void Render_Any_DrawsTicksOverTheFill()
    {
        var svg = GaugeSvg.Render(250, 250);

        Assert.True(svg.IndexOf(Fill, StringComparison.Ordinal) < svg.IndexOf("<line ", StringComparison.Ordinal), "ticks before fill");
        Assert.Contains("<line x1='120' y1='0' x2='120' y2='20' stroke='#808080' stroke-opacity='0.5' stroke-width='1'/>", svg);
    }

    [Fact]
    public void Render_Any_UsesInvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var svg = GaugeSvg.Render(180, 250);

            // 180 of 250 is 72 % of 240 units.
            Assert.Contains("width='172.8'", svg);
            Assert.DoesNotContain(",", svg);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_Any_StaysInsideWhatTheHostDraws()
    {
        var svg = GaugeSvg.Render(180, 250);

        // No namespace (ADR-0017, rule R6), no text (Direct2D drops it), no 8-digit colours (Direct2D draws them black).
        Assert.DoesNotContain("xmlns", svg);
        Assert.DoesNotContain("<text", svg);
        Assert.DoesNotMatch("#[0-9A-Fa-f]{8}", svg);
    }

    [Fact]
    public void DataUri_Any_HasSvgPrefixAndDecodesToTheSvg()
    {
        const string prefix = "data:image/svg+xml;base64,";

        var uri = GaugeSvg.DataUri(180, 250);

        Assert.StartsWith(prefix, uri, StringComparison.Ordinal);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(uri.Substring(prefix.Length)));
        Assert.Equal(GaugeSvg.Render(180, 250), decoded);
    }
}
