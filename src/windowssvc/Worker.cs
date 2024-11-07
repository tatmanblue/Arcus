namespace ArcusWinSvc;

/// <summary>
/// for keeping windows service alive :(  TODO eval
/// TODO move grpc hosting inside this class if we keep it
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Arcus Service up at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        logger.LogInformation("Arcus Service down at: {time}", DateTimeOffset.Now);
    }
}
