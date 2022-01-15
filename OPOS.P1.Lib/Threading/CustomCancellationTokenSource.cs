using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPOS.P1.Lib.Threading
{
    public class CustomCancellationTokenSource
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationTokenSource PauseTokenSource { get; set; }
    }
}
