using System;

namespace OPOS.P1.Lib.Threading
{
    public record CustomTaskSettings
    {
        public TimeSpan MaxRunDuration { get; init; } = TimeSpan.FromSeconds(3);

        public DateTime Deadline { get; init; } = DateTime.Now.AddSeconds(5);

        // TODO respect this
        public int MaxCores { get; init; }

        public int Priority { get; init; }
    }
}
