using System;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public interface ICustomTask
    {
        TimeSpan MaxRunDuration { get; }
        DateTime Deadline { get; }
        int UsableCores { get; }
        int Priority { get; }
        float Progress { get; }

        TaskStatus Status { get; }

        void Stop();
        void Pause();
        void Resume();
        
        void SaveState();
        void LoadState();
    }
}