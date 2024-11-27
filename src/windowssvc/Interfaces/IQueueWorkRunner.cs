namespace ArcusWinSvc.Interfaces;

/// <summary>
/// For types to add IRunnable into the queue that gets executed in the order
/// added to the queue
/// </summary>
public interface IQueueWorkRunner
{
    void AddRunner(IRunnable runner);
}