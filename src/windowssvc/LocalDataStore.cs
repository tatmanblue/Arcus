using Arcus.GRPC;

namespace ArcusWinSvc;

/// <summary>
/// V1 of storing files.  Simple file copy
///
/// If I continue this project, use interface and blah blah blah
/// </summary>
public class LocalDataStore
{
    public void AddRequest(IndexFileRecord record)
    {
        var dir = $"{GetStoreLocation()}\\{record.Id}";
        Directory.CreateDirectory(dir);
        
        File.Copy(record.OriginFullPath, $"{dir}\\{record.Id}.file");
    }

    public string GetRequest(IndexFileRecord record, string destination)
    {
        var dir = $"{GetStoreLocation()}\\{record.Id}";
        if (false == Directory.Exists(dir))
            throw new DirectoryNotFoundException();
        
        if (false == Directory.Exists(destination))
            throw new DirectoryNotFoundException();
        
        string fullPath = $"{destination}\\{record.ShortName}";
        
        File.Copy($"{dir}\\{record.Id}.file", fullPath);
        
        return fullPath;
    }
    
    private string GetStoreLocation()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rootLocation = Path.Combine(path, @"Arcus FS");
        return Path.Combine(rootLocation, @"Data Store");
    }

    public bool RemoveRequest(IndexFileRecord record)
    {
        var dir = $"{GetStoreLocation()}\\{record.Id}";
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