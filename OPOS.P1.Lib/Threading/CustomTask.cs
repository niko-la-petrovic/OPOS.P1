using OPOS.P1.Lib.Algo;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("OPOS.P1.Lib.Test")]
namespace OPOS.P1.Lib.Threading
{
    public class IntegerDescendingComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return x > y ? -1 : 1;
        }
    }

    // TODO adjust to consider all the ordering
    public class CustomTaskPriorityComparer : IComparer<CustomTask>
    {
        public int Compare(CustomTask x, CustomTask y)
        {
            return x.Settings.Priority > y.Settings.Priority ? -1 : 1;
        }
    }

    public abstract class CustomTask : Task, ICustomTask
    {
        public bool MetDeadline { get; set; }
        public bool WantsToRun { get; set; }

        internal CustomScheduler Scheduler { get; set; }

        public new Guid Id { get; init; } = Guid.NewGuid();

        public CustomTaskSettings Settings { get; init; }

        public float Progress { get; internal set; }

        public new TaskStatus Status { get; internal set; }

        public DateTime LastStartedRunning { get; internal set; }
        public TimeSpan TotalRunDuration { get; internal set; }

        internal ICustomTaskState State { get; set; }

        public CustomTask(
            Action<ICustomTaskState, CustomCancellationToken> runAction,
            ICustomTaskState state = default,
            CustomTaskSettings customTaskSettings = default,
            CustomCancellationToken cancellationToken = default,
            ImmutableList<CustomResource> customResources = null,
            CustomScheduler scheduler = null) : base(() => runAction(state, cancellationToken))
        {
            Settings = customTaskSettings ?? throw new ArgumentNullException(nameof(customTaskSettings));

            if (Settings.MaxCores == 0)
                Settings = Settings with { MaxCores = Environment.ProcessorCount };
            else if (Settings.MaxCores < 0) throw new ArgumentOutOfRangeException(nameof(customTaskSettings));

            State = state;
            Run = runAction;
            Scheduler = scheduler;
        }

        int ICustomTask.UsableCores => Settings.MaxCores;

        int ICustomTask.Priority => Settings.Priority;

        public new Action<ICustomTaskState, CustomCancellationToken> Run { internal get; init; }

        public Action<ICustomTaskState, CustomCancellationToken> Resume { internal get; init; }

        public new void Start()
        {
            if (Status != TaskStatus.Created)
                throw new InvalidOperationException($"Task must be in steate {TaskStatus.Created}.");

            WantsToRun = true;
            Scheduler.UpdateTaskStatus(this, TaskStatus.WaitingForActivation);
        }

        public void Pause()
        {
            if (Status != TaskStatus.Running)
                throw new InvalidOperationException($"Cannot pause a task that isn't already running.");

            // TODO don't use update task status or add another method for pausing
            // TODO custom token for pausing
            WantsToRun = false;
            Scheduler.UpdateTaskStatus(this, TaskStatus.Created);
        }

        public void Continue()
        {
            if (Status != TaskStatus.Created)
                throw new InvalidOperationException($"Cannot resume a task that is already running.");

            WantsToRun = true;
            Scheduler.UpdateTaskStatus(this, TaskStatus.WaitingForActivation);
        }

        public void Stop()
        {
            if (Status != TaskStatus.Running)
                throw new InvalidOperationException($"Cannot stop a task that isn't running.");

            WantsToRun = false;
            Scheduler.UpdateTaskStatus(this, TaskStatus.Canceled);
        }

        public string Serialize<T>() where T : ICustomTaskState
        {
            string json = null;

            json = JsonSerializer.Serialize((T)State);

            return json;
        }

        public abstract CustomTask Deserialize(string json);

        public override bool Equals(object obj)
        {
            return obj is CustomTask task &&
                   Id.Equals(task.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
        {
            return $"Id = {Id}";
        }
    }
}
