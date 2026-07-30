using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Event
{
    public class WebViewLogEventArgs : EventArgs
    {
        public string Data { get; set; }
        public WebViewLogEventArgs(string value)
        {
            Data = value;
        }
    }
}
