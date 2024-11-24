namespace ArcusWinSvc;

/// <summary>
/// Opens up the opporunity to change configuration through environment variables etc
/// TODO: read environment variables for override
/// </summary>
public class Configuration : ArcusWinSvc.Interfaces.IConfiguration
{
    private const string ARCUS_STORE_LOCATION = "ARCUS_STORE_LOC";
    private const string ARCUS_GPRC_PORT = "ARCUS_GPRC_PORT";
    private const string ARCUS_FS_STORE_KEY = "Arcus FS";
    
    
    private const int DEFAULT_GRPC_PORT = 5001;
    private const int DEFAULT_MAX_MESSAGE_SIZE = 10 * 1024;
    
    private int grpcPort = DEFAULT_GRPC_PORT;
    private int maxMessageSize = DEFAULT_MAX_MESSAGE_SIZE;
    
    public string IndexFile => GetIndexFile();

    public string IndexFilePath => GetIndexFilePath();

    public string StoreLocation => GetStoreLocation();

    public int GrpcPort
    {
        get => grpcPort;
        init
        {
            // ignoring value input since this should not be set in code anyways
            string portStr = Environment.GetEnvironmentVariable(ARCUS_GPRC_PORT) ?? "0";
            if (int.TryParse(portStr, out int portId) && portId > 1024 && portId <= 65535)
                grpcPort = portId;
        }
    }

    public int GrpcMaxMessageSize
    {
        get => maxMessageSize;
        init
        {
            // ignoring value input since this should not be set in code anyways   
            string maxSizeStr = Environment.GetEnvironmentVariable(ARCUS_GPRC_PORT) ?? "0";
            if (int.TryParse(maxSizeStr, out int maxSize) && maxSize > 1024)
                maxMessageSize = maxSize;
        }
    }

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