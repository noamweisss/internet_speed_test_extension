using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedTest.Core;

/// <summary>
/// Upload body: a fixed number of zero bytes streamed in chunks. Contains no user data.
/// Reports each chunk as it is handed to the network stream so upload progress is live.
/// </summary>
internal sealed class ZeroContent : HttpContent
{
    private const int ChunkSize = 64 * 1024;
    private static readonly byte[] Zeros = new byte[ChunkSize];
    private readonly long _length;
    private readonly Action<int> _onBytesSent;

    public ZeroContent(long length, Action<int> onBytesSent)
    {
        _length = length;
        _onBytesSent = onBytesSent;
        Headers.ContentType = new("application/octet-stream");
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        var remaining = _length;
        while (remaining > 0)
        {
            var count = (int)Math.Min(ChunkSize, remaining);
            await stream.WriteAsync(Zeros.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            remaining -= count;
            _onBytesSent(count);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length;
        return true;
    }
}
