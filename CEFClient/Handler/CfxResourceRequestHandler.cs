using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CefClient.Handler.Event;
using CefSharp;
using CefSharp.Handler;
using CefSharp.ResponseFilter;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CefClient.Handler
{
    public class CfxResourceRequestHandler : ResourceRequestHandler
    {
        public readonly JObject _args = null;
        private string _localCacheFilePath = string.Empty;
        private bool IsLocalCacheFileExist => System.IO.File.Exists(_localCacheFilePath);
        public CfxResourceRequestHandler(JObject args)
        {
            this._args = args;
        }
        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {

            if (request.Url.StartsWith("http://192.168.") )
            {
                if(new int[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }[new Random(Guid.NewGuid().GetHashCode()).Next(0, 10)] == 1)
                {
                    return new Cfx2ResourceHandler();
                }
                return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);
            }

            if (!request.Url.StartsWith("devtools:"))
            {
                try
                {
                    _localCacheFilePath = CacheFileHelper.CalculateResourceFileName(request.Url, request.ResourceType);
                    if (string.IsNullOrWhiteSpace(_localCacheFilePath))
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
                if (!IsLocalCacheFileExist)
                {
                    return null;
                }
                return new CfxResourceHandler(_localCacheFilePath);
            }
            return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);

        }
        protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            if (!request.Url.StartsWith("devtools:"))
            {
                if (!IsLocalCacheFileExist)
                {
                    return new CfxResponseFilter() { LocalCacheFilePath = _localCacheFilePath };
                }
            }
            return null;
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