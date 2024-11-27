using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// Allows for lonoger running tasks to be run on separate threads
/// </summary>
public class WorkQueueRunner : BackgroundService
{
    private ILogger<WorkQueueRunner> logger;
    private IWorkQueue workQueue;

    public WorkQueueRunner(ILogger<WorkQueueRunner> logger, IWorkQueue workQueue) 
    {
        this.logger = logger;
        this.workQueue = workQueue;

        logger.LogWarning("QueueWorkRunner started");
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        workQueue.CTS = stoppingToken;

        logger.LogInformation("Arcus Service up at: {time}", DateTimeOffset.Now);
        List<Task> tasks = new();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            tasks = workQueue.Tasks;
            
            if (tasks.Count == 0)
            {
                // seems like the delay should be configurable
                await Task.Delay(250, stoppingToken);
                continue;
            }
            
            Task.WaitAll(tasks.ToArray(), stoppingToken);
            tasks.Clear();
        }
        
        // we probably need to figure out how to prevent this but for now just log that
        // pending tasks exist
        if (tasks.Count > 0)
            logger.LogCritical("Arcus is stopping with pending tasks");
        
        logger.LogInformation("Arcus Service down at: {time}", DateTimeOffset.Now);
    }
}
