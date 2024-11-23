namespace ArcusWinSvc;

/// <summary>
/// Opens up the opporunity to change configuration through environment variables etc
/// </summary>
public class Configuration
{
    public string IndexFile
    {
        get
        {
            return GetIndexFile();
        }
    }
    
    public string IndexFilePath 
    {
        get
        {
            return GetIndexFilePath();
        }
    }

    public string StoreLocation
    {
        get
        {
            return GetStoreLocation();
        }
    }

    private string GetStoreLocation()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rootLocation = Path.Combine(path, @"Arcus FS");
        return Path.Combine(rootLocation, @"Data Store");
    }

    private string GetIndexFile()
    {
        return $"{GetIndexFilePath()}\\index.txt";
    }
    
    private string GetIndexFilePath()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(path, @"Arcus FS");
    }
}