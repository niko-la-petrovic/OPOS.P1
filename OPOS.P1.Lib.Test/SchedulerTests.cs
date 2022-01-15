﻿using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static OPOS.P1.Lib.Threading.CustomScheduler;

namespace OPOS.P1.Lib.Test
{
    public class MockCustomTask : CustomTask
    {
        public MockCustomTask(
            Action<ICustomTaskState, CustomCancellationToken> runAction = default,
            ICustomTaskState state = default,
            CustomTaskSettings customTaskSettings = default) : base(runAction, state, customTaskSettings)
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

            foreach (var task in tasks.Reverse<MockCustomTask>())
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

        public class TaskOperations
        {
            private readonly ITestOutputHelper output;

            public TaskOperations(ITestOutputHelper output)
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
                output.WriteLine(task.Status.ToString());
                Assert.Equal(TaskStatus.Running, task.Status);
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
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Timed out.");
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
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Timed out.");
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
                            t.CancellationToken.ThrowIfCancellationRequested();
                            output.WriteLine("Timed out.");
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
                            output.WriteLine("Timed out.");
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
                Thread.Sleep(250);

                Assert.Equal(1, actionRunCount);
                Assert.Equal(TaskStatus.Running, task.Status);
                output.WriteLine(task.Serialize<SumState>());

                task.Pause();

                var state = task.State as SumState;
                var serializedState = task.Serialize<SumState>();
                output.WriteLine(serializedState);
                Assert.Equal(1, actionRunCount);
                Assert.Equal(TaskStatus.Created, task.Status);
                Assert.Equal(2, state.Value);

                // TODO
                task.Continue();
                Thread.Sleep(250);

                Assert.Equal(5, state.Value);
                Assert.Equal(TaskStatus.Running, task.Status);
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
                            output.WriteLine("Timed out.");
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
                            output.WriteLine("Timed out.");
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
                            output.WriteLine("Timed out.");
                        },
                        state: initialState,
                        customTaskSettings: customTaskSettings));

                var serialized = task.Serialize<SumState>();
                output.WriteLine(serialized);
                Assert.Equal(JsonSerializer.Serialize(new SumState { Value = 20 }), serialized);

                task.Start();
                Thread.Sleep(1000);
            }
        }

        private static void GetScheduler(out int maxConcurrentTasks, out int maxCores, out CustomScheduler scheduler)
        {
            maxConcurrentTasks = 2;
            maxCores = 3;
            var schedulerSettings = new CustomSchedulerSettings { MaxCores = maxCores, MaxConcurrentTasks = maxConcurrentTasks };
            scheduler = new CustomScheduler(schedulerSettings);
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
