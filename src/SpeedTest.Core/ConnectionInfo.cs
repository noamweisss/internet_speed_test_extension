using System.Linq;
using System.Net.Http.Headers;

namespace SpeedTest.Core;

/// <summary>
/// What Cloudflare reports about the client. Displayed only; never logged or persisted (docs/SECURITY.md).
/// </summary>
/// <param name="Isp">Network operator name (autonomous system organisation).</param>
/// <param name="Ip">Public IP address as seen by the server.</param>
/// <param name="City">City of the client, geolocated by Cloudflare.</param>
/// <param name="Country">ISO country code of the client.</param>
/// <param name="Server">Cloudflare data-centre code (IATA airport code) that served the test.</param>
public sealed record ConnectionInfo(string? Isp, string? Ip, string? City, string? Country, string? Server)
{
    public static ConnectionInfo Empty { get; } = new(null, null, null, null, null);

    /// <summary>
    /// Every response from speed.cloudflare.com carries these headers; used when /meta is unavailable.
    /// Cloudflare documents the <c>cf-meta-*</c> names and has also been observed sending bare names, so both are read.
    /// </summary>
    public static ConnectionInfo FromHeaders(HttpResponseHeaders headers)
    {
        string? Get(string name) => headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
        string? First(string preferred, string fallback) => Get(preferred) is { Length: > 0 } value ? value : Get(fallback);
        return new(
            Isp: null,
            Ip: Get("cf-meta-ip"),
            City: First("cf-meta-city", "city"),
            Country: First("cf-meta-country", "country"),
            Server: First("cf-meta-colo", "colo"));
    }

    /// <summary>Fills each null field of this instance from <paramref name="fallback"/>.</summary>
    public ConnectionInfo FillFrom(ConnectionInfo fallback) => new(
        Isp ?? fallback.Isp, Ip ?? fallback.Ip, City ?? fallback.City, Country ?? fallback.Country, Server ?? fallback.Server);

    /// <summary>"City, CC" or whichever part is known; empty when nothing is.</summary>
    public string Location => string.Join(", ", new[] { City, Country }.Where(s => !string.IsNullOrEmpty(s)));
}
