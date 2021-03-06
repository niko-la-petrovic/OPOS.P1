using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static OPOS.P1.Lib.Threading.CustomScheduler;
using Xunit.Abstractions;

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

    public class SavedTask
    {
        public string AssemblyQualifiedName { get; set; }
        public string Id { get; set; }
        public float Progress { get; set; }
        public bool WantsToRun { get; set; }
        public TaskStatus Status { get; set; }
        public CustomTaskSettings Settings { get; set; }
        public CustomResource[] CustomResources { get; set; }
        public string DerivedSerializedState { get; set; }
    }

    /// <summary>
    /// CustomTask comparer that use uses task priority only.
    /// </summary>
    public class CustomTaskPriorityComparer : IComparer<CustomTask>
    {
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

                (TaskStatus.Faulted, _) => -1,
                (_, TaskStatus.Faulted) => 1,

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

    public abstract class CustomTask : ICustomTask, IComparable<CustomTask>, IEquatable<CustomTask>
    {
        private volatile float progress;

        public bool MetDeadline { get; set; }
        public bool WantsToRun { get; set; }

        internal CustomScheduler Scheduler { get; set; }

        public Guid Id { get; private set; } = Guid.NewGuid();

        public CustomTaskSettings Settings { get; init; }

        public float Progress
        {
            get => progress;
            internal set
            {
                if (progress == value)
                    return;

                progress = value;
                Scheduler?.OnTaskProgressChanged(new TaskProgressEventArgs { Progress = value, Task = this });
            }
        }

        public new TaskStatus Status { get; internal set; }

        public DateTime LastStartedRunning { get; internal set; }
        public TimeSpan TotalRunDuration { get; internal set; }

        internal virtual ICustomTaskState State { get; set; }

        public AggregateException Exception { get; internal set; }

        public ImmutableList<CustomResource> CustomResources { get; internal set; }

        public CustomTask(
            Action<ICustomTaskState, CustomCancellationToken> runAction,
            ICustomTaskState state = default,
            CustomTaskSettings customTaskSettings = default,
            ImmutableList<CustomResource> customResources = null,
            CustomScheduler scheduler = null)
        {
            Settings = customTaskSettings ?? throw new ArgumentNullException(nameof(customTaskSettings));

            if (Settings.Parallelize)
            {
                if (Settings.MaxCores == 0)
                    Settings = Settings with { MaxCores = Environment.ProcessorCount };
                else if (Settings.MaxCores < 0) throw new ArgumentOutOfRangeException(nameof(customTaskSettings));
            }
            else
                Settings = Settings with { MaxCores = 1 };

            State = state;
            Run = runAction;
            Scheduler = scheduler;
            CustomResources = customResources;
        }

        int ICustomTask.UsableCores => Settings.MaxCores;

        int ICustomTask.Priority => Settings.Priority;

        internal Action<ICustomTaskState, CustomCancellationToken> Run { get; set; }

        public void Start()
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

        protected void LockResourceAndAct(CustomResource customResource, Action action)
        {
            Scheduler.LockResourceAndAct(customResource, action);
        }

        protected void LockResourceAndAct(string uri, Action action)
        {
            Scheduler.LockResourceAndAct(uri, action);
        }

        protected void LockResourcesAndAct(ImmutableList<CustomResource> requestedResources, Action action)
        {
            Scheduler.LockResourcesAndAct(requestedResources, action);
        }

        // TODO rename to Cancel
        public void Stop()
        {
            if (Status != TaskStatus.Running)
                throw new InvalidOperationException($"Cannot stop a task that isn't running.");

            WantsToRun = false;
            Scheduler.UpdateTaskStatus(this, TaskStatus.Canceled);
        }

        public virtual string Serialize()
        {
            var stateJson = JsonSerializer.Serialize(State, State.GetType());

            var savedState = new SavedTask
            {
                Id = Id.ToString(),
                DerivedSerializedState = stateJson,
                Settings = Settings,
                WantsToRun = WantsToRun,
                Progress = Progress,
                Status = Status,
                AssemblyQualifiedName = GetType().AssemblyQualifiedName,
                CustomResources = new List<CustomResource>(CustomResources).ToArray(),
            };

            var json = JsonSerializer.Serialize(savedState);

            return json;
        }

        public static SavedTask GetSavedTask(string json)
        {
            var savedTask = JsonSerializer.Deserialize<SavedTask>(json);
            return savedTask;
        }

        public static CustomTask DeserializeWithSavedTask(string json, out SavedTask savedTask)
        {
            savedTask = GetSavedTask(json);

            var type = Type.GetType(savedTask.AssemblyQualifiedName);
            if (type is null)
                throw new InvalidOperationException(nameof(savedTask.AssemblyQualifiedName));

            if (!type.IsAssignableTo(typeof(CustomTask)))
                throw new InvalidOperationException(nameof(savedTask.AssemblyQualifiedName));

            // TODO make it so that the ctors have the same arguments
            var ctor = type.GetConstructor(new[] {
                typeof(Action<ICustomTaskState, CustomCancellationToken>),
                typeof(ICustomTaskState),
                typeof(CustomTaskSettings),
                typeof(ImmutableList<CustomResource>),
                typeof(CustomScheduler)
            });

            var customTask = ctor.Invoke(new object[] {
                null,
                null,
                savedTask.Settings,
                ImmutableList.Create(savedTask.CustomResources),
                null
            })
                as CustomTask;
            if (customTask is null)
                throw new ArgumentException(nameof(json));

            customTask.Id = new Guid(savedTask.Id);
            customTask.WantsToRun = savedTask.WantsToRun;
            customTask.Progress = savedTask.Progress;

            return customTask;
        }

        public virtual CustomTask Deserialize(string json)
        {
            return DeserializeWithSavedTask(json, out _);
        }

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
            return $"Id = {Id}, Priority = {Settings.Priority}";
        }

        public int CompareTo(CustomTask other)
        {
            return CustomTaskComparer.CompareTasks(this, other);
        }

        bool IEquatable<CustomTask>.Equals(CustomTask other)
        {
            return Equals(other);
        }
    }
}
