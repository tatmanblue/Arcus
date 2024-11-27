using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc;

/// <summary>
/// for keeping windows service alive :(  TODO eval
/// TODO move grpc hosting inside this class if we keep it
/// </summary>
public class Worker(ILogger<Worker> logger) : BackgroundService, IQueueWorkRunner
{
    public void AddRunner(IRunnable runner)
    {
        logger.LogInformation("Adding runner");
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
/*
    private void Test()
    {
       List<Task> tasks = new List<Task>();
       // Create a TaskFactory and pass it our custom scheduler.
       TaskFactory factory = new TaskFactory(lcts);
       CancellationTokenSource cts = new CancellationTokenSource();

       // Use our factory to run a set of tasks.
       Object lockObj = new Object();
       int outputItem = 0;

       for (int tCtr = 0; tCtr <= 4; tCtr++) {
          int iteration = tCtr;
          Task t = factory.StartNew(() => {
                                       for (int i = 0; i < 1000; i++) {
                                          lock (lockObj) {
                                             Console.Write("{0} in task t-{1} on thread {2}   ",
                                                           i, iteration, Thread.CurrentThread.ManagedThreadId);
                                             outputItem++;
                                             if (outputItem % 3 == 0)
                                                Console.WriteLine();
                                          }
                                       }
                                    }, cts.Token);
          tasks.Add(t);
      }
      // Use it to run a second set of tasks.
      for (int tCtr = 0; tCtr <= 4; tCtr++) {
         int iteration = tCtr;
         Task t1 = factory.StartNew(() => {
                                       for (int outer = 0; outer <= 10; outer++) {
                                          for (int i = 0x21; i <= 0x7E; i++) {
                                             lock (lockObj) {
                                                Console.Write("'{0}' in task t1-{1} on thread {2}   ",
                                                              Convert.ToChar(i), iteration, Thread.CurrentThread.ManagedThreadId);
                                                outputItem++;
                                                if (outputItem % 3 == 0)
                                                   Console.WriteLine();
                                             }
                                          }
                                       }
                                    }, cts.Token);
         tasks.Add(t1);
      }

      // Wait for the tasks to complete before displaying a completion message.
      Task.WaitAll(tasks.ToArray());
      cts.Dispose();
      Console.WriteLine("\n\nSuccessful completion.");
    }
*/    
}
