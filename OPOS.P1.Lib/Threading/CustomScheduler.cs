using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public class CustomScheduler
    {
        public CustomSchedulerSettings Settings { get; init; }

        private PriorityQueue<CustomTask, int> tasks =
            new(new CustomTaskPriorityComparer());


        public CustomScheduler(CustomSchedulerSettings customSchedulerSettings)
        {
            Settings = customSchedulerSettings ?? throw new ArgumentNullException(nameof(customSchedulerSettings));

            if (Settings.MaxCores == 0)
                Settings = Settings with { MaxCores = Environment.ProcessorCount };
            else if (Settings.MaxCores < 0) throw new ArgumentOutOfRangeException(nameof(Settings.MaxCores));
        }

        public void Enqueue(CustomTask customTask)
        {
            tasks.Enqueue(customTask, customTask.Settings.Priority);
        }

        public CustomTask Dequeue()
        {
            var dequeued = tasks.Dequeue();
            return dequeued;
        }

        protected IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected void QueueTask(Task task)
        {
            throw new NotImplementedException();
        }

        protected bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            throw new NotImplementedException();
        }
    }
}
