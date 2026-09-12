namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Wraps a stream (typically a MemoryStream) so disposing the wrapper -- e.g. an
/// IStreamCipher's write stream, which disposes its underlying stream when done -- doesn't
/// close the inner stream, letting tests inspect its bytes afterward.
/// </summary>
public sealed class NonDisposingStream(Stream inner) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        // Deliberately does not dispose `inner` -- that's the point of this class.
    }
}
