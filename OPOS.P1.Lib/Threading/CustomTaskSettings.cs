using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public class CustomTaskSettings
    {
        public long MillisecondRunTime { get; set; }

        public DateTime Deadline { get; set; }

        public int UsableCores { get; set; }

        public int Priority { get; set; }
    }
}
