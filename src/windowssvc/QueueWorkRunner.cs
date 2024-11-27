using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// Allows for lonoger running tasks to be run on separate threads
/// </summary>
public class QueueWorkRunner(ILogger<QueueWorkRunner> logger) : BackgroundService, IQueueWorkRunner
{
    private LimitedConcurrencyLevelTaskScheduler taskScheduler = null;
    private List<Task> tasks = null;
    private TaskFactory factory = null;
    private CancellationToken cts;
    
    public void AddRunner(IRunnable runner)
    {
        logger.LogInformation("Adding runner");
        Task t = factory.StartNew(runner.Run, cts);
        tasks.Add(t);        
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        taskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);
        tasks = new List<Task>();
        factory = new TaskFactory(taskScheduler);
        cts = stoppingToken;
        
        logger.LogInformation("Arcus Service up at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (tasks.Count == 0)
            {
                // do we really want to wait a whole second before checking again
                await Task.Delay(1000, stoppingToken);
                continue;
            }
            
            tasks.ForEach(t => logger.LogInformation("Running task: {task}", t.GetType().Name));
            Task.WaitAll(tasks.ToArray(), stoppingToken);
        }
        
        // we probably need to figure out how to prevent this but for now just log that
        // pending tasks exist
        if (tasks.Count > 0)
            logger.LogCritical("Arcus is stopping with pending tasks");
        
        logger.LogInformation("Arcus Service down at: {time}", DateTimeOffset.Now);
    }
}
