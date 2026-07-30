using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Event
{
    public class ReportDspEventArgs
    {
        public int Count { get; set; }
        public int Type { get; set; }
        public ReportDspEventArgs(int count = 1,int type = 1)
        {
            this.Count = count;
            this.Type = type;
        }
    }
}
