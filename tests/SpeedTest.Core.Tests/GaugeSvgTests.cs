using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class GaugeSvgTests
{
    private const string Track = "M20 100 A80 80 0 0 1 180 100";

    [Fact]
    public void Render_Zero_HasTrackAndNoProgress()
    {
        var svg = GaugeSvg.Render(0, 100);

        Assert.StartsWith("<svg xmlns=", svg, StringComparison.Ordinal);
        Assert.Contains("viewBox='0 0 200 110' width='220' height='121'", svg);
        Assert.Contains(Track, svg);
        Assert.Contains("stroke='#808080' stroke-opacity='0.5'", svg);
        Assert.DoesNotContain("#0078D4", svg);
        Assert.True(svg.Length < 600, $"svg is {svg.Length} bytes");
    }

    [Fact]
    public void Render_Half_EndsAtTop()
    {
        var svg = GaugeSvg.Render(50, 100);

        Assert.Contains("M20 100 A80 80 0 0 1 100 20", svg);
        Assert.Contains("#0078D4", svg);
    }

    [Fact]
    public void Render_Full_EndsAtRightEnd()
    {
        var svg = GaugeSvg.Render(100, 100);

        Assert.Contains("M20 100 A80 80 0 0 1 180 100' stroke='#0078D4'", svg);
        Assert.Contains("#0078D4", svg);
    }

    [Fact]
    public void Render_AboveMax_IsClamped()
    {
        Assert.Equal(GaugeSvg.Render(100, 100), GaugeSvg.Render(150, 100));
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
