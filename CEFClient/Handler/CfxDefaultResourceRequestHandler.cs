using CefSharp;
using CefSharp.Handler;
using CefSharp.WinForms;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CefClient.Handler
{
    public class CfxDefaultResourceRequestHandler : ResourceRequestHandler
    {
        public readonly JObject _args = null;
        private string url = string.Empty;
        public CfxDefaultResourceRequestHandler(JObject args)
        {
            this._args = args;
            this.url = _args["url"].Value<string>();
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            // && new int[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 }[new Random(Guid.NewGuid().GetHashCode()).Next(0, 10)] == 1
            if (request.Url.StartsWith("http://192.168.") && new int[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0 }[new Random(Guid.NewGuid().GetHashCode()).Next(0, 10)] == 1)
            {
                return new Cfx2ResourceHandler();
            }
            return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);
        }
        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var ua = this._args.SelectToken("dev.ua").Value<string>();
            var headers = request.Headers;
            headers["User-Agent"] = ua;
            request.Headers = headers;
            return CefReturnValue.Continue;
        }
    }
}
