using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{
    public enum DispatchTaskEventKind
    {
        Enqueued = 0,
        Dequeued = 1,
        Started = 2,
        Succeeded = 3,
        Failed = 4,
        Canceled = 5,
        Dropped = 6
    }
}
