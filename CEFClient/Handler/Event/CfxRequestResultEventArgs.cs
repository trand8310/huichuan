using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler.Event
{
    public class CfxRequestResultEventArgs : EventArgs
    {
        public string Url;
        public IWebBrowser WebBrowser;
        public IBrowser Browser;
        public IFrame Frame;
        public string Body;

        public CfxRequestResultEventArgs(string url, IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string data = null)
        {
            Url = url;
            WebBrowser = chromiumWebBrowser;
            Browser = browser;
            Frame = frame;
            Body = data;
        }
    }
}
