using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Algo
{
    public class FftTask
    {
        public FftTask()
        {

        }

        public FftTaskState State { get; set; }
    }

    public class FftTaskState : CustomTaskState
    {
        public List<FftTaskSubState> FftTaskSubStates { get; set; }
    }

    public class FftTaskSubState : CustomTaskState
    {
        public string InputFilePath { get; init; }
        public string OutputFilePath { get; init; }

    }

    public abstract class CustomTaskState
    {
        
    }

    public class CustomResource
    {
        public string Uri { get; init; }
    }
}
