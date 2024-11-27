namespace ArcusWinSvc;

/// <summary>
/// This must match the proto enum FileStatuses
/// </summary>
public enum FileStatuses
{
    UNKNOWN = 0,
    PENDING = 1,
    VALID = 2,
    ERROR = 3,
}

/// <summary>
/// 
/// </summary>
public enum FileOperations
{
    NONE,
    DELETE,
    ERASE,
}