using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using CefClient.Handler.Event;
using CefSharp;
using CefSharp.Handler;
using Newtonsoft.Json.Linq;

namespace CefClient.Handler
{
    public class CfxRequestHandler : RequestHandler
    {
        public event EventHandler<CfxRequestResultEventArgs> OnRequestResultHandler;
        public event EventHandler<string> OnLogEventHandler;
        public readonly JObject _args = null;
        public bool UseLocalCache = false;
        private bool useCacheImg = false;
        private bool useCacheVideo = false;
        private bool useCacheCss = false;
        private bool useCacheJS = false;
        public CfxRequestHandler(JObject args, EventHandler<string> logEventHandler)
        {
            this._args = args;
            if (logEventHandler != null)
                this.OnLogEventHandler += logEventHandler;
            if (this._args.SelectToken("useCacheImg") != null)
                this.useCacheImg = this._args.SelectToken("useCacheImg").Value<bool>();
            if (this._args.SelectToken("useCacheVideo") != null)
                this.useCacheVideo = this._args.SelectToken("useCacheVideo").Value<bool>();
            if (this._args.SelectToken("useCacheCss") != null)
                this.useCacheCss = this._args.SelectToken("useCacheCss").Value<bool>();
            if (this._args.SelectToken("useCacheJS") != null)
                this.useCacheJS = this._args.SelectToken("useCacheJS").Value<bool>();
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
 
            if (request.Url.StartsWith("devtools://"))
                return null;
            this.OnLogEventHandler?.Invoke(this, $"RequestUrl:{request.Url}");
            if (UseLocalCache && (
                (request.ResourceType == ResourceType.Image && this.useCacheImg) ||
                (request.ResourceType == ResourceType.Media && this.useCacheVideo) ||
                (request.ResourceType == ResourceType.Stylesheet && this.useCacheCss) ||
                  (request.ResourceType == ResourceType.FontResource && this.useCacheCss) ||
                (request.ResourceType == ResourceType.Script && this.useCacheJS)))
            {
                return new CfxResourceRequestHandler(this._args);
            }
            return new CfxDefaultResourceRequestHandler(this._args);
        }

    }
}
