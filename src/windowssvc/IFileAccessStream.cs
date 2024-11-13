namespace ArcusWinSvc;

/// <summary>
/// File store stream knows how to read and write files from the data store
/// </summary>
public interface IFileAccessStream : IDisposable
{
    Task WriteBytes(byte[] chunk);

    Task<int> ReadBytes(byte[] chunk, int chunkSize);

    void Close();
}