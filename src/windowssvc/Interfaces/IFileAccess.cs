namespace ArcusWinSvc.Interfaces;

/// <summary>
/// File store is how files are stored.  For local, this means reading and writing to disk
/// For cloud, this would mean reading and writing the cloud service like S3
/// </summary>
public interface IFileAccess
{
    IFileAccessStream AddRequest(IndexFileRecord record);

    IFileAccessStream GetFileStream(IndexFileRecord record);

    IFileAccessStream GetRequest(IndexFileRecord record);
    
    bool RemoveRequest(IndexFileRecord record);
}