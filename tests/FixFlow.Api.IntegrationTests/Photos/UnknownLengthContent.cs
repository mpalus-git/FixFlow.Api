using System.Net;

namespace FixFlow.Api.IntegrationTests.Photos;

public sealed class UnknownLengthContent(byte[] content) : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        stream.WriteAsync(content).AsTask();

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
