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
    private bool _firstProbeSeen;

    public List<Uri> Requests { get; } = new();

    /// <summary>
    /// The real /meta shape as observed on 2026-09-28 (values replaced with documentation addresses and names):
    /// <c>colo</c> is an object, not a string.
    /// </summary>
    public string MetaJson { get; set; } = """{"hostname":"speed.cloudflare.com","clientIp":"203.0.113.7","httpProtocol":"HTTP/1.1","asn":64496,"asOrganization":"Example ISP","country":"IL","city":"Tel Aviv","region":"Tel Aviv","latitude":"32.0","longitude":"34.7","colo":{"iata":"TLV","lat":32.011398,"lon":34.8867,"cca2":"IL","region":"Middle East","city":"Tel Aviv"}}""";

    public HttpStatusCode MetaStatus { get; set; } = HttpStatusCode.OK;

    /// <summary>
    /// The real /meta answers <c>403 {}</c> unless the request carries <c>Referer: https://speed.cloudflare.com/</c>
    /// (observed 2026-09-28). On by default so tests exercise the real behaviour; off to test a plain failure.
    /// </summary>
    public bool RequireMetaReferer { get; set; } = true;

    /// <summary>The Referer the measurer sent on its last /meta request, or null when it sent none.</summary>
    public Uri? MetaReferer { get; private set; }

    /// <summary>
    /// Delay before the first latency probe answers, standing in for the DNS + TCP + TLS setup that the first request
    /// on a cold connection pays. Later probes answer at once.
    /// </summary>
    public TimeSpan FirstProbeDelay { get; set; }


    public HttpStatusCode DownloadStatus { get; set; } = HttpStatusCode.OK;

    public bool ThrowOnEveryRequest { get; set; }

    /// <summary>When set, download bodies throw this once read, simulating a connection reset mid-transfer.</summary>
    public bool ResetDuringDownload { get; set; }

    /// <summary>When set, /meta sends headers and then never sends a body, simulating a stalled server.</summary>
    public bool StallMetaBody { get; set; }

    /// <summary>Extra bytes a download response carries beyond what was requested. Negative values are ignored.</summary>
    public int DownloadExtraBytes { get; set; }

    /// <summary>When set, download responses omit Content-Length (chunked-style), so only the read loop can bound them.</summary>
    public bool DownloadWithoutContentLength { get; set; }

    /// <summary>Bytes the measurer actually read from each length-unknown download body, in order.</summary>
    public List<long> BytesReadPerLengthUnknownResponse { get; } = new();

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
            MetaReferer = request.Headers.Referrer;
            if (RequireMetaReferer && request.Headers.Referrer != CloudflareEndpoints.Base)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
            }

            HttpContent metaBody = StallMetaBody
                ? new StreamContent(new StallingStream())
                : new StringContent(MetaJson, System.Text.Encoding.UTF8, "application/json");
            return new HttpResponseMessage(MetaStatus) { Content = metaBody };
        }

        if (path == "/__up")
        {
            // Read the body so ZeroContent actually streams (and counts) its bytes.
            await request.Content!.CopyToAsync(Stream.Null, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        var bytes = long.Parse(request.RequestUri.Query.Replace("?bytes=", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
        if (bytes == 0 && !_firstProbeSeen)
        {
            _firstProbeSeen = true;
            if (FirstProbeDelay > TimeSpan.Zero)
            {
                await Task.Delay(FirstProbeDelay, cancellationToken);
            }
        }

        var payload = new byte[bytes + (bytes > 0 ? Math.Max(0, DownloadExtraBytes) : 0)];
        HttpContent body = ResetDuringDownload && bytes > 0
            ? new StreamContent(new ResettingStream())
            : DownloadWithoutContentLength && bytes > 0
                ? new StreamContent(new NonSeekableStream(payload, this))
                : new ByteArrayContent(payload);
        var response = new HttpResponseMessage(DownloadStatus) { Content = body };
        // The real probe responses carry cf-meta-ip plus bare city/country/colo, with city percent-encoded UTF-8
        // ("H%CC%B1olon" is "H̱olon", observed 2026-09-28).
        response.Headers.Add("cf-meta-ip", "198.51.100.9");
        response.Headers.Add("city", "H%CC%B1olon");
        response.Headers.Add("country", "IL");
        response.Headers.Add("colo", "HFA");
        if (ServerDurationMs > 0)
        {
            response.Headers.Add("Server-Timing", "cfRequestDuration;dur=" + ServerDurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return response;
    }

    /// <summary>A body whose reads never complete until cancelled, like a server that stalls after the headers.</summary>
    private sealed class StallingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("use ReadAsync");

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A readable body with unknown length, so StreamContent cannot set Content-Length. Reports bytes read on dispose.</summary>
    private sealed class NonSeekableStream(byte[] data, FakeCloudflareHandler owner) : Stream
    {
        private int _position;
        private bool _reported;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = Math.Min(count, data.Length - _position);
            Array.Copy(data, _position, buffer, offset, n);
            _position += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_reported)
            {
                _reported = true;
                lock (owner.BytesReadPerLengthUnknownResponse)
                {
                    owner.BytesReadPerLengthUnknownResponse.Add(_position);
                }
            }

            base.Dispose(disposing);
        }
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
