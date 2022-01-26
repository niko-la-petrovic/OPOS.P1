using OPOS.P1.Lib.Threading;
using System.Collections.Generic;

namespace OPOS.P1.Lib.Algo
{
    public class FftTask
    {
        public FftTask()
        {

        }

        public FftTaskState State { get; set; }
    }

    public class FftTaskState : ICustomTaskState
    {
        public List<FftTaskSubState> FftTaskSubStates { get; set; }
    }

    public class FftTaskSubState : ICustomTaskState
    {
        public string InputFilePath { get; init; }
        public string OutputFilePath { get; init; }

    }
}
