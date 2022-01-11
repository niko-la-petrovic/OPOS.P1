using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public record CustomTaskSettings
    {
        public TimeSpan MaxRunDuration { get; init; }

        public DateTime Deadline { get; init; }

        public int MaxCores { get; init; }

        public int Priority { get; init; }
    }
}
