namespace ArcusWinSvc.Interfaces;

/// <summary>
/// 
/// </summary>
public interface IRunnable
{
    /// <summary>
    /// human-readable name describing the Runner implementation for use when logging
    /// </summary>
    string FriendlyName { get; }
    /// <summary>
    /// This is the entry point for the runner
    /// </summary>
    void Run();    
}