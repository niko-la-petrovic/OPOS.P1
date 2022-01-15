using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public record CustomTaskSettings
    {
        public TimeSpan MaxRunDuration { get; init; } = TimeSpan.FromSeconds(3);

        public DateTime Deadline { get; init; } = DateTime.Now.AddSeconds(5);

        public int MaxCores { get; init; }

        public int Priority { get; init; }
    }
}
