using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public class CustomCancellationToken
    {
        public CancellationToken CancellationToken { get; init; }
        public CancellationToken PauseToken { get; set; }

        public CustomCancellationToken(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

    }
}
