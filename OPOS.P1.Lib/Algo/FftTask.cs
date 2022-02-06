using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace OPOS.P1.Lib.Algo
{
    public class FftTask : CustomTask
    {
        public FftTask(
            CustomTaskSettings customTaskSettings = null,
            ImmutableList<CustomResource> customResources = null,
            CustomScheduler scheduler = null)
            : base(
                  RunAction,
                  DefaultState,
                  customTaskSettings,
                  customResources,
                  scheduler)
        {
            Run = RunAction;
        }

        // TODO init
        public static FftTaskState DefaultState => new FftTaskState { };

        private static void RunAction(ICustomTaskState state, CustomCancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override CustomTask Deserialize(string json)
        {
            throw new NotImplementedException();
        }
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
