namespace ArcusWinSvc;

/// <summary>
/// File store stream knows how to read and write files from the data store
/// </summary>
public interface IFileAccessStream : IDisposable
{
    Task WriteBytes(byte[] chunk);

    Task<int> ReadBytes(byte[] chunk, int chunkSize);

    /// <summary>
    /// the file already exists where the service lives and it needs to get the file
    /// into the store
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    Task LocalCopy(string source);

    void Close();
}