using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// Reads and writes content to/from files sent via GRPC or created by the service to
/// the local storage area
/// </summary>
public class LocalDataAccessStream(string file, IStreamCipher cipher) : IFileAccessStream, IDisposable
{
    private Stream? fileStream = null;

    public async Task WriteBytes(byte[] chunk)
    {
        if (null == fileStream)
        {
            var raw = new FileStream(file, FileMode.Create, FileAccess.Write);
            fileStream = cipher.WrapForWrite(raw);
        }

        await fileStream.WriteAsync(chunk);
    }

    public async Task<int> ReadBytes(byte[] chunk, int chunkSize)
    {
        if (null == fileStream)
        {
            var raw = new FileStream(file, FileMode.Open, FileAccess.Read);
            fileStream = cipher.WrapForRead(raw);
        }

        int bytesRead = await fileStream.ReadAsync(chunk, 0, chunkSize);

        return bytesRead;
    }

    /// <summary>
    /// This is destructive and will delete source
    /// </summary>
    /// <param name="source"></param>
    public async Task LocalCopy(string source)
    {
        if (File.Exists(file))
            File.Delete(file);

        await using FileStream sourceStream = File.Open(source, FileMode.Open);
        var destinationRaw = File.Create(file);
        // destinationStream (the cipher wrapper) takes ownership of disposing destinationRaw
        await using (Stream destinationStream = cipher.WrapForWrite(destinationRaw))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        File.Delete(source);
    }

    public void Close()
    {
        fileStream?.Close();
        fileStream?.Dispose();
        fileStream = null;
    }

    public void Dispose()
    {
        fileStream?.Close();
    }
}
