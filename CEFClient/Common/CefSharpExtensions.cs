using CefSharp;
using CefSharp.DevTools;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace CefClient.Common
{
    public static class CefSharpExtensions
    {
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
