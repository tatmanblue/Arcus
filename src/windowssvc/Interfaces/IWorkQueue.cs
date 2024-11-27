namespace ArcusWinSvc.Interfaces;

/// <summary>
/// For types to add IRunnable into the queue that gets executed in the order
/// added to the queue
/// </summary>
public interface IWorkQueue
{
    public List<Task> Tasks { get; }
    public CancellationToken CTS { get; set; }
    void AddRunner(IRunnable runner);
}