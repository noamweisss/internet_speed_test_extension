using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest.Core.Tests;

/// <summary>
/// In-memory stand-in for speed.cloudflare.com. Answers /meta, /__down?bytes=N and /__up like the real
/// service, records every request, and can be told to fail. No test touches the network (docs/TESTING.md).
/// </summary>
internal sealed class FakeCloudflareHandler : HttpMessageHandler
{
    public List<Uri> Requests { get; } = new();

    public string MetaJson { get; set; } = """{"clientIp":"203.0.113.7","asOrganization":"Example ISP","city":"Tel Aviv","country":"IL","colo":"TLV"}""";

    public HttpStatusCode DownloadStatus { get; set; } = HttpStatusCode.OK;

    public bool ThrowOnEveryRequest { get; set; }

    public double ServerDurationMs { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request.RequestUri!);
        }

        if (ThrowOnEveryRequest)
        {
            throw new HttpRequestException("simulated network failure");
        }

        var path = request.RequestUri!.AbsolutePath;
        if (path == "/meta")
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(MetaJson, System.Text.Encoding.UTF8, "application/json") };
        }

        if (path == "/__up")
        {
            // Read the body so ZeroContent actually streams (and counts) its bytes.
            await request.Content!.CopyToAsync(System.IO.Stream.Null, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        var bytes = long.Parse(request.RequestUri.Query.Replace("?bytes=", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
        var response = new HttpResponseMessage(DownloadStatus) { Content = new ByteArrayContent(new byte[bytes]) };
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("city", "Haifa");
        response.Headers.Add("country", "IL");
        response.Headers.Add("colo", "HFA");
        if (ServerDurationMs > 0)
        {
            response.Headers.Add("Server-Timing", "cfRequestDuration;dur=" + ServerDurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return response;
    }
}
