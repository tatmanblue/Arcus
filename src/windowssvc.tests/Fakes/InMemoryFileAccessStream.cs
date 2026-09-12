using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Backs a single write session (accumulates bytes, hands them to onClose on Close/Dispose)
/// or a single read session (reads from pre-existing bytes) -- never both, matching how
/// LocalDataAccessStream is used in practice (AddRequest vs. GetFileStream/GetRequest).
/// </summary>
public class InMemoryFileAccessStream : IFileAccessStream
{
    private readonly MemoryStream buffer;
    private readonly Action<byte[]>? onClose;

    public InMemoryFileAccessStream(Action<byte[]> onClose)
    {
        buffer = new MemoryStream();
        this.onClose = onClose;
    }

    public InMemoryFileAccessStream(byte[] existingContent)
    {
        buffer = new MemoryStream(existingContent);
        onClose = null;
    }

    public Task WriteBytes(byte[] chunk)
    {
        buffer.Write(chunk, 0, chunk.Length);
        return Task.CompletedTask;
    }

    public Task<int> ReadBytes(byte[] chunk, int chunkSize)
    {
        int read = buffer.Read(chunk, 0, chunkSize);
        return Task.FromResult(read);
    }

    public Task LocalCopy(string source)
    {
        byte[] bytes = File.ReadAllBytes(source);
        buffer.Write(bytes, 0, bytes.Length);
        File.Delete(source);
        return Task.CompletedTask;
    }

    public void Close()
    {
        onClose?.Invoke(buffer.ToArray());
    }

    public void Dispose()
    {
        Close();
        buffer.Dispose();
    }
}
