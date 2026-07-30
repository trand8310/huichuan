using CefSharp;
using CefSharp.DevTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public static class CefSharpExtensions
    {

        /// <summary>
        /// 获取DOM在页面的绝对位置
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static async Task<CefSharp.Structs.Rect> GetElementRect(this IWebBrowser browser, string selector)
        {
            var js = @$"(() => {{ 
                let element = {selector};
                if(element) {{
                    let position = element.getBoundingClientRect();
                    return {{x : position.left,y:position.top,width:parseInt( element.offsetWidth),height:parseInt(element.offsetHeight)}};  
                }}
                return null;
            }})();";
            var response = await browser.GetMainFrame().EvaluateScriptAsync(js);
            if (response.Success && response.Result != null)
            {
                var result = ((ExpandoObject)response.Result).ToJson();
                return new CefSharp.Structs.Rect(result["x"].Value<int>(), result["y"].Value<int>(), result["width"].Value<int>(), result["height"].Value<int>());
            }
            return default(CefSharp.Structs.Rect);
        }




        public static Task<string> GetCookieText(this IWebBrowser browser, string url)
        {
            var requestContext = browser.GetBrowserHost().RequestContext;
            var cookieManager = requestContext.GetCookieManager(null);
            CookieVisitor _cookieVisitor = new CookieVisitor();
            cookieManager.VisitUrlCookies(url, true, _cookieVisitor);
            return _cookieVisitor.Task;
        }
        public static Task<string> GetAllCookieText(this IWebBrowser browser)
        {
            var requestContext = browser.GetBrowserHost().RequestContext;
            var cookieManager = requestContext.GetCookieManager(null);
            CookieVisitor _cookieVisitor = new CookieVisitor();
            cookieManager.VisitAllCookies(_cookieVisitor);
            return _cookieVisitor.Task;
        }



        /// <summary>
        /// 发送鼠标点击消息
        /// </summary>
        /// <param name="host"></param>
        /// <param name="pt"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        public static void SendMouseClickEvent(this IWebBrowser browser, Point pt, int rx = 0, int ry = 0)
        {
            int dx = pt.X + rx;
            int dy = pt.Y + ry;
            browser.GetBrowserHost().SendMouseClickEvent(dx, dy, MouseButtonType.Left, false, 1, CefEventFlags.None);
            System.Threading.Thread.Sleep(new Random().Next(20, 30));
            browser.GetBrowserHost().SendMouseClickEvent(dx, dy, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }
        /// <summary>
        /// 发送鼠标移动消息
        /// </summary>
        /// <param name="host"></param>
        /// <param name="pt"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        public static void SendMouseMoveEvent(this IWebBrowser browser, int rx = 0, int ry = 0)
        {
            browser.GetBrowserHost().SendMouseMoveEvent(rx, ry, false, new CefEventFlags());//移动鼠标
        }

        public static void SendMouseWheelEvent(this IWebBrowser browser, int x, int y, int deltaX, int deltaY)
        {
            browser.GetBrowserHost().SendMouseWheelEvent(x, y, deltaX, deltaY, CefEventFlags.None);
        }

        public static async Task SetScrollbarsHidden(this DevToolsClient cdpSession, bool hidden = true)
        {
            await cdpSession.ExecuteDevToolsMethodAsync("Emulation.setScrollbarsHidden", new Dictionary<string, object>() {
                {"hidden",hidden },
            });
        }

        public static async Task SetTouchEmulationEnabled(this DevToolsClient cdpSession, bool enabled = true, int maxTouchPoints = 1)
        {
            await cdpSession.ExecuteDevToolsMethodAsync("Emulation.setTouchEmulationEnabled", new Dictionary<string, object>() {
                {"enabled",enabled },
                {"maxTouchPoints",maxTouchPoints },
            });
        }

        public static async Task SetEmitTouchEventsForMouse(this DevToolsClient cdpSession, bool enabled = true, string configuration = "mobile")
        {
            await cdpSession.ExecuteDevToolsMethodAsync("Emulation.setEmitTouchEventsForMouse", new Dictionary<string, object>() {
                {"enabled",enabled },
                {"configuration",configuration},
            });
        }
    }
}
