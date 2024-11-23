using Arcus.GRPC;
using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// V1 of storing files.  Simple file copy
///
/// If I continue this project, use interface and blah blah blah
/// </summary>
public class LocalDataAccess(Configuration config) : IFileAccess
{
    public IFileAccessStream AddRequest(IndexFileRecord record)
    {
        var dir = $"{config.StoreLocation}\\{record.Id}";
        // hard assumption it doesn't already exist, and it is a problem if it is
        // but for this POC, not important enough
        Directory.CreateDirectory(dir);

        return GetFileStream(record);
    }

    public IFileAccessStream GetFileStream(IndexFileRecord record)
    {
        var dir = $"{config.StoreLocation}\\{record.Id}";
        var file = $"{dir}\\{record.Id}.file";

        LocalDataAccessStream ldss = new LocalDataAccessStream(file);
        return ldss;
    }

    public IFileAccessStream GetRequest(IndexFileRecord record)
    {
        var dir = $"{config.StoreLocation}\\{record.Id}";
        if (false == Directory.Exists(dir))
            throw new DirectoryNotFoundException();
        
        return GetFileStream(record);
    }

    public bool RemoveRequest(IndexFileRecord record)
    {
        var dir = $"{config.StoreLocation}\\{record.Id}";
        if (false == Directory.Exists(dir))
            throw new DirectoryNotFoundException();
        
        string file = $"{dir}\\{record.Id}.file";
        
        if (false == File.Exists(file))
            throw new FileNotFoundException();
        
        File.Delete(file);
        Directory.Delete(dir);
        
        return true;
    }
}