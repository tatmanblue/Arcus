namespace ArcusWinSvc;

/// <summary>
/// a single record in the index file
/// </summary>
public class IndexFileRecord
{
    public string Id {get; set;} = Guid.NewGuid().ToString();
    public string ShortName { get; set; } = string.Empty;
    public string OriginFullPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<string> Keywords { get; set; } = new();
    public FileStatuses Status { get; set; } = FileStatuses.UNKNOWN;
    public string Version { get; } = "1.0";
}

/// <summary>
/// Not sure if this will be used long term, keeping it here knowing
/// best practice separates enum out.  This must match the proto
/// </summary>
public enum FileStatuses
{
    UNKNOWN = 0,
    PENDING = 1,
    VALID = 2,
}