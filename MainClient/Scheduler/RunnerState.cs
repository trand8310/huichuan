using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{
    public enum RunnerState
    {
        Stopped = 0,
        Running = 1,
        Stopping = 2,
        Faulted = 3
    }

}
