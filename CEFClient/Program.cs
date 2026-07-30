using CefClient.Common;
using CefSharp;
using CefSharp.WinForms;
using System.Diagnostics;
using System.Text;

namespace CefClient
{
    public class Program
    {

        [STAThread]
        public static int Main(string[] args)
        {
            var consumerId = args
            .FirstOrDefault(x => x.StartsWith("--consumer-id=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("--consumer-id=".Length);

            if (!string.IsNullOrWhiteSpace(consumerId))
            {
                CefCachePaths.RootCachePath = CefCachePaths.GetConsumerRootCachePath(consumerId);
            }

            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                Debug.WriteLine("ThreadException");
                // TODO: 这里接你的日志
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Debug.WriteLine("UnhandledException");
                // TODO: 这里接你的日志
            };
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                // 如无必要，不建议这里做重日志
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved();
            };

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            Cef.EnableWaitForBrowsersToClose();
            var settings = new CefSettings
            {
                //RootCachePath = CefCachePaths.RootCachePath,
                //CachePath = CefCachePaths.RootCachePath,
                //RootCachePath = CefCachePaths.RootCachePath,
                //CachePath = string.Empty,
                PersistSessionCookies = false,
                //PersistUserPreferences = false,
                //WindowlessRenderingEnabled = false,
                IgnoreCertificateErrors = true,
                LogSeverity = LogSeverity.Disable,
                UserAgent = "Mozilla/5.0 (Linux; Android 13; SM-G981B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36",
            };
            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            //// WebRTC 代理相关
            //settings.CefCommandLineArgs.Add("force-webrtc-ip-handling-policy", "disable_non_proxied_udp");
            //settings.CefCommandLineArgs.Add("webrtc-ip-handling-policy", "disable_non_proxied_udp");
            //// 建议加：禁用 QUIC，代理环境更稳定
            //settings.CefCommandLineArgs.Add("disable-quic", "1");
            //// 可选：减少 DoH / SVCB 之类 DNS 行为干扰
            //settings.CefCommandLineArgs.Add("disable-features", "UseDnsHttpsSvcbAlpn");


            //--force-webrtc-ip-handling-policy --webrtc-ip-handling-policy=default  disable_non_proxied_udp
            //settings.CefCommandLineArgs.Add("plugin-policy", "allow");
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
            Application.ApplicationExit += (sender, e) =>
            {
                if (Cef.IsInitialized)
                {
                    Cef.WaitForBrowsersToClose();
                    Cef.Shutdown();
                }
            };
            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                try
                {
                    if (Cef.IsInitialized == true)
                    {
                        Cef.WaitForBrowsersToClose();
                        Cef.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Cef shutdown error: {ex}");
                }
            }
            return 0;
        }
    }
}