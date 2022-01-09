using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public abstract class CustomTask : ICustomTask
    {
        public CustomTaskSettings Settings { get; set; }

        public float Progress { get; protected set; }

        public TaskStatus Status { get; protected set; }

        long ICustomTask.MillisecondRunTime => Settings.MillisecondRunTime;

        DateTime ICustomTask.Deadline => Settings.Deadline;

        int ICustomTask.UsableCores => Settings.UsableCores;

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
