using System.Threading;

namespace OPOS.P1.Lib.Threading
{
    public class CustomCancellationTokenSource
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationTokenSource PauseTokenSource { get; set; }
    }
}
