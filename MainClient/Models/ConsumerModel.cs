using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MainClient.Models
{
    public class ConsumerModel
    {
        public string ProcessPath { get; set; }
        public int ProcessId { get; set; }
        public int ClientWindowHandle { get; set; }
        public DateTime time { get; set; }

        public int TaskCount;

        public int IncrementCount()
        {
            return Interlocked.Increment(ref TaskCount);
        }
    }
}
