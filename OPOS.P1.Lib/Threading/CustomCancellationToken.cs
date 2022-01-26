using System;
using System.Threading;

namespace OPOS.P1.Lib.Threading
{
    public class CustomCancellationToken
    {
        public CancellationToken CancellationToken { get; init; }
        public CancellationToken PauseToken { get; set; }

        public class OperationPausedException : OperationCanceledException
        {
        }

        public void ThrowIfPauseRequested()
        {
            if (PauseToken.IsCancellationRequested)
                throw new OperationPausedException();
        }

        public CustomCancellationToken(
            CancellationToken cancellationToken,
            CancellationToken pauseToken)
        {
            CancellationToken = cancellationToken;
            PauseToken = pauseToken;
        }

    }
}
