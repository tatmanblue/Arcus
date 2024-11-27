namespace ArcusWinSvc.Interfaces;

/// <summary>
/// 
/// </summary>
public interface IRunnable
{
    string FriendlyName { get; }
    void Run();    
}