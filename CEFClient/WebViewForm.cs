using CefClient.Common;
using CefClient.Handler;
using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CefClient
{
    public partial class WebViewForm : Form
    {
        private string caption = "浏览器";
        private readonly JObject _args;
        private bool isHiddenMode = true;
        private bool isShowLog = false;

        #region  LogWrite

        public event EventHandler<string> OnLogEventHandler;
        public event EventHandler<int> OnDspEventHandler;
        public event EventHandler<int> OnDspClickEventHandler;

        private void DspChanged(int count = 1)
        {
            OnDspEventHandler?.Invoke(this, count);
        }
        private void DspClickChanged(int count = 1)
        {
            OnDspClickEventHandler?.Invoke(this, count);
        }
        private void LogWriteLine(string message)
        {
            if (this.isShowLog)
            {
                OnLogEventHandler?.Invoke(this, message);
            }
        }

        #endregion

        private Task<LoadUrlAsyncResponse> LoadPageAsync(IWebBrowser browser, string address = null, int timeout = 10)
        {
            return browser.LoadUrlAsync(address).TimeoutAfter(TimeSpan.FromSeconds(timeout));
        }
        static string url_macro_process(JToken ad, string url, int os, JToken dev, string ad_action = null, int turl_index = -1, string? urlname = null)
        {

            try
            {
                var stm = CommonHelper.UnixTimeNowSecond();
                url = url.Replace("{TS}", stm.ToString());
                if (!string.IsNullOrWhiteSpace(ad_action) && ad_action.Equals("download") && turl_index != -1)
                {
                    url += $"&hc_subid={turl_index}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return url;
        }


        static string url_macro_process_v2(JToken ad, string request_id, string sid, string slot_id, string url, int os, JToken dev, string ad_action = null, int turl_index = -1, string? urlname = null)
        {

            try
            {
                int dsp_bid_price = ad.SelectToken("ad_content.dsp_bid_price")?.Value<int>() ?? 0;
                var stm = CommonHelper.UnixTimeNowSecond();
                url = url.Replace("{TS}", stm.ToString());
                url = url.Replace("${AUCTION_ID}", request_id ?? "${AUCTION_ID}");
                url = url.Replace("${AUCTION_BID_ID}", sid ?? "${AUCTION_BID_ID}");
                url = url.Replace("${AUCTION_IMP_ID}", slot_id ?? "${AUCTION_IMP_ID}");
                url = url.Replace("${AUCTION_PRICE}", dsp_bid_price.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return url;
        }

        private static async Task<bool> SetRequestContextProxyAsync(
        IRequestContext requestContext,
        string proxyServer,
        Action<string>? log = null)
        {
            try
            {
                return await Cef.UIThreadTaskFactory.StartNew(() =>
                {
                    var proxy = new Dictionary<string, object>
                    {
                        ["mode"] = "fixed_servers",
                        ["server"] = proxyServer,
                    };
                    bool success = requestContext.SetPreference("proxy", proxy, out string error);
                    if (!success || !string.IsNullOrWhiteSpace(error))
                    {
                        log?.Invoke($"SetPreference proxy 失败: {error}");
                        return false;
                    }
                    //log?.Invoke($"SetPreference proxy 成功: {proxyServer}");
                    return true;
                });
            }
            catch (Exception ex)
            {
                log?.Invoke($"SetPreference proxy 异常: {ex}");
                return false;
            }
        }

        public WebViewForm(JObject args, EventHandler<string> logEventHandler)
        {
            InitializeComponent();
            this.OnLogEventHandler += logEventHandler;
            this._args = args;
            var uvIndex = _args.SelectToken("uv")?.Value<int>() ?? 0;
            this.caption = $"local_{uvIndex}:";
            var isProxyMode = _args.SelectToken("isProxyMode")?.Value<bool>() ?? false;
            var realip = _args.SelectToken("realip")?.Value<string>();
            var proxy_server = _args.SelectToken("proxy_server")?.Value<string>();
            if (isProxyMode && !string.IsNullOrWhiteSpace(proxy_server))
            {
                this.caption = $"proxy_{uvIndex}:realip={realip},{proxy_server}:";
            }
            var cacheIndex = _args.SelectToken("cacheIndex")?.Value<string>();
            var cachePath = System.IO.Path.Combine(CefCachePaths.RootCachePath, cacheIndex ?? "s00");
            this.isHiddenMode = _args.SelectToken("isHiddenMode")?.Value<bool>() ?? false;
            this.isShowLog = _args.SelectToken("isShowLog")?.Value<bool>() ?? false;
            var scaleX = _args.SelectToken("scaleX")?.Value<double>() ?? 1.0;
            var scaleY = _args.SelectToken("scaleY")?.Value<double>() ?? 1.0;
            var dev = _args.SelectToken("dev")?.Value<JObject>();
            var os = _args.SelectToken("os")?.Value<int>() ?? 1;
            var ua = _args.SelectToken("dev.ua")?.Value<string>();
            var model = _args.SelectToken("dev.model")?.Value<string>();
            int sw = _args.SelectToken("dev.sw")?.Value<int>() ?? 1080;
            int sh = _args.SelectToken("dev.sh")?.Value<int>() ?? 2400;
            var devProfile = DeviceViewportMatcher.Match(sw, sh, (os == 2 ? DeviceSystemType.IOS : DeviceSystemType.Android), model);
            var address_height = textBox_Address.Height;
            var browser_width = (int)Math.Ceiling(devProfile.ViewportWidth * scaleX);
            var browser_height = (int)Math.Ceiling(devProfile.ViewportHeight * scaleY);
            this.Width = browser_width + 16;
            this.Height = browser_height + address_height + 16;

            var browserSettings = new BrowserSettings()
            {

            };
            var requestContextSettings = new RequestContextSettings
            {
                CachePath = string.Empty,
                AcceptLanguageList = "zh-CN,zh;q=0.9",
            };
            var requestContext = new RequestContext(requestContextSettings);
            var browser = new ChromiumWebBrowser("about:blank", requestContext)
            {
                BrowserSettings = browserSettings,
                Size = new Size(browser_width, browser_height),
                Location = new Point(0, address_height),
                Dock = DockStyle.None,
            };

            if (!isHiddenMode)
            {
                browser.FrameLoadEnd += (sender, args) =>
                {
                    if (args.Frame.IsMain)
                    {
                        if (_args.SelectToken("showDevTools")?.Value<bool>() ?? false)
                        {
                            (sender as ChromiumWebBrowser).GetBrowserHost().ShowDevTools();
                        }
                    }
                };

                browser.TitleChanged += (s, args) =>
                {
                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        var title = args.Title;
                        if (string.IsNullOrWhiteSpace(title))
                            title = args.Browser.MainFrame.Url;
                        title = $"{this.caption}{title}";
                        this.Text = title;

                    });
                };

                browser.AddressChanged += (s, args) =>
                {
                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        this.textBox_Address.Text = args.Address;

                    });
                };
            }
            this.Controls.Add(browser);


            Task.Run(async () =>
            {
                if (isProxyMode && !string.IsNullOrWhiteSpace(proxy_server))
                {
                    await SetRequestContextProxyAsync(requestContext, proxy_server, message =>
                    {
                        LogWriteLine(message);
                    });
                }
                browser.DownloadHandler = new DisableDownloadHandler();
                browser.RequestHandler = new ExternalProtocolRequestHandler(message => LogWriteLine($"{message}"));
                browser.LifeSpanHandler = new CefLifeSpanHandler();
                browser.JsDialogHandler = new CefJsDialogHandler();
                await browser.WaitForInitialLoadAsync();
                LogWriteLine("浏览器初始化完成");

                try
                {
                    var task = _args.SelectToken("task")?.Value<JObject>()!;
                    var vast = _args.SelectToken("vast")?.Value<JObject>();
                    int pv = task.SelectToken("pv")?.Value<int>() ?? 1;
                    pv = pv == 0 ? 1 : pv;
                    #region sleep
                    int sleep = 0;
                    if (task.ContainsKey("sleep") && !string.IsNullOrWhiteSpace(task["sleep"].ToString()))
                    {
                        var text = task["sleep"].ToString();
                        try
                        {
                            if (text.Contains("-"))
                            {
                                var values = text.Split('-');
                                sleep = new Random().Next(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                            }
                            else
                            {
                                sleep = Convert.ToInt32(text);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        sleep = new Random().Next(4, 8);
                    }
                    #endregion

                    var pageLoadingTimeout = _args["pageLoadingTimeout"]?.Value<int>() ?? 10;
                    pageLoadingTimeout = pageLoadingTimeout == 0 ? 10 : pageLoadingTimeout;
                    var clickJump = _args.SelectToken("clickJump")?.Value<bool>() ?? false;

                    using (var devToolsClient = browser.GetDevToolsClient())
                    {
                        //var clearDataForOrigin = _args.SelectToken("clearDataForOrigin")?.Value<string>() ?? "cache_storage,cookies,local_storage";//"appcache,cache_storage,cookies,local_storage"
                        //await devToolsClient.Storage.ClearDataForOriginAsync("*", clearDataForOrigin);
                        if (os == 1 || os == 2)
                        {
                            await devToolsClient.Emulation.SetDeviceMetricsOverrideAsync(
                                width: devProfile.ViewportWidth,
                                height: devProfile.ViewportHeight,
                                deviceScaleFactor: devProfile.DeviceScaleFactor,
                                mobile: true,
                                scale: 1.0,
                                positionX: 0, positionY: 0,
                                dontSetVisibleSize: false,
                                screenOrientation: new CefSharp.DevTools.Emulation.ScreenOrientation()
                                {
                                    Type = CefSharp.DevTools.Emulation.ScreenOrientationType.PortraitPrimary,
                                    Angle = 0
                                });

                            await devToolsClient.Emulation.SetTouchEmulationEnabledAsync(true, 5);
                            if (os == 1)
                            {
                                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: ua, platform: "Android");
                            }
                            else
                            {
                                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: ua, platform: "iPhone");
                            }
                            await devToolsClient.Emulation.SetScrollbarsHiddenAsync(true);
                        }
                        else
                        {
                            await devToolsClient.Emulation.SetDeviceMetricsOverrideAsync(
                                width: devProfile.ViewportWidth,
                                height: devProfile.ViewportHeight,
                                deviceScaleFactor: devProfile.DeviceScaleFactor,
                                mobile: false,
                                scale: 1.0);
                        }
                        //url_macro_process



                        var request_id = vast.SelectToken("request_id").Value<string>();
                        var sid = vast.SelectToken("sid").Value<string>();
                        //LogWriteLine($"广告:{vast.ToString()}");
                        foreach (var slot_ad in vast.SelectToken("slot_ad"))
                        {
                            var slot_id = slot_ad.SelectToken("slot_id").Value<string>();
                            foreach (var ad in slot_ad.SelectToken("ad"))
                            {

                                var ad_action = ad.SelectToken("ad_action.action")?.Value<string>() ?? "";
                                //wnurl
                                #region 竞胜反馈打点wnurl
                                var wnurl = ad.SelectToken("wnurl")?.Value<string>();
                                if (!string.IsNullOrWhiteSpace(wnurl))
                                {
                                    var url = wnurl;
                                    url = url_macro_process_v2(ad, request_id, sid, slot_id, url, os, _args["dev"]);
                                    await LoadPageAsync(browser, url);
                                    LogWriteLine($"竞胜反馈[{task["id"]}]:{url}");
                                }
                                #endregion



                                #region 广告展示监控
                                var vurls = ad.SelectToken("vurl");
                                if (vurls != null)
                                {
                                    foreach (var vurl in vurls)
                                    {
                                        try
                                        {
                                            var url = vurl.Value<string>();
                                            url = url_macro_process(ad, url, os, _args["dev"]);
                                            await LoadPageAsync(browser, url);
                                            LogWriteLine($"广告展示监控[{task["id"]}]:{url}");

                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }

                                }
                                var end_vurl = ad.SelectToken("end_vurl");
                                if (end_vurl != null)
                                {
                                    try
                                    {
                                        var url = end_vurl.Value<string>();
                                        url = url_macro_process(ad, url, os, _args["dev"]);
                                        await LoadPageAsync(browser, url);
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                                DspChanged();
                                #endregion

                                if (clickJump)
                                {
                                    #region 广告点击监控
                                    var turl_index = -1;
                                    if (ad_action.Equals("download"))
                                    {
                                        var turls = ad.SelectToken("turl").Select(s => s.ToString()).ToArray(); ;
                                        turl_index = Random.Shared.Next(0, turls.Length);
                                    }


                                    var curls = ad.SelectToken("curl");

                                    if (curls != null)
                                    {
                                        foreach (var curl in curls)
                                        {
                                            try
                                            {
                                                var url = curl.Value<string>();
                                                url = url_macro_process(ad, url, os, _args["dev"], ad_action, turl_index);
                                                await LoadPageAsync(browser, url);
                                                LogWriteLine($"广告点击监控[{task["id"]}]:{url}");
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        DspClickChanged();
                                    }
                                    #endregion

                                }
                            }
                        }
                    }
                    LogWriteLine($"vast[{task["id"]}]:操作完成");
                    await TaskDelay(sleep, "关闭浏览器");
                    LogWriteLine($"vast[{task["id"]}]:任务结束");
                }
                catch (Exception ex)
                {
                    LogWriteLine($"任务异常:{ex.Message}");
                }
                finally
                {
                    TaskEnd();
                }
            });

            if (isHiddenMode)
            {
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private async Task TaskDelay(int interval, string text = "结束")
        {
            try
            {
                while (interval-- > 1 && this.IsHandleCreated)
                {
                    this.InvokeOnUiThreadIfRequired(() => { this.Text = $"{caption}{interval}秒后{text}."; });
                    await Task.Delay(1000);
                }
            }
            catch (Exception)
            {


            }
        }
        private void TaskEnd()
        {
            this.InvokeOnUiThreadIfRequired(() => { this.Close(); });
        }

        private void WebViewForm_Load(object sender, EventArgs e)
        {

        }
    }
}