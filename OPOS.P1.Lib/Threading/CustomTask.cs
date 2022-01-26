﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    /// <summary>
    /// CustomTask comparer that use uses task priority only.
    /// </summary>
    public class CustomTaskPriorityComparer : IComparer<CustomTask>
    {
        private readonly CustomTaskStatusComparer _statusComparer = new();

        public int Compare(CustomTask x, CustomTask y)
        {
            if (x is null && y is null) return 0;
            else if (x is null)
                return 1;
            else if (y is null)
                return -1;

            var priorityComparison = x.Settings.Priority.CompareTo(y.Settings.Priority);

            return priorityComparison;
        }
    }

    /// <summary>
    /// CustomTask comparer that uses the task status.
    /// </summary>
    public class CustomTaskComparer : IComparer<CustomTask>
    {
        public int Compare(CustomTask x, CustomTask y)
        {
            return CompareTasks(x, y);
        }

        public static int CompareTasks(CustomTask x, CustomTask y)
        {
            return CustomTaskStatusComparer.CompareTaskStatus((TaskStatus)(x?.Status), (TaskStatus)(y?.Status));
        }
    }

    /// <summary>
    /// CustomTask comparer that uses the task status.
    /// </summary>
    public class CustomTaskStatusComparer : IComparer<TaskStatus>
    {
        public int Compare(TaskStatus x, TaskStatus y)
        {
            return CompareTaskStatus(x, y);
        }

        public static int CompareTaskStatus(TaskStatus x, TaskStatus y)
        {
            if (x == y)
                return 0;

            var tuple = (x, y);
            var result = tuple switch
            {
                (TaskStatus.RanToCompletion, _) => -1,
                (_, TaskStatus.RanToCompletion) => 1,

                (TaskStatus.Canceled, _) => -1,
                (_, TaskStatus.Canceled) => 1,

                (TaskStatus.WaitingToRun, _) => -1,
                (_, TaskStatus.WaitingToRun) => 1,

                (TaskStatus.WaitingForActivation, _) => -1,
                (_, TaskStatus.WaitingForActivation) => 1,

                (TaskStatus.Created, _) => -1,
                (_, TaskStatus.Created) => -1,

                _ => 0
            };

            return result;
        }
    }

    public abstract class CustomTask : Task, ICustomTask, IComparable<CustomTask>
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

            Scheduler.PauseTask(this);
            return;
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

        public int CompareTo(CustomTask other)
        {
            return CustomTaskComparer.CompareTasks(this, other);
        }
    }
}
