namespace ArcusWinSvc.Interfaces;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-9.0
/// </summary>
public interface IRunnable
{
    void Run();    
}