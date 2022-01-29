using System;
using System.Collections.Concurrent;
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
        private const int lockTimeoutMillis = 1;
        private bool disposed;
        private readonly ThreadLocal<CustomTask> currentTask = new();

        private readonly List<Thread> threads;

        public CustomSchedulerSettings Settings { get; init; }

        private readonly object _taskQueueLock = new();
        internal PriorityQueue<CustomTask, CustomTask> taskQueue =
            new(new CustomTaskComparer());

        private readonly ConcurrentDictionary<CustomTask, CustomCancellationTokenSource> taskCancellationTokenSources = new();

        internal readonly ConcurrentDictionary<CustomTask, Thread> threadTasks = new();
        internal readonly ConcurrentDictionary<Thread, CustomThreadState> threadStates = new();
        internal readonly ConcurrentDictionary<string, CustomResource> customResources = new();
        private readonly object _customResourcesLock = new();

        internal readonly ConcurrentDictionary<string, CancellationTokenSource> customResourceTokenSources = new();

        internal readonly ConcurrentDictionary<CustomResource, PriorityQueue<CustomTask, int>> resourceTasks = new();

        public record CustomThreadState
        {
            public bool WakingUp { get; init; }
        }

        public class CustomTaskComparer : IComparer<CustomTask>
        {
            public int Compare(CustomTask x, CustomTask y)
            {
                return CompareTasks(x, y);
            }

            public static int CompareTasks(CustomTask x, CustomTask y)
            {
                if (x?.Settings is null && y?.Settings is null)
                    return 0;

                if (x?.Settings is null)
                    return 1;

                if (y?.Settings is null)
                    return -1;

                if (x.WantsToRun && !y.WantsToRun)
                    return -1;
                else if (!x.WantsToRun && y.WantsToRun)
                    return 1;

                int statusComparison = CustomTaskStatusComparer.CompareTaskStatus((TaskStatus)(x?.Status), (TaskStatus)(y?.Status));
                if (statusComparison != 0)
                    return statusComparison;

                int xPriority = x.Settings.Priority;
                int yPriority = y.Settings.Priority;

                return xPriority > yPriority ? -1 : 1;

                // TODO also compare by number of resources held?
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
                threadStates.TryAdd(thread, new());
            }
        }

        public TCustomTask PrepareTask<TCustomTask>(TCustomTask customTask)
            where TCustomTask : CustomTask
        {
            customTask.Scheduler = this;

            if (customTask.CustomResources is not null)
                foreach (var customResource in customTask.CustomResources)
                    customResources.TryAdd(customResource.Uri, customResource);

            Enqueue(customTask);
            return customTask;
        }

        public CustomResource FindCustomResource(string uri)
        {
            var existingResource = customResources.GetValueOrDefault(uri);
            return existingResource;
        }

        internal void LockResourceAndAct(CustomResource customResource, Action action)
        {
            LockResourceAndAct(customResource.Uri, action);
        }

        internal void LockResourcesAndAct(ImmutableList<CustomResource> requestedResources, Action action)
        {
            if (!requestedResources.Any())
                throw new ArgumentException($"The {nameof(requestedResources)} cannot be empty.", nameof(requestedResources));

            var existingResources = requestedResources
                .Select(customResource =>
                    {
                        customResources.TryGetValue(customResource.Uri, out var existingCustomResource);
                        return existingCustomResource;
                    })
                .ToList();

            if (existingResources.Count != requestedResources.Count)
                throw new ArgumentException($"One of the {nameof(requestedResources)} was not obtained in the current task context {currentTask.Value}", nameof(requestedResources));

            bool acquiredAll = false;
            bool[] acquiredArr = new bool[existingResources.Count];
            bool acquiredResourceLock = false;

            try
            {
                while (!acquiredAll)
                {
                    try
                    {
                        acquiredResourceLock = Monitor.TryEnter(customResources, TimeSpan.FromMilliseconds(lockTimeoutMillis));
                        if (!acquiredResourceLock)
                            continue;

                        for (int i = 0; i < existingResources.Count; i++)
                        {
                            var resource = existingResources[i];
                            bool acquired = false;
                            while (!acquired)
                            {
                                acquired = Monitor.TryEnter(resource, TimeSpan.FromMilliseconds(lockTimeoutMillis));
                                acquiredArr[i] = acquired;
                            }
                        }
                    }
                    finally
                    {
                        if (acquiredResourceLock)
                            Monitor.Exit(customResources);
                    }

                    acquiredAll = true;
                }

                action();
            }
            finally
            {
                for (int i = 0; i < existingResources.Count; i++)
                {
                    if (acquiredArr[i])
                        Monitor.Exit(existingResources[i]);
                }
            }
        }

        internal void LockResourceAndAct(string uri, Action action)
        {
            customResources.TryGetValue(uri, out var customResource);
            if (!(currentTask.Value?.CustomResources?.Contains(customResource) ?? false))
                throw new ArgumentException($"Resource {uri} is not owned by current task context '{currentTask.Value}'.", nameof(uri));

            var newQueue = new PriorityQueue<CustomTask, int>(new IntegerDescendingComparer());
            PriorityQueue<CustomTask, int> priorityQueue = null;

            var task = currentTask.Value;
            var taskPriority = task.Settings.Priority;
            var taskPriorityTuple = (task, task.Settings.Priority);

            // TODO do the same thing if paused or think of alternative approach or leave as is?
            bool acquired = false;
            bool acquiredResourceLock = false;
            while (!acquired)
            {
                try
                {
                    try
                    {
                        priorityQueue = resourceTasks.GetOrAdd(customResource, newQueue);

                        lock (priorityQueue)
                        {
                            if (!priorityQueue.UnorderedItems.Contains(taskPriorityTuple))
                                priorityQueue.Enqueue(task, task.Settings.Priority);
                        }

                        //lock (priorityQueue)
                        //{
                        //    if (!priorityQueue.Peek().Equals(task))
                        //        continue;
                        //}

                        acquiredResourceLock = Monitor.TryEnter(customResources, TimeSpan.FromMilliseconds(lockTimeoutMillis));

                        if (!acquiredResourceLock)
                            continue;

                        acquired = Monitor.TryEnter(customResource, TimeSpan.FromMilliseconds(lockTimeoutMillis));

                        if (!acquired)
                            continue;

                        lock (priorityQueue)
                        {
                            if (!priorityQueue.Peek().Equals(task))
                                continue;
                            priorityQueue.Dequeue();

                            // TODO enqueue it back in if PCP needed 
                            //priorityQueue.Enqueue()
                            // TODO boost priority to ceiling, as proposed by PCP, to the max int value
                        }
                    }
                    finally
                    {
                        if (acquiredResourceLock)
                            Monitor.Exit(customResources);
                    }

                    action();
                }
                finally
                {
                    // TODO remove from queue if PCP needed
                    //lock (customResources)
                    //{
                    //    var currentQueue = priorityQueue.
                    //}

                    if (acquired)
                        Monitor.Exit(customResource);
                }
            }
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

                taskCancellationTokenSources.Remove(customTask, out _);

                var deadlineInterval = customTask.Settings.Deadline.Subtract(DateTime.Now);
                // TODO use the remaining time, rather than the max run duration
                var runDurationInterval = customTask.Settings.MaxRunDuration - customTask.TotalRunDuration;
                var cancelInterval = deadlineInterval < runDurationInterval ? deadlineInterval : runDurationInterval;
                var source = new CustomCancellationTokenSource
                {
                    CancellationTokenSource = new CancellationTokenSource(cancelInterval),
                    PauseTokenSource = new CancellationTokenSource()
                };

                taskCancellationTokenSources.TryAdd(customTask, source);

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
                try
                {
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

                    if (nextTask is null)
                    {
                        try
                        {
                            threadStates.AddOrUpdate(
                                Thread.CurrentThread,
                                new CustomThreadState { WakingUp = false },
                                (t, ts) => new CustomThreadState { WakingUp = false });
                            Thread.Sleep(Timeout.Infinite);
                        }
                        catch (ThreadInterruptedException) { }
                        continue;
                    }

                    threadStates.AddOrUpdate(
                                Thread.CurrentThread,
                                new CustomThreadState { WakingUp = false },
                                (t, ts) => new CustomThreadState { WakingUp = false });
                    taskCancellationTokenSources.TryGetValue(nextTask, out var tokenSource);
                    var cancellationToken = tokenSource.CancellationTokenSource.Token;
                    var pauseToken = tokenSource.PauseTokenSource.Token;
                    var token = new CustomCancellationToken(cancellationToken, pauseToken);

                    threadTasks.TryAdd(nextTask, Thread.CurrentThread);
                    currentTask.Value = nextTask;

                    nextTask.Status = TaskStatus.Running;
                    bool ranNoExceptions = false;
                    bool preempted = false;
                    try
                    {
                        nextTask.LastStartedRunning = DateTime.Now;
                        try
                        {
                            nextTask.Run(nextTask.State, token);
                            ranNoExceptions = true;
                        }
                        // TODO handle interrupt
                        catch (ThreadInterruptedException) { preempted = true; }
                    }
                    catch (OperationCanceledException ex)
                    {
                        var newTotalRunDuration = nextTask.TotalRunDuration.Add(DateTime.Now - nextTask.LastStartedRunning);
                        nextTask.TotalRunDuration = newTotalRunDuration;

                        bool reachedDeadline = nextTask.Settings.Deadline <= DateTime.Now;
                        bool reachedMaxRunDuration = nextTask.TotalRunDuration >= nextTask.Settings.MaxRunDuration;
                        if (reachedDeadline
                            || reachedMaxRunDuration
                            || nextTask.Status == TaskStatus.Canceled)
                        {
                            nextTask.MetDeadline = reachedDeadline;
                            nextTask.Status = TaskStatus.Canceled;
                            // TODO replace with method that also disposes both the cancellationton sources
                            taskCancellationTokenSources.Remove(nextTask, out _);
                            currentTask.Value = null;
                            threadTasks.Remove(nextTask, out _);
                            continue;
                        }

                        nextTask.WantsToRun = false;
                        taskCancellationTokenSources.Remove(nextTask, out _);
                        currentTask.Value = null;
                        threadTasks.Remove(nextTask, out _);
                        if (ex is OperationPausedException)
                        {
                            nextTask.Status = TaskStatus.Created;
                            Enqueue(nextTask);
                            continue;
                        }

                        // TODO remove duplicated
                        nextTask.Status = TaskStatus.Canceled;
                        taskCancellationTokenSources.Remove(nextTask, out _);
                        currentTask.Value = null;
                        threadTasks.Remove(nextTask, out _);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        nextTask.Exception = new AggregateException(ex);
                    }

                    if (preempted)
                    {
                        nextTask.Status = TaskStatus.WaitingForActivation;
                        lock (_taskQueueLock)
                            taskQueue.Enqueue(nextTask, nextTask);

                        continue;
                    }

                    if (!ranNoExceptions)
                    {
                        nextTask.Status = TaskStatus.Faulted;
                        currentTask.Value = null;
                        continue;
                    }

                    nextTask.Status = TaskStatus.RanToCompletion;
                    currentTask.Value = null;
                }
                // TODO handle interrupt
                catch (ThreadInterruptedException) { }
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
                    taskCancellationTokenSources.TryGetValue(dequeued, out var token);

                    token.CancellationTokenSource.Cancel();
                    dequeued.Status = TaskStatus.Canceled;
                }

                taskCancellationTokenSources.Remove(dequeued, out _);

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

                threadTasks.TryGetValue(customTask, out var thread);
                if (thread is not null)
                    thread.Interrupt();

                // TODO or if the priority of the currently enqueued task is not higher than that of the other currently running ones

                // If no prevention
                //if (threadTasks.Count == threads.Count)
                //    return;

                // TODO find thread thats running the least prioritized task currently and choose it for the interrupt
                var higherOrEqualPriorityThreads = threadTasks
                    .Where(p => customTask.CompareTo(p.Key) >= 0 && p.Value is not null && p.Key is not null)
                    .Select(p => p.Value);

                var freeThread = threadStates
                    .Where(t => !t.Value.WakingUp)
                    .Select(t => t.Key)
                    .Except(higherOrEqualPriorityThreads)
                    .OrderBy(thread => thread,
                        new ThreadTaskPriorityComparer(this))
                    .FirstOrDefault();
                if (freeThread is null)
                    return;

                var threadTask = threadTasks.FirstOrDefault(pair => pair.Value == freeThread);

                threadStates.AddOrUpdate(freeThread,
                    new CustomThreadState { WakingUp = true },
                    (t, ts) => new CustomThreadState { WakingUp = true });
                freeThread.Interrupt();
            }
        }

        private class ThreadTaskPriorityComparer : IComparer<Thread>
        {
            private readonly CustomScheduler scheduler;

            public ThreadTaskPriorityComparer(CustomScheduler scheduler)
            {
                this.scheduler = scheduler;
            }

            public int Compare(Thread x, Thread y)
            {
                var pairX = scheduler.threadTasks.FirstOrDefault(pair => pair.Value == x);
                var pairY = scheduler.threadTasks.FirstOrDefault(pair => pair.Value == y);

                return -CustomTaskComparer.CompareTasks(pairX.Key, pairY.Key);
            }
        }

        public IEnumerable<CustomTask> GetScheduledTasks()
        {
            return taskQueue.UnorderedItems
                .Select(c => c.Element)
                .ToImmutableSortedSet(new CustomTaskComparer())
                .AsEnumerable();
        }

        //protected void QueueTask(Task task)
        //{
        //    if (task is not CustomTask customTask)
        //        throw new NotImplementedException();
        //    throw new NotImplementedException();
        //}

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
