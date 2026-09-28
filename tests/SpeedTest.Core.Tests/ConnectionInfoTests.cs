using System.Net.Http;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class ConnectionInfoTests
{
    [Fact]
    public void FromHeaders_ReadsDocumentedCfMetaHeaders()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("cf-meta-city", "Haifa");
        response.Headers.Add("cf-meta-country", "IL");
        response.Headers.Add("cf-meta-colo", "HFA");

        Assert.Equal(new ConnectionInfo(null, "198.51.100.9", "Haifa", "IL", "HFA"), ConnectionInfo.FromHeaders(response.Headers));
    }

    [Fact]
    public void FromHeaders_FallsBackToBareHeaderNames()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("city", "Haifa");
        response.Headers.Add("country", "IL");
        response.Headers.Add("colo", "HFA");

        Assert.Equal(new ConnectionInfo(null, "198.51.100.9", "Haifa", "IL", "HFA"), ConnectionInfo.FromHeaders(response.Headers));
    }

    [Fact]
    public void FromHeaders_DecodesPercentEncodedUtf8()
    {
        // The real "city" header for Holon (with a macron below the H) arrives as "H%CC%B1olon".
        using var response = new HttpResponseMessage();
        response.Headers.Add("city", "H%CC%B1olon");

        Assert.Equal("H̱olon", ConnectionInfo.FromHeaders(response.Headers).City);
    }

    [Theory]
    [InlineData("Tel%ZZAviv")]
    [InlineData("50%")]
    public void FromHeaders_KeepsInvalidEscapesAsIs(string value)
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("city", value);

        Assert.Equal(value, ConnectionInfo.FromHeaders(response.Headers).City);
    }

    [Fact]
    public void FromHeaders_IgnoresOverlongValues()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("city", new string('x', 257));
        response.Headers.Add("country", new string('y', 256));

        var info = ConnectionInfo.FromHeaders(response.Headers);

        Assert.Null(info.City);
        Assert.Equal(256, info.Country!.Length);
    }

    [Fact]
    public void FillFrom_OnlyReplacesNulls()
    {
        var primary = new ConnectionInfo("ISP", null, "Tel Aviv", null, null);
        var fallback = new ConnectionInfo("Other", "1.2.3.4", "Haifa", "IL", "HFA");

        Assert.Equal(new ConnectionInfo("ISP", "1.2.3.4", "Tel Aviv", "IL", "HFA"), primary.FillFrom(fallback));
    }

    [Theory]
    [InlineData("Tel Aviv", "IL", "Tel Aviv, IL")]
    [InlineData(null, "IL", "IL")]
    [InlineData(null, null, "")]
    public void Location_JoinsKnownParts(string? city, string? country, string expected) =>
        Assert.Equal(expected, new ConnectionInfo(null, null, city, country, null).Location);
}
