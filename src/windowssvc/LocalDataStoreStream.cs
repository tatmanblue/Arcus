namespace ArcusWinSvc;

/// <summary>
/// 
/// </summary>
public class LocalDataStoreStream(string file) : IDisposable
{
    private FileStream fileStream = null;

    public async Task AddBytes(byte[] chunk)
    {
        if (null == fileStream)
            fileStream = new FileStream(file, FileMode.Create, FileAccess.Write);
        
        await fileStream.WriteAsync(chunk);
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