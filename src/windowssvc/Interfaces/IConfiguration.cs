namespace ArcusWinSvc.Interfaces;

public interface IConfiguration
{
    public string IndexFile { get; }
    
    public string IndexFilePath { get; }

    public string StoreLocation { get; }
    
    public int GrpcPort { get; }
    
    public int GrpcMaxMessageSize { get; }
}