namespace ArcusWinSvc;

/// <summary>
/// Taken from
/// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-9.0
/// </summary>
public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
   [ThreadStatic]
   private static bool currentThreadIsProcessingItems;
   private readonly LinkedList<Task> queuedTasks = new (); 

   // The maximum concurrency level allowed by this scheduler.
   private readonly int maxDegreeOfParallelism;

   // Indicates whether the scheduler is currently processing work items.
   private int delegatesQueuedOrRunning = 0;

   // Creates a new instance with the specified degree of parallelism.
   public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
   {
       if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
       this.maxDegreeOfParallelism = maxDegreeOfParallelism;
   }

   // Queues a task to the scheduler.
   protected sealed override void QueueTask(Task task)
   {
      // Add the task to the list of tasks to be processed.  If there aren't enough
      // delegates currently queued or running to process tasks, schedule another.
       lock (queuedTasks)
       {
           queuedTasks.AddLast(task);
           if (delegatesQueuedOrRunning < maxDegreeOfParallelism)
           {
               ++delegatesQueuedOrRunning;
               NotifyThreadPoolOfPendingWork();
           }
       }
   }

   // Inform the ThreadPool that there's work to be executed for this scheduler.
   private void NotifyThreadPoolOfPendingWork()
   {
       ThreadPool.UnsafeQueueUserWorkItem(_ =>
       {
           // Note that the current thread is now processing work items.
           // This is necessary to enable inlining of tasks into this thread.
           currentThreadIsProcessingItems = true;
           try
           {
               // Process all available items in the queue.
               while (true)
               {
                   Task item;
                   lock (queuedTasks)
                   {
                       // When there are no more items to be processed,
                       // note that we're done processing, and get out.
                       if (queuedTasks.Count == 0)
                       {
                           --delegatesQueuedOrRunning;
                           break;
                       }

                       // Get the next item from the queue
                       item = queuedTasks.First.Value;
                       queuedTasks.RemoveFirst();
                   }

                   // Execute the task we pulled out of the queue
                   base.TryExecuteTask(item);
               }
           }
           // We're done processing items on the current thread
           finally { currentThreadIsProcessingItems = false; }
       }, null);
   }

   // Attempts to execute the specified task on the current thread.
   protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
   {
       // If this thread isn't already processing a task, we don't support inlining
       if (!currentThreadIsProcessingItems) return false;

       // If the task was previously queued, remove it from the queue
       if (taskWasPreviouslyQueued)
          // Try to run the task.
          if (TryDequeue(task))
            return base.TryExecuteTask(task);
          else
             return false;
       else
          return base.TryExecuteTask(task);
   }

   // Attempt to remove a previously scheduled task from the scheduler.
   protected sealed override bool TryDequeue(Task task)
   {
       lock (queuedTasks) return queuedTasks.Remove(task);
   }

   // Gets the maximum concurrency level supported by this scheduler.
   public sealed override int MaximumConcurrencyLevel { get { return maxDegreeOfParallelism; } }

   // Gets an enumerable of the tasks currently scheduled on this scheduler.
   protected sealed override IEnumerable<Task> GetScheduledTasks()
   {
       bool lockTaken = false;
       try
       {
           Monitor.TryEnter(queuedTasks, ref lockTaken);
           if (lockTaken) return queuedTasks;
           else throw new NotSupportedException();
       }
       finally
       {
           if (lockTaken) Monitor.Exit(queuedTasks);
       }
   }
}