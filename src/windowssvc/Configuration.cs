namespace ArcusWinSvc;

/// <summary>
/// Opens up the opporunity to change configuration through environment variables etc
/// TODO: read environment variables for override
/// </summary>
public class Configuration : ArcusWinSvc.Interfaces.IConfiguration
{
    private const string ARCUS_STORE_LOCATION = "ARCUS_STORE_LOC";
    private const string ARCUS_FS_STORE_KEY = "Arcus FS";
    
    public string IndexFile => GetIndexFile();

    public string IndexFilePath => GetIndexFilePath();

    public string StoreLocation => GetStoreLocation();

    public int GrpcPort => 5001;
    
    public int GrpcMaxMessageSize => 10 * 1024;

    private string GetStoreLocation()
    {
        var path = Environment.GetEnvironmentVariable(ARCUS_STORE_LOCATION);
        if (string.IsNullOrEmpty(path))
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        var rootLocation = Path.Combine(path, @ARCUS_FS_STORE_KEY);
        return Path.Combine(rootLocation, @"Data Store");
    }

    private string GetIndexFile()
    {
        return $"{GetIndexFilePath()}\\index.txt";
    }
    
    private string GetIndexFilePath()
    {
        var path = Environment.GetEnvironmentVariable(ARCUS_STORE_LOCATION);
        if (string.IsNullOrEmpty(path))
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        return Path.Combine(path, @ARCUS_FS_STORE_KEY);
    }
}