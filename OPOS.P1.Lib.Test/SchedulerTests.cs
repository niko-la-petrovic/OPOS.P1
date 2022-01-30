using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static OPOS.P1.Lib.Threading.CustomScheduler;
using static OPOS.P1.Lib.Threading.CustomTaskStatusComparer;

namespace OPOS.P1.Lib.Test
{
    public class MockCustomTask : CustomTask
    {
        public MockCustomTask(
            Action<ICustomTaskState, CustomCancellationToken> runAction = default,
            ICustomTaskState state = default,
            CustomTaskSettings customTaskSettings = default,
            ImmutableList<CustomResource> customResources = default)
            : base(runAction, state, customTaskSettings, customResources)
        {
        }

        public override CustomTask Deserialize(string json)
        {
            throw new NotImplementedException();
        }
    }

    public class SchedulerTests
    {
        private readonly ITestOutputHelper output;

        public SchedulerTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void GetSettings()
        {
            GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

            Assert.Equal(maxCores, scheduler.Settings.MaxCores);
            Assert.Equal(maxConcurrentTasks, scheduler.Settings.MaxConcurrentTasks);
        }

        [Fact]
        public void NullSettings()
        {
            Assert.Throws<ArgumentNullException>(() => { new CustomScheduler(null); });
        }

        [Fact]
        public void OrderTaskByHigherPriority()
        {
            GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

            CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

            int priority = basePriority;
            var taskCount = 5;

            List<MockCustomTask> tasks = new List<MockCustomTask>() { };

            Enumerable.Range(0, taskCount).ToList()
                .ForEach(i =>
                    tasks.Add(new MockCustomTask(
                        (state, token) => { },
                        customTaskSettings: customTaskSettings with
                        {
                            Priority = priority++,
                        })));

            tasks.ForEach(t => scheduler.Enqueue(t));
            var reversedTasks = tasks.Reverse<MockCustomTask>();

            foreach (var task in reversedTasks)
            {
                var dequeued = scheduler.Dequeue();
                Assert.Equal(dequeued, task);
            }
        }

        [Fact]
        public void OrderTaskByHigherPriorityWithRoundRobin()
        {
            GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

            CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

            basePriority = 5;
            int priority = basePriority;

            List<MockCustomTask> tasks = new List<MockCustomTask>() { };

            Enumerable.Range(0, 2).ToList()
                .ForEach(i =>
                    tasks.Add(new MockCustomTask(
                        (state, token) => { },
                        customTaskSettings: customTaskSettings with
                        {
                            Priority = basePriority
                        })));
            Enumerable.Range(0, 3).ToList()
                .ForEach(i =>
                    tasks.Add(new MockCustomTask(
                        (state, token) => { },
                        customTaskSettings: customTaskSettings with
                        {
                            Priority = --priority
                        })));

            tasks.ForEach(t => scheduler.Enqueue(t));

            var task1 = scheduler.Dequeue();
            task1.Status = TaskStatus.Created;

            Assert.Equal(basePriority, task1.Settings.Priority);
            var remainingTasks = scheduler.GetScheduledTasks();
            foreach (var item in remainingTasks)
            {
                Assert.True(task1.Settings.Priority >= item.Settings.Priority);
            }

            scheduler.Enqueue(task1);
            var task2 = scheduler.Dequeue();
            task2.Status = TaskStatus.Created;

            Assert.False(task1.Equals(task2));

            scheduler.Enqueue(task2);
            var task1_1 = scheduler.Dequeue();
            task1_1.Status = TaskStatus.Created;

            Assert.True(task1.Equals(task1_1));
        }

        [Fact]
        public void CannotQueueCompletedTask()
        {
            GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

            CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

            var task = new MockCustomTask(customTaskSettings: customTaskSettings);
            scheduler.Enqueue(task);
            scheduler.Dequeue();

            Assert.Throws<ArgumentException>(() => scheduler.Enqueue(task));
        }

        [Fact]
        public void CanStartThreads()
        {
            GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

            var threadStates = scheduler.Threads
                .Select(t => t.ThreadState);

            var boolStates = threadStates.Select(t => t != ThreadState.Unstarted
                && t != ThreadState.Stopped);

            output.WriteLine(string.Join(",", threadStates));

            Assert.True(boolStates.All(s => s));
        }

        public class TaskOperationsTests
        {
            private const string testFile = "test.txt";
            private const string testFile1 = "test1.txt";
            private const string testFile2 = "test2.txt";
            private const string testFile3 = "test3.txt";

            private readonly ITestOutputHelper output;

            public TaskOperationsTests(ITestOutputHelper output)
            {
                this.output = output;
            }

            [Fact]
            public void CanRunTask()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                bool startedRunning = false;
                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            startedRunning = true;
                            output.WriteLine("Started run.");
                            Thread.Sleep(2000);
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(1000);

                Assert.True(startedRunning);
                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine(task.Status.ToString());
            }

            [Fact]
            public void CanRunTask_QueueEmptyAfter()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                bool startedRunning = false;
                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            startedRunning = true;
                            output.WriteLine("Started run.");
                            Thread.Sleep(2000);
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(1000);

                Assert.True(startedRunning);
                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine(task.Status.ToString());

                Assert.False(scheduler.Any());
            }

            [Fact]
            public void CanFinishTask()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            Thread.Sleep(800);
                            output.WriteLine("Finished run.");
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(2000);

                output.WriteLine(task.Status.ToString());
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }

            [Fact]
            public void CanTimeoutTaskByRuntime()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                int actionRunCount = 0;
                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            actionRunCount++;
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                Thread.Sleep(200);
                            }
                            output.WriteLine("Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(2500);

                output.WriteLine($"{nameof(actionRunCount)}: {actionRunCount}");
                output.WriteLine(task.Status.ToString());
                Assert.Equal(TaskStatus.Canceled, task.Status);
                Assert.False(task.MetDeadline);
            }

            [Fact]
            public void CanTimeoutTaskByDeadline()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMilliseconds(500),
                };

                int actionRunCount = 0;
                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            actionRunCount++;
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                Thread.Sleep(200);
                            }
                            output.WriteLine("Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(2500);

                output.WriteLine($"{nameof(actionRunCount)}: {actionRunCount}");
                output.WriteLine(task.Status.ToString());
                Assert.Equal(TaskStatus.Canceled, task.Status);
                Assert.True(task.MetDeadline);
            }

            [Fact]
            public void CanStopTask()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromMinutes(1),
                };

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    Thread.Sleep(200);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            output.WriteLine("Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(2500);
                task.Stop();
                Thread.Sleep(500);
                output.WriteLine(task.Status.ToString());
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }

            [Fact]
            public void CanPauseTask()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                int actionRunCount = 0;
                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            actionRunCount++;
                            var sumState = s as SumState;
                            output.WriteLine("Started run.");
                            while (!t.CancellationToken.IsCancellationRequested
                                && !t.PauseToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                t.ThrowIfPauseRequested();
                                output.WriteLine($"Iteration {sumState.Value++}");
                                Thread.Sleep(200);
                            }
                            output.WriteLine("Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                            t.ThrowIfPauseRequested();
                        },
                        state: new SumState
                        {
                            Value = 0,
                        },
                        customTaskSettings: customTaskSettings));

                var serialized = task.Serialize<SumState>();
                output.WriteLine(serialized);
                Assert.Equal(JsonSerializer.Serialize(new SumState { Value = 0 }), serialized);

                task.Start();
                Thread.Sleep(50);

                Assert.Equal(1, actionRunCount);
                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine(task.Serialize<SumState>());

                task.Pause();
                Thread.Sleep(500);

                var state = task.State as SumState;
                var serializedState = task.Serialize<SumState>();
                output.WriteLine(serializedState);
                Assert.Equal(1, actionRunCount);
                Assert.Equal(TaskStatus.Created, task.Status);
                Assert.Equal(1, state.Value);

                task.Continue();
                Thread.Sleep(250);

                output.WriteLine(task.Serialize<SumState>());
                Assert.Equal(3, state.Value);
                Assert.Equal(TaskStatus.Running, task.Status);
            }

            [Fact]
            public void CanRealTimeSchedule()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromMinutes(1),
                };

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("[1] Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"[1] Iteration {i++}");
                                try
                                {
                                    Thread.Sleep(1000);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            output.WriteLine("[1] Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                Assert.Equal(1, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");

                task.Start();

                // Because thread interrupts don't immediately happen and the task action isn't immediately executed, we need a small pause
                Thread.Sleep(10);
                Assert.Equal(0, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");
                Thread.Sleep(500);

                Assert.Equal(TaskStatus.Running, task.Status);

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("[2] Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"[2] Iteration {i++}");
                                try
                                {
                                    Thread.Sleep(1000);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            output.WriteLine("[2] Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                Assert.Equal(TaskStatus.Created, task1.Status);
                Assert.Equal(1, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");

                var task2 = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("[3] Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"[3] Iteration {i++}");
                                try
                                {
                                    Thread.Sleep(1000);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            output.WriteLine("[3] Stopped sleep");
                            t.CancellationToken.ThrowIfCancellationRequested();
                        },
                        customTaskSettings: customTaskSettings));

                Assert.Equal(TaskStatus.Created, task2.Status);

                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine("[1] Running");
                Assert.Equal(2, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");

                task1.Start();
                Thread.Sleep(10);

                Assert.Equal(TaskStatus.Running, task1.Status);
                output.WriteLine("[2] Running");
                Assert.Equal(1, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");

                task2.Start();
                Thread.Sleep(10);

                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine("[1] Running");

                Assert.Equal(0, scheduler.taskQueue.Count);
                output.WriteLine($"Tasks in queue: {scheduler.taskQueue.Count}");

                Assert.Equal(TaskStatus.Running, task1.Status);
                output.WriteLine("[2] Running");

                Assert.Equal(TaskStatus.Running, task2.Status);
                output.WriteLine("[3] Running");
            }

            [Fact]
            public void WillEmptyQueueOverTime()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromSeconds(5),
                };

                int taskIndex = 1;

                Action<ICustomTaskState, CustomCancellationToken> taskAction(int taskIndex)
                {
                    return (s, t) =>
                    {
                        int j = taskIndex;
                        output.WriteLine($"[{j}] Started run.");
                        int i = 0;
                        while (!t.CancellationToken.IsCancellationRequested && i < 2)
                        {
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{taskIndex}] Iteration {i++}");
                            try
                            {
                                Thread.Sleep(1000);
                            }
                            catch (ThreadInterruptedException) { }
                        }
                        output.WriteLine($"[{taskIndex}] Stopped sleep");
                        t.CancellationToken.ThrowIfCancellationRequested();
                    };
                }

                var task = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task2 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task3 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                Assert.Equal(4, scheduler.taskQueue.Count);

                task.Start();
                task1.Start();
                task2.Start();

                Thread.Sleep(10);
                Assert.Equal(1, scheduler.taskQueue.Count);

                task3.Start();
                Thread.Sleep(10);
                Assert.Equal(1, scheduler.taskQueue.Count);
                Assert.Equal(TaskStatus.WaitingForActivation, task3.Status);

                Thread.Sleep(TimeSpan.FromSeconds(2.1));

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task2.Status);

                Assert.Equal(TaskStatus.Running, task3.Status);

                Thread.Sleep(TimeSpan.FromSeconds(2.1));
            }

            [Fact]
            public void CanPreemptiveSchedule()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(10),
                    MaxRunDuration = TimeSpan.FromMinutes(10),
                };

                int taskIndex = 1;

                Action<ICustomTaskState, CustomCancellationToken> taskAction(int taskIndex)
                {
                    return (s, t) =>
                    {
                        int j = taskIndex;
                        output.WriteLine($"[{j}] Started run.");
                        int i = 0;
                        while (!t.CancellationToken.IsCancellationRequested && i < 2)
                        {
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{taskIndex}] Iteration {i++}");
                            // TODO remove ThreadInterruptExceptions from this similar blocks
                            Thread.Sleep(1000);
                        }
                        output.WriteLine($"[{taskIndex}] Stopped sleep");
                        t.CancellationToken.ThrowIfCancellationRequested();
                    };
                }

                var task = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings with { Priority = basePriority++ }));

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings with { Priority = basePriority++ }));

                var task2 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings with { Priority = basePriority++ }));

                var task3 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings with { Priority = basePriority++ }));

                // Ensure expected task queue state

                Assert.Equal(4, scheduler.taskQueue.Count);

                Assert.Equal(task3, scheduler.taskQueue.Peek());
                Assert.Equal(task, scheduler.GetScheduledTasks().Last());

                Assert.Equal(task1, scheduler.GetScheduledTasks().SkipLast(1).Last());
                Assert.Equal(task2, scheduler.GetScheduledTasks().Skip(1).First());

                // Use up thread pool threads with lower priority tasks

                task.Start();
                task1.Start();
                task2.Start();

                // Ensure threads pool threads are used and that the highest priority task has not been considered for scheduling yet
                Thread.Sleep(40);
                Assert.Equal(TaskStatus.Running, task.Status);
                Assert.Equal(TaskStatus.Running, task1.Status);
                Assert.Equal(TaskStatus.Running, task2.Status);
                Assert.Equal(TaskStatus.Created, task3.Status);

                //  When a higher priority task is started, the thread running the lowest priority task will be interrupted and scheduled to execute it
                task3.Start();

                Thread.Sleep(40);
                // task has lowest priority, so it's waiting to be scheduled once its thread preempts task3 with higher priority
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
                Assert.Equal(TaskStatus.Running, task1.Status);
                Assert.Equal(TaskStatus.Running, task2.Status);
                Assert.Equal(TaskStatus.Running, task3.Status);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                // First iteration completed for task3
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);
                Assert.Equal(TaskStatus.Running, task1.Status);
                Assert.Equal(TaskStatus.Running, task2.Status);
                Assert.Equal(TaskStatus.Running, task3.Status);

                // Second iteration completed for task3 - finished, as well as task1 and task2
                // task begins executing since task3's thread has been freed up and it's the only task left in the queue
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Assert.Equal(TaskStatus.Running, task.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task3.Status);

                // Ensure everything has completed
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task3.Status);
            }

            [Fact]
            public void CanSerializeTaskState_Null()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    Thread.Sleep(200);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customTaskSettings: customTaskSettings));

                var serialized = task.Serialize<SumState>();
                output.WriteLine(serialized);
                Assert.Equal(JsonSerializer.Serialize((object)null), serialized);
            }

            internal class SumState : ICustomTaskState
            {
                public int Value { get; set; }
            }

            [Fact]
            public void CanSerializeTaskState_Sum()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            var sumState = s as SumState;
                            output.WriteLine("Started run.");
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {sumState.Value++}");
                                try
                                {
                                    Thread.Sleep(200);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        state: new SumState
                        {
                            Value = 0,
                        },
                        customTaskSettings: customTaskSettings));

                var serialized = task.Serialize<SumState>();
                output.WriteLine(serialized);
                Assert.Equal(JsonSerializer.Serialize(new SumState { Value = 0 }), serialized);

                task.Start();
                Thread.Sleep(1000);

                var serializedState = task.Serialize<SumState>();
                output.WriteLine(serializedState);
            }

            [Fact]
            public void CanDeserializeTaskState()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var initialState = new SumState { Value = 20 };

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            var sumState = s as SumState;
                            output.WriteLine("Started run.");
                            while (!t.CancellationToken.IsCancellationRequested)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {sumState.Value++}");
                                try
                                {
                                    Thread.Sleep(200);
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        state: initialState,
                        customTaskSettings: customTaskSettings));

                var serialized = task.Serialize<SumState>();
                output.WriteLine(serialized);
                Assert.Equal(JsonSerializer.Serialize(new SumState { Value = 20 }), serialized);

                task.Start();
                Thread.Sleep(1000);
            }

            [Fact]
            public void CanManuallyForceDeadlock()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(10),
                    MaxRunDuration = Timeout.InfiniteTimeSpan,
                };

                var customResourceFile = new CustomResourceFile(testFile);
                var customResourceFile1 = new CustomResourceFile(testFile1);
                var customResourceFile2 = new CustomResourceFile(testFile2);
                var customResourceFile3 = new CustomResourceFile(testFile3);

                var customResources = ImmutableList.Create<CustomResource>(
                    customResourceFile,
                    customResourceFile1,
                    customResourceFile2,
                    customResourceFile3);

                var customResources1 = customResources.Reverse();

                int taskIndex = 1;

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, taskIndex++),
                        customResources: customResources,
                        customTaskSettings: customTaskSettings));

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources1, taskIndex++),
                        customResources: customResources,
                        customTaskSettings: customTaskSettings));

                task.Start();
                task1.Start();
                Thread.Sleep(TimeSpan.FromSeconds(6));

                Assert.Equal(TaskStatus.Running, task.Status);
                Assert.Equal(TaskStatus.Running, task1.Status);

                Action<ICustomTaskState, CustomCancellationToken> taskAction(CustomScheduler scheduler, ImmutableList<CustomResource> customResources, int taskIndex)
                {
                    return (s, t) =>
                    {
                        int ti = taskIndex;
                        var _customResources = customResources;
                        output.WriteLine("Started run.");
                        int i = 0;
                        int lockCount = 0;
                        while (!t.CancellationToken.IsCancellationRequested && i < 1)
                        {
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{ti}] Iteration {i++}");
                            try
                            {
                                // first lock
                                output.WriteLine($"[{ti}] Waiting for lock {lockCount} '{_customResources[0]}'");
                                scheduler.LockResourceAndAct(_customResources[0], () =>
                                {
                                    output.WriteLine($"[{ti}] Obtained lock {lockCount++} '{_customResources[0]}'");

                                    using var fs = File.OpenRead(customResources[0].Uri);
                                    using var reader = new StreamReader(fs);
                                    output.WriteLine(reader.ReadToEnd());

                                    // second lock
                                    output.WriteLine($"[{ti}] Waiting for lock {lockCount} '{_customResources[1]}'");
                                    scheduler.LockResourceAndAct(_customResources[1], () =>
                                    {
                                        output.WriteLine($"[{ti}] Obtained lock {lockCount++} '{_customResources[1]}'");

                                        using var fs = File.OpenRead(_customResources[1].Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());

                                        // third lock
                                        output.WriteLine($"[{ti}] Waiting for lock {lockCount} '{_customResources[2]}'");
                                        scheduler.LockResourceAndAct(_customResources[2], () =>
                                        {
                                            output.WriteLine($"[{ti}] Obtained lock {lockCount++} '{_customResources[2]}'");

                                            using var fs = File.OpenRead(_customResources[2].Uri);
                                            using var reader = new StreamReader(fs);
                                            output.WriteLine(reader.ReadToEnd());

                                            Thread.Sleep(Timeout.Infinite);
                                        });
                                        output.WriteLine($"[{ti}] Released lock {--lockCount}");
                                    });
                                    output.WriteLine($"[{ti}] Released lock {--lockCount}");

                                });
                                output.WriteLine($"[{ti}] Released lock {--lockCount}");
                            }
                            catch (ThreadInterruptedException) { }
                        }
                        t.CancellationToken.ThrowIfCancellationRequested();
                        output.WriteLine($"[{ti}] Stopped sleep");
                    };
                }
            }

            [Fact]
            public void CanLockResource()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var customResourceFile = new CustomResourceFile(testFile);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 2)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    output.WriteLine($"Waiting for lock");
                                    scheduler.LockResourceAndAct(customResourceFile, () =>
                                    {
                                        output.WriteLine($"Obtained lock");
                                        using var fs = File.OpenRead(customResourceFile.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());
                                        Thread.Sleep(200);
                                    });
                                    output.WriteLine($"Released lock");
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customResources: ImmutableList.Create<CustomResource>(customResourceFile),
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(430));

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }

            [Fact]
            public void CantLockUnregisteredResource()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(10),
                    MaxRunDuration = Timeout.InfiniteTimeSpan,
                };

                var customResourceFile = new CustomResourceFile(testFile);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 2)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    output.WriteLine($"Waiting for lock");
                                    var ex = Assert.Throws<ArgumentException>(() =>
                                    {
                                        scheduler.LockResourceAndAct(customResourceFile, () =>
                                        {
                                            output.WriteLine($"Obtained lock");
                                            using var fs = File.OpenRead(customResourceFile.Uri);
                                            using var reader = new StreamReader(fs);
                                            output.WriteLine(reader.ReadToEnd());
                                            Thread.Sleep(200);
                                        });
                                        output.WriteLine($"Released lock");
                                    });
                                    output.WriteLine(ex.Message);
                                    throw ex;
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromSeconds(2));

                Assert.Equal(TaskStatus.Faulted, task.Status);
            }

            [Fact]
            public void CanAvoidDeadlockMultipleResources()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(2),
                    MaxRunDuration = TimeSpan.FromMinutes(2),
                };

                var customResourceFile = new CustomResourceFile(testFile);
                var customResourceFile1 = new CustomResourceFile(testFile1);
                var customResourceFile2 = new CustomResourceFile(testFile2);
                var customResourceFile3 = new CustomResourceFile(testFile3);

                ImmutableList<CustomResource> customResources = ImmutableList.Create<CustomResource>(
                    customResourceFile,
                    customResourceFile1,
                    customResourceFile2);

                var timeout = TimeSpan.FromMilliseconds(200);

                int taskIndex = 1;

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex++),
                        customResources: customResources,
                        customTaskSettings: customTaskSettings));

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex++),
                        customResources: customResources,
                        customTaskSettings: customTaskSettings));

                task.Start();
                task1.Start();
                Thread.Sleep(TimeSpan.FromSeconds(1.2));

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);

                Action<ICustomTaskState, CustomCancellationToken> taskAction(
                    CustomScheduler scheduler,
                    ImmutableList<CustomResource> customResources,
                    TimeSpan timeout,
                    int taskIndex)
                {
                    return (s, t) =>
                    {
                        int ti = taskIndex;
                        output.WriteLine($"[{ti}] Started run.");
                        int i = 0;
                        while (!t.CancellationToken.IsCancellationRequested && i < 2)
                        {
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{ti}] Iteration {i++}");
                            try
                            {
                                output.WriteLine($"[{ti}] Waiting for lock");
                                scheduler.LockResourcesAndAct(customResources, () =>
                                {
                                    output.WriteLine($"[{ti}] Obtained lock");
                                    foreach (var item in customResources)
                                    {
                                        using var fs = File.OpenRead(item.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());
                                    }

                                    Thread.Sleep(timeout);
                                });
                                output.WriteLine($"[{ti}] Released lock");
                            }
                            catch (ThreadInterruptedException) { }
                        }
                        t.CancellationToken.ThrowIfCancellationRequested();
                        output.WriteLine($"[{ti}] Stopped sleep");
                    };
                }
            }

            [Fact]
            public void CanLockMultipleResourcesSimultaneously()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromMinutes(1),
                };

                var customResourceFile = new CustomResourceFile(testFile);
                var customResourceFile1 = new CustomResourceFile(testFile1);
                var customResourceFile2 = new CustomResourceFile(testFile2);

                ImmutableList<CustomResource> customResources = ImmutableList.Create<CustomResource>(
                    customResourceFile,
                    customResourceFile1,
                    customResourceFile2);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 2)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    output.WriteLine($"Waiting for lock");
                                    scheduler.LockResourcesAndAct(customResources, () =>
                                    {
                                        output.WriteLine($"Obtained lock");
                                        foreach (var item in customResources)
                                        {
                                            using var fs = File.OpenRead(item.Uri);
                                            using var reader = new StreamReader(fs);
                                            output.WriteLine(reader.ReadToEnd());
                                        }

                                        Thread.Sleep(200);
                                    });
                                    output.WriteLine($"Released lock");
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromSeconds(0.500));

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }

            [Fact]
            public void CanTimeoutTaskWithResourceRelease()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                var customResourceFile = new CustomResourceFile(testFile);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 2)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        output.WriteLine($"Waiting for lock");
                                        scheduler.LockResourceAndAct(customResourceFile, () =>
                                        {
                                            if (t.CancellationToken.IsCancellationRequested)
                                            {
                                                output.WriteLine($"Cancellation requested");
                                                t.CancellationToken.ThrowIfCancellationRequested();
                                            }
                                            output.WriteLine($"Obtained lock");
                                            using var fs = File.OpenRead(customResourceFile.Uri);
                                            using var reader = new StreamReader(fs);
                                            output.WriteLine(reader.ReadToEnd());
                                            Thread.Sleep(TimeSpan.FromSeconds(1.5));
                                        });
                                        output.WriteLine($"Released lock");
                                    }
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customResources: ImmutableList.Create<CustomResource>(customResourceFile),
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromSeconds(3));

                Assert.Equal(TaskStatus.Canceled, task.Status);
            }

            [Fact]
            public void CanCancelTaskWithResourceRelease()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromMinutes(1),
                };

                var customResourceFile = new CustomResourceFile(testFile);

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            output.WriteLine("Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 2)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"Iteration {i++}");
                                try
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        output.WriteLine($"Waiting for lock");
                                        scheduler.LockResourceAndAct(customResourceFile, () =>
                                            {
                                                if (t.CancellationToken.IsCancellationRequested)
                                                {
                                                    output.WriteLine($"Cancellation requested");
                                                    t.CancellationToken.ThrowIfCancellationRequested();
                                                }
                                                output.WriteLine($"Obtained lock");
                                                using var fs = File.OpenRead(customResourceFile.Uri);
                                                using var reader = new StreamReader(fs);
                                                output.WriteLine(reader.ReadToEnd());
                                                Thread.Sleep(TimeSpan.FromSeconds(1.5));
                                            });
                                    }
                                    output.WriteLine($"Released lock");
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Stopped sleep");
                        },
                        customResources: ImmutableList.Create<CustomResource>(customResourceFile),
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Assert.Equal(TaskStatus.Running, task.Status);

                task.Stop();
                Thread.Sleep(TimeSpan.FromMilliseconds(600));

                Assert.Equal(TaskStatus.Canceled, task.Status);
            }

            private class MockTaskResourceState : ICustomTaskState
            {
                public int TaskIndex { get; init; }
                public int TaskPriority { get; set; }
                public bool HoldingLock { get; set; }

                public override string ToString()
                {
                    return $"{nameof(TaskIndex)} = {TaskIndex}, {nameof(TaskPriority)} = {TaskPriority}, {nameof(HoldingLock)} = {HoldingLock}";
                }
            }

            [Fact]
            public void CanPriorityInvert()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(10),
                    MaxRunDuration = TimeSpan.FromMinutes(10),
                };

                var customResourceFile = new CustomResourceFile(testFile);
                var customResourceFile1 = new CustomResourceFile(testFile1);
                var customResourceFile2 = new CustomResourceFile(testFile2);
                var customResourceFile3 = new CustomResourceFile(testFile3);

                ImmutableList<CustomResource> customResources = ImmutableList.Create<CustomResource>(
                    customResourceFile,
                    customResourceFile1,
                    customResourceFile2,
                    customResourceFile3);

                var timeout = TimeSpan.FromMilliseconds(200);

                int taskIndex = 1;

                var taskL = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority }));

                var taskM = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority + 1 },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority + 1 }));

                var taskH = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority + 2 },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority + 2 }));

                var stateL = taskL.State as MockTaskResourceState;
                var stateM = taskM.State as MockTaskResourceState;
                var stateH = taskH.State as MockTaskResourceState;

                taskL.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                taskM.Start();
                // L ahead of L by 50
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                // L ahead of H by 100
                // M ahead of H by 50
                taskH.Start();

                // L iteration 0
                Assert.True(stateL.HoldingLock);
                Assert.False(stateM.HoldingLock);
                Assert.False(stateH.HoldingLock);

                Thread.Sleep(TimeSpan.FromMilliseconds(20));

                Assert.Equal(TaskStatus.Running, taskL.Status);
                Assert.Equal(TaskStatus.Running, taskM.Status);
                Assert.Equal(TaskStatus.Running, taskH.Status);

                output.WriteLine("--");
                Thread.Sleep(timeout);

                Assert.Equal(TaskStatus.RanToCompletion, taskL.Status);

                // H iteration 0
                Assert.False(stateL.HoldingLock);
                Assert.False(stateM.HoldingLock);
                Assert.True(stateH.HoldingLock);

                output.WriteLine("--");
                Thread.Sleep(timeout);
                // M iteration 0
                Assert.Equal(TaskStatus.RanToCompletion, taskH.Status);
                Assert.Equal(TaskStatus.Running, taskM.Status);
                Assert.False(stateL.HoldingLock);
                Assert.True(stateM.HoldingLock);
                Assert.False(stateH.HoldingLock);

                Thread.Sleep(timeout);

                Action<ICustomTaskState, CustomCancellationToken> taskAction(
                    CustomScheduler scheduler,
                    ImmutableList<CustomResource> customResources,
                    TimeSpan timeout,
                    int taskIndex)
                {
                    return (s, t) =>
                    {
                        var resourceState = s as MockTaskResourceState;
                        int ti = taskIndex;
                        output.WriteLine($"[{ti}] Started run.");
                        int i = 0;
                        while (i < 1)
                        {
                            output.WriteLine($"[{ti}] Iteration {i++}");
                            try
                            {
                                output.WriteLine($"[{ti}] Waiting for lock");
                                scheduler.LockResourceAndAct(customResourceFile, () =>
                                    {
                                        resourceState.HoldingLock = true;
                                        output.WriteLine($"[{ti}] Obtained lock");

                                        using var fs = File.OpenRead(customResourceFile.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());

                                        Thread.Sleep(timeout);
                                        output.WriteLine($"[{ti}] Releasing lock");
                                    });
                                resourceState.HoldingLock = false;
                            }
                            catch (ThreadInterruptedException) { output.WriteLine($"[{ti}] Thread interrupted"); }
                        }
                        output.WriteLine($"[{ti}] Stopped sleep");
                    };
                }
            }

            [Fact]
            public void CanPriorityInvertMultiple()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(10),
                    MaxRunDuration = TimeSpan.FromMinutes(10),
                };

                var customResourceFile = new CustomResourceFile(testFile);
                var customResourceFile1 = new CustomResourceFile(testFile1);
                var customResourceFile2 = new CustomResourceFile(testFile2);
                var customResourceFile3 = new CustomResourceFile(testFile3);

                ImmutableList<CustomResource> customResources = ImmutableList.Create<CustomResource>(
                    customResourceFile,
                    customResourceFile1,
                    customResourceFile2,
                    customResourceFile3);

                var timeout = TimeSpan.FromMilliseconds(200);

                int taskIndex = 1;

                var taskL = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority }));

                var taskM = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority + 1 },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority + 1 }));

                var taskH = scheduler.PrepareTask(
                    new MockCustomTask(
                        taskAction(scheduler, customResources, timeout, taskIndex),
                        state: new MockTaskResourceState { TaskIndex = taskIndex++, TaskPriority = basePriority + 2 },
                        customResources: customResources,
                        customTaskSettings: customTaskSettings with { Priority = basePriority + 2 }));

                var stateL = taskL.State as MockTaskResourceState;
                var stateM = taskM.State as MockTaskResourceState;
                var stateH = taskH.State as MockTaskResourceState;

                taskL.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                taskM.Start();
                // L ahead of L by 50
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                // L ahead of H by 100
                // M ahead of H by 50
                taskH.Start();

                // L iteration 0
                Assert.True(stateL.HoldingLock);
                Assert.False(stateM.HoldingLock);
                Assert.False(stateH.HoldingLock);

                Thread.Sleep(TimeSpan.FromMilliseconds(20));

                Assert.Equal(TaskStatus.Running, taskL.Status);
                Assert.Equal(TaskStatus.Running, taskM.Status);
                Assert.Equal(TaskStatus.Running, taskH.Status);

                output.WriteLine("--");
                Thread.Sleep(timeout);

                Assert.Equal(TaskStatus.RanToCompletion, taskL.Status);

                // H iteration 0
                Assert.False(stateL.HoldingLock);
                Assert.False(stateM.HoldingLock);
                Assert.True(stateH.HoldingLock);

                output.WriteLine("--");
                Thread.Sleep(timeout);
                // M iteration 0
                Assert.Equal(TaskStatus.RanToCompletion, taskH.Status);
                Assert.Equal(TaskStatus.Running, taskM.Status);
                Assert.False(stateL.HoldingLock);
                Assert.True(stateM.HoldingLock);
                Assert.False(stateH.HoldingLock);

                Thread.Sleep(timeout);

                Action<ICustomTaskState, CustomCancellationToken> taskAction(
                    CustomScheduler scheduler,
                    ImmutableList<CustomResource> customResources,
                    TimeSpan timeout,
                    int taskIndex)
                {
                    return (s, t) =>
                    {
                        var resourceState = s as MockTaskResourceState;
                        int ti = taskIndex;
                        output.WriteLine($"[{ti}] Started run.");
                        int i = 0;
                        while (i < 1)
                        {
                            output.WriteLine($"[{ti}] Iteration {i++}");
                            try
                            {
                                output.WriteLine($"[{ti}] Waiting for lock");
                                scheduler.LockResourcesAndAct(customResources, () =>
                                {
                                    resourceState.HoldingLock = true;
                                    output.WriteLine($"[{ti}] Obtained lock");
                                    foreach (var item in customResources)
                                    {
                                        using var fs = File.OpenRead(item.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());
                                    }

                                    Thread.Sleep(timeout);
                                    output.WriteLine($"[{ti}] Releasing lock");
                                });
                                resourceState.HoldingLock = false;
                            }
                            catch (ThreadInterruptedException) { output.WriteLine($"[{ti}] Thread interrupted"); }
                        }
                        output.WriteLine($"[{ti}] Stopped sleep");
                    };
                }
            }

            [Fact]
            public void CanRespectMaxConcurrentTasks()
            {
                var settings = GetCustomSchedulerSettings(out var maxConcurrentTasks, out var maxCores);
                maxConcurrentTasks = 2;
                settings = settings with { MaxConcurrentTasks = maxConcurrentTasks };
                var scheduler = new CustomScheduler(settings);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);
                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Now.AddMinutes(1),
                    MaxRunDuration = TimeSpan.FromSeconds(5),
                };

                int taskIndex = 1;

                Action<ICustomTaskState, CustomCancellationToken> taskAction(int taskIndex)
                {
                    return (s, t) =>
                    {
                        int j = taskIndex;
                        output.WriteLine($"[{j}] Started run.");
                        int i = 0;
                        while (!t.CancellationToken.IsCancellationRequested && i < 2)
                        {
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{taskIndex}] Iteration {i++}");
                            try
                            {
                                Thread.Sleep(1000);
                            }
                            catch (ThreadInterruptedException) { }
                        }
                        output.WriteLine($"[{taskIndex}] Stopped sleep");
                        t.CancellationToken.ThrowIfCancellationRequested();
                    };
                }

                var task = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task2 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                var task3 = scheduler.PrepareTask(
                    new MockCustomTask(taskAction(taskIndex++),
                    customTaskSettings: customTaskSettings));

                Assert.Equal(4, scheduler.taskQueue.Count);

                task.Start();
                task1.Start();
                task2.Start();
                task3.Start();

                Thread.Sleep(10);
                Assert.Equal(2, scheduler.taskQueue.Count);

                Thread.Sleep(2100);
                Assert.Equal(0, scheduler.taskQueue.Count);

                Thread.Sleep(2100);

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
                Assert.Equal(TaskStatus.RanToCompletion, task3.Status);
            }

            [Fact]
            public void CanPermaLockResource()
            {
                GetScheduler(out var maxConcurrentTasks, out var maxCores, out var scheduler);

                CustomTaskTests.GetCustomTaskSettings(out var deadLine, out var maxTaskCores, out var maxRunDuration, out var basePriority, out var customTaskSettings);

                customTaskSettings = customTaskSettings with
                {
                    Deadline = DateTime.Today.AddDays(1),
                    MaxRunDuration = TimeSpan.FromDays(1),
                };

                var customResourceFile = new CustomResourceFile(testFile1);

                var taskIndex = 1;
                var timeout = Timeout.InfiniteTimeSpan;
                var acquired = false;
                var acquired1 = false;

                var task = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            int ti = taskIndex++;
                            output.WriteLine($"[{ti}] Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 1)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"[{ti}] Iteration {i++}");
                                try
                                {
                                    output.WriteLine($"[{ti}] Waiting for lock");
                                    scheduler.LockResourceAndAct(customResourceFile, () =>
                                    {
                                        acquired = true;
                                        output.WriteLine($"[{ti}] Obtained lock");
                                        using var fs = File.OpenRead(customResourceFile.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());
                                        Thread.Sleep(timeout);
                                    });
                                    output.WriteLine($"[{ti}] Released lock");
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{ti}] Stopped sleep");
                        },
                        customResources: ImmutableList.Create<CustomResource>(customResourceFile),
                        customTaskSettings: customTaskSettings));

                var timeout1 = TimeSpan.FromMilliseconds(200);
                var task1 = scheduler.PrepareTask(
                    new MockCustomTask(
                        (s, t) =>
                        {
                            int ti = taskIndex++;
                            TimeSpan sleepTime = timeout;

                            output.WriteLine($"[{ti}] Started run.");
                            int i = 0;
                            while (!t.CancellationToken.IsCancellationRequested && i < 1)
                            {
                                t.CancellationToken.ThrowIfCancellationRequested();
                                output.WriteLine($"[{ti}] Iteration {i++}");
                                try
                                {
                                    output.WriteLine($"[{ti}] Waiting for lock");
                                    scheduler.LockResourceAndAct(customResourceFile, () =>
                                    {
                                        acquired1 = true;
                                        output.WriteLine($"[{ti}] Obtained lock");
                                        using var fs = File.OpenRead(customResourceFile.Uri);
                                        using var reader = new StreamReader(fs);
                                        output.WriteLine(reader.ReadToEnd());
                                        Thread.Sleep(timeout1);
                                    });
                                    output.WriteLine($"[{ti}] Released lock");
                                }
                                catch (ThreadInterruptedException) { }
                            }
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine($"[{ti}] Stopped sleep");
                        },
                        customResources: ImmutableList.Create<CustomResource>(customResourceFile),
                        customTaskSettings: customTaskSettings));

                task.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(300));
                Assert.True(acquired);
                Assert.Equal(TaskStatus.Running, task.Status);

                task1.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(400));
                Assert.False(acquired1);
                Assert.Equal(TaskStatus.Running, task1.Status);
            }
        }

        private static void GetScheduler(out int maxConcurrentTasks, out int maxCores, out CustomScheduler scheduler)
        {
            var schedulerSettings = GetCustomSchedulerSettings(out maxConcurrentTasks, out maxCores);
            scheduler = new CustomScheduler(schedulerSettings);
        }

        private static CustomSchedulerSettings GetCustomSchedulerSettings(out int maxConcurrentTasks, out int maxCores)
        {
            maxConcurrentTasks = 3;
            maxCores = 3;
            var schedulerSettings = new CustomSchedulerSettings { MaxCores = maxCores, MaxConcurrentTasks = maxConcurrentTasks };

            return schedulerSettings;
        }
    }

    public class CustomTaskTests
    {
        [Fact]
        public void GetSettings()
        {
            DateTime deadLine;
            int maxCores, priority;
            TimeSpan maxRunDuration;
            CustomTaskSettings taskSettings;

            GetCustomTaskSettings(out deadLine, out maxCores, out maxRunDuration, out priority, out taskSettings);
            var mockCustomTask = new MockCustomTask(customTaskSettings: taskSettings);

            Assert.Equal(deadLine, mockCustomTask.Settings.Deadline);
            Assert.Equal(maxCores, mockCustomTask.Settings.MaxCores);
            Assert.Equal(priority, mockCustomTask.Settings.Priority);
            Assert.Equal(maxRunDuration, mockCustomTask.Settings.MaxRunDuration);
        }

        [Fact]
        public void NullSettings()
        {
            Assert.Throws<ArgumentNullException>(() => { new MockCustomTask(null); });
        }

        public static void GetCustomTaskSettings(
            out DateTime deadLine,
            out int maxCores,
            out TimeSpan maxRunDuration,
            out int priority,
            out CustomTaskSettings taskSettings)
        {
            deadLine = DateTime.Now.AddSeconds(2);
            maxCores = 3;
            maxRunDuration = TimeSpan.FromSeconds(1);
            priority = 0;
            taskSettings = new CustomTaskSettings
            {
                Deadline = deadLine,
                MaxCores = maxCores,
                MaxRunDuration = maxRunDuration,
                Priority = priority
            };
        }
    }
}
