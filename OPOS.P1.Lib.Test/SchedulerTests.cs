using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OPOS.P1.Lib.Test
{
    public class MockCustomTask : CustomTask
    {
        public MockCustomTask(CustomTaskSettings customTaskSettings) : base(customTaskSettings)
        {
        }
    }

    public class SchedulerTests
    {
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
                    tasks.Add(new MockCustomTask(customTaskSettings with
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
            var mockCustomTask = new MockCustomTask(taskSettings);

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
            deadLine = DateTime.Now.AddSeconds(1);
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
