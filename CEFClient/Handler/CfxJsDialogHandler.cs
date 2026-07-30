using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class CfxJsDialogHandler : IJsDialogHandler
    {
        public bool OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
        {
            return true;
        }

        public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {

        }

        public bool OnJSDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            switch (dialogType)
            {
                case CefSharp.CefJsDialogType.Alert:
                    suppressMessage = true;
                    return false;
                case CefSharp.CefJsDialogType.Confirm:
                    callback.Continue(false, string.Empty);
                    suppressMessage = false;
                    return true;
                case CefSharp.CefJsDialogType.Prompt:
                    break;
                default:
                    break;
            }
            return false;
        }

        public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {

        }
    }
}
