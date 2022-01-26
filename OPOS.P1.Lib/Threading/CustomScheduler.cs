using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OPOS.P1.Lib.Threading.CustomCancellationToken;

namespace OPOS.P1.Lib.Threading
{
    public class CustomScheduler : IDisposable
    {
        private bool disposed;
        private ThreadLocal<CustomTask> currentTask = new();

        private List<Thread> threads;

        public CustomSchedulerSettings Settings { get; init; }

        private readonly object _taskQueueLock = new object();
        private PriorityQueue<CustomTask, CustomTask> taskQueue =
            new(new CustomTaskComparer());

        // TODO add lock or use concurrent variant
        private readonly Dictionary<CustomTask, CustomCancellationTokenSource> taskCancellationTokenSources = new();

        // TODO add lock or use concurrent variant
        private Dictionary<CustomTask, Thread> threadTasks = new();
        private readonly object _threadTasksLock = new();

        public class CustomTaskComparer : IComparer<CustomTask>
        {
            public int Compare(CustomTask x, CustomTask y)
            {
                if (x?.Settings is null && y?.Settings is null)
                    return 0;

                if (x?.Settings is null)
                    return 1;

                if (y?.Settings is null)
                    return -1;

                int xPriority = x.Settings.Priority;
                int yPriority = y.Settings.Priority;

                return xPriority > yPriority ? -1 : 1;
            }
        }

        public CustomScheduler(CustomSchedulerSettings customSchedulerSettings)
        {
            Settings = customSchedulerSettings ?? throw new ArgumentNullException(nameof(customSchedulerSettings));

            if (Settings.MaxCores == 0)
                Settings = Settings with { MaxCores = Environment.ProcessorCount };
            else if (Settings.MaxCores < 0) throw new ArgumentOutOfRangeException(nameof(Settings.MaxCores));

            threads = new List<Thread>(Settings.MaxCores);
            for (int i = 0; i < Settings.MaxCores; i++)
            {
                Thread thread = new(ThreadLoop) { Name = $"{nameof(CustomScheduler)}:{i}" };
                threads.Add(thread);
                thread.Start();
            }
        }

        public TCustomTask PrepareTask<TCustomTask>(TCustomTask customTask)
            where TCustomTask : CustomTask
        {
            customTask.Scheduler = this;
            Enqueue(customTask);
            return customTask;
        }

        public void Enqueue(CustomTask customTask)
        {
            if (customTask.Status != TaskStatus.Created)
                throw new ArgumentException($"Task must be in state {TaskStatus.Created}.", nameof(customTask));

            if (customTask.Settings.Deadline <= DateTime.Now)
                throw new ArgumentException($"The task deadline cannot be the present or a past moment.", nameof(customTask.Settings.Deadline));

            lock (_taskQueueLock)
            {
                taskQueue.Enqueue(customTask, customTask);

                taskCancellationTokenSources.Remove(customTask);

                var deadlineInterval = customTask.Settings.Deadline.Subtract(DateTime.Now);
                var runDurationInterval = customTask.Settings.MaxRunDuration;
                var cancelInterval = deadlineInterval < runDurationInterval ? deadlineInterval : runDurationInterval;
                var source = new CustomCancellationTokenSource
                {
                    CancellationTokenSource = new CancellationTokenSource(cancelInterval),
                    PauseTokenSource = new CancellationTokenSource()
                };

                lock (taskCancellationTokenSources)
                    taskCancellationTokenSources.Add(customTask, source);

                if (customTask.WantsToRun)
                    customTask.Start();
            }
        }

        internal List<Thread> Threads => threads;

        private void ThreadLoop()
        {
            while (!disposed)
            {
                bool shouldTakeNextTask = false;
                CustomTask nextTask = null;
                lock (_taskQueueLock)
                {
                    if (taskQueue.Count > 0)
                        shouldTakeNextTask = true;

                    taskQueue.TryPeek(out var peekTask, out _);

                    if (shouldTakeNextTask
                        && taskQueue.Count > 0
                        && peekTask?.Status == TaskStatus.WaitingForActivation
                        && (peekTask?.WantsToRun ?? false))
                        nextTask = taskQueue.Dequeue();
                }

                if (nextTask is not null)
                {
                    taskCancellationTokenSources.TryGetValue(nextTask, out var tokenSource);
                    var cancellationToken = tokenSource.CancellationTokenSource.Token;
                    var pauseToken = tokenSource.PauseTokenSource.Token;
                    var token = new CustomCancellationToken(cancellationToken, pauseToken);



                    currentTask.Value = nextTask;

                    nextTask.Status = TaskStatus.Running;
                    try
                    {
                        nextTask.LastStartedRunning = DateTime.Now;
                        try
                        {
                            nextTask.Run(nextTask.State, token);
                        }
                        // TODO handle interrupt
                        catch (ThreadInterruptedException)
                        {
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        var lastRunDuration = nextTask.TotalRunDuration.Add(DateTime.Now - nextTask.LastStartedRunning);
                        nextTask.TotalRunDuration = lastRunDuration;

                        bool reachedDeadline = nextTask.Settings.Deadline <= DateTime.Now;
                        bool reachedMaxRunDuration = nextTask.TotalRunDuration >= nextTask.Settings.MaxRunDuration;
                        if (reachedDeadline
                            || reachedMaxRunDuration
                            || nextTask.Status == TaskStatus.Canceled)
                        {
                            nextTask.MetDeadline = reachedDeadline;
                            nextTask.Status = TaskStatus.Canceled;
                            taskCancellationTokenSources.Remove(nextTask);
                            currentTask.Value = null;
                            threadTasks.Remove(nextTask);
                            continue;
                        }

                        nextTask.WantsToRun = false;
                        taskCancellationTokenSources.Remove(nextTask);
                        currentTask.Value = null;
                        threadTasks.Remove(nextTask);
                        if (ex is OperationPausedException)
                        {
                            nextTask.Status = TaskStatus.Created;
                            Enqueue(nextTask);
                            continue;
                        }

                        nextTask.Status = TaskStatus.Canceled;
                        taskCancellationTokenSources.Remove(nextTask);
                        currentTask.Value = null;
                        threadTasks.Remove(nextTask);
                        continue;
                    }

                    nextTask.Status = TaskStatus.RanToCompletion;
                }
                else
                {
                    try
                    {
                        Thread.Sleep(Timeout.Infinite);
                    }
                    catch (ThreadInterruptedException) { }
                    continue;
                }
            }
        }

        public CustomTask Dequeue()
        {
            lock (_taskQueueLock)
            {
                if (taskQueue.Count == 0)
                    return null;

                var dequeued = taskQueue.Dequeue();
                if (dequeued.Status != TaskStatus.RanToCompletion)
                {
                    var token = taskCancellationTokenSources.GetValueOrDefault(dequeued);
                    token.CancellationTokenSource.Cancel();
                    dequeued.Status = TaskStatus.Canceled;
                }

                lock (taskCancellationTokenSources)
                    taskCancellationTokenSources.Remove(dequeued);

                return dequeued;
            }
        }

        internal bool Any()
        {
            lock (_taskQueueLock)
            {
                return taskQueue.Count != 0;
            }
        }

        internal void PauseTask(CustomTask customTask)
        {
            customTask.WantsToRun = false;
            taskCancellationTokenSources.TryGetValue(customTask, out var cancellationTokenSource);

            cancellationTokenSource.PauseTokenSource.Cancel();
        }

        internal void UpdateTaskStatus(CustomTask customTask, TaskStatus taskStatus)
        {
            var currentStatus = customTask.Status;
            if (currentStatus == taskStatus)
                return;

            if (taskStatus != TaskStatus.Created
                && taskStatus != TaskStatus.WaitingForActivation
                && taskStatus != TaskStatus.Canceled)
                throw new InvalidOperationException($"Only supported operations are task pausing and resuming.");

            if (taskStatus == TaskStatus.Canceled)
            {
                customTask.WantsToRun = false;
                taskCancellationTokenSources.TryGetValue(customTask, out var cancellationTokenSource);
                cancellationTokenSource.CancellationTokenSource.Cancel();
                return;
            }

            lock (_taskQueueLock)
            {
                var newTaskQueue = new PriorityQueue<CustomTask, CustomTask>();
                newTaskQueue.EnqueueRange(taskQueue.UnorderedItems.Where(t => !t.Element.Equals(customTask)));

                taskQueue = newTaskQueue;

                customTask.Status = taskStatus;
                taskQueue.Enqueue(customTask, customTask);

                lock (_threadTasksLock)
                {
                    threadTasks.TryGetValue(customTask, out var thread);
                    if (thread is not null)
                        thread.Interrupt();

                    if (threadTasks.Count == threads.Count)
                        return;

                    var freeThread = threads.Except(threadTasks.Values).FirstOrDefault();
                    if (freeThread is null)
                        return;

                    freeThread.Interrupt();
                }
            }
        }

        public IEnumerable<CustomTask> GetScheduledTasks()
        {
            return taskQueue.UnorderedItems
                .Select(c => c.Element)
                .ToImmutableSortedSet(new CustomTaskPriorityComparer())
                .AsEnumerable();
        }

        protected void QueueTask(Task task)
        {
            if (task is not CustomTask customTask)
                throw new NotImplementedException();
            throw new NotImplementedException();
        }

        protected bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            disposed = true;

            // TODO implement
            // TODO cancel all running tasks
            throw new NotImplementedException();
        }
    }
}
