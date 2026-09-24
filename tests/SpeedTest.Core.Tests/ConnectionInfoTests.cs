using System.Net.Http;
using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class ConnectionInfoTests
{
    [Fact]
    public void FromHeaders_ReadsCloudflareMetaHeaders()
    {
        using var response = new HttpResponseMessage();
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("city", "Haifa");
        response.Headers.Add("country", "IL");
        response.Headers.Add("colo", "HFA");

        Assert.Equal(new ConnectionInfo(null, "198.51.100.9", "Haifa", "IL", "HFA"), ConnectionInfo.FromHeaders(response.Headers));
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
