using System;
using System.Collections.Generic;
using System.IO;
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

    public HttpStatusCode MetaStatus { get; set; } = HttpStatusCode.OK;

    public HttpStatusCode DownloadStatus { get; set; } = HttpStatusCode.OK;

    public bool ThrowOnEveryRequest { get; set; }

    /// <summary>When set, download bodies throw this once read, simulating a connection reset mid-transfer.</summary>
    public bool ResetDuringDownload { get; set; }

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
            return new HttpResponseMessage(MetaStatus) { Content = new StringContent(MetaJson, System.Text.Encoding.UTF8, "application/json") };
        }

        if (path == "/__up")
        {
            // Read the body so ZeroContent actually streams (and counts) its bytes.
            await request.Content!.CopyToAsync(Stream.Null, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        var bytes = long.Parse(request.RequestUri.Query.Replace("?bytes=", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
        HttpContent body = ResetDuringDownload && bytes > 0
            ? new StreamContent(new ResettingStream())
            : new ByteArrayContent(new byte[bytes]);
        var response = new HttpResponseMessage(DownloadStatus) { Content = body };
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("cf-meta-city", "Haifa");
        response.Headers.Add("cf-meta-country", "IL");
        response.Headers.Add("cf-meta-colo", "HFA");
        if (ServerDurationMs > 0)
        {
            response.Headers.Add("Server-Timing", "cfRequestDuration;dur=" + ServerDurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return response;
    }

    /// <summary>A body whose first read fails like a dropped connection.</summary>
    private sealed class ResettingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("simulated connection reset");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
