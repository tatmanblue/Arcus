using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// Basically a DTO for list of IRunnable which will be run by WorkQueueRunner 
/// </summary>
public class WorkQueue : IWorkQueue
{
    private ILogger<WorkQueue> logger;
    
    private LimitedConcurrencyLevelTaskScheduler taskScheduler = null;
    private List<Task> tasks = null;
    private TaskFactory factory = null;

    public CancellationToken CTS { get; set; }
    public List<Task> Tasks => tasks;
    
    public WorkQueue(ILogger<WorkQueue> logger) 
    {
        this.logger = logger;
        // seems like the threading value should be configurable
        taskScheduler = new LimitedConcurrencyLevelTaskScheduler(2);
        tasks = new List<Task>();
        factory = new TaskFactory(taskScheduler);
    }
    
    public void AddRunner(IRunnable runner)
    {
        logger.LogInformation("Adding runner");
        Task t = factory.StartNew(() => {
            try
            {
                logger.LogInformation($"Running runner {runner.FriendlyName}");
                runner.Run();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to run runner {runner.FriendlyName}");
            }
            
        }, CTS);
        tasks.Add(t);        
    }
}
