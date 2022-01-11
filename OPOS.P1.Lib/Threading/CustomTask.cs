using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public class CustomTaskPriorityComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return x > y ? -1 : 1;
        }
    }

    public abstract class CustomTask : ICustomTask
    {
        public CustomTask(CustomTaskSettings customTaskSettings)
        {
            Settings = customTaskSettings ?? throw new ArgumentNullException(nameof(customTaskSettings));

            if (Settings.MaxCores == 0)
                Settings = Settings with { MaxCores = Environment.ProcessorCount };
            else if (Settings.MaxCores < 0) throw new ArgumentOutOfRangeException(nameof(Settings.MaxCores));
        }

        public CustomTaskSettings Settings { get; init; }

        public float Progress { get; protected set; }

        public TaskStatus Status { get; protected set; }

        public TimeSpan MaxRunDuration => Settings.MaxRunDuration;

        DateTime ICustomTask.Deadline => Settings.Deadline;

        int ICustomTask.UsableCores => Settings.MaxCores;

        int ICustomTask.Priority => Settings.Priority;

        public void LoadState()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void SaveState()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        void ICustomTask.LoadState()
        {
            throw new NotImplementedException();
        }

        void ICustomTask.Pause()
        {
            throw new NotImplementedException();
        }

        void ICustomTask.Resume()
        {
            throw new NotImplementedException();
        }

        void ICustomTask.SaveState()
        {
            throw new NotImplementedException();
        }

        void ICustomTask.Stop()
        {
            throw new NotImplementedException();
        }
    }
}
