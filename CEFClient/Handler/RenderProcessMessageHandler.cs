using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        private readonly int _os;

        public RenderProcessMessageHandler(int os)
        {
            _os = os;
        }

        public void OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {

        }

        public void OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node)
        {

        }

        public void OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
        {

        }
        public void OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            StringBuilder js = new StringBuilder();
            js.AppendLine($"Object.defineProperty(navigator,'vendor',{{get:() => \"{(this._os == 1 ? "Google Inc." : "Apple Inc.")}\"}});");
            js.AppendLine($"Object.defineProperty(navigator,'platform',{{get:() => \"{(this._os == 1 ? "Android" : "iPhone")}\"}});");
            frame.ExecuteJavaScriptAsync(js.ToString());
        }
    }
}