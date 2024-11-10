namespace ArcusWinSvc;

/// <summary>
/// a single record in the index file
/// </summary>
public class IndexFileRecord
{
    /// <summary>
    /// for uniquely identifying a record allowing short name to not necessarily
    /// be a conflict
    /// </summary>
    public string Id {get; set;} = Guid.NewGuid().ToString().Substring(0, 8);
    /// <summary>
    /// just the file name
    /// </summary>
    public string ShortName { get; set; } = string.Empty;
    /// <summary>
    /// Tracks where the file originated on the file system
    /// </summary>
    public string OriginFullPath { get; set; } = string.Empty;
    /// <summary>
    /// Reflects last update of the record.  
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    /// <summary>
    /// Keywords, for searching
    /// </summary>
    public List<string> Keywords { get; set; } = new();
    /// <summary>
    /// indicates status. example may not want deleted to physcially deleted until
    /// a specific command removes it 
    /// </summary>
    public FileStatuses Status { get; set; } = FileStatuses.UNKNOWN;
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
    ERROR = 3,
}