using CefClient.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using Huichuan.Protocol;


namespace CefClient
{
    public partial class MainForm : Form
    {

        private SynchronizationContext sync;
        private int hMainWnd = 0;
        private bool isHiddenMode = true;
        private string clientId = string.Empty;
        private int taskCount = 0;


        #region  LogWrite

        public void LogWriteLine()
        {
            LogWrite(Environment.NewLine);
        }
        public void LogWriteLine(string msg)
        {
            LogWrite(msg + Environment.NewLine);
        }
        public void LogWriteLine(string msg, params object[] parameters)
        {
            LogWrite(msg + Environment.NewLine, parameters);
        }

        public void LogWrite(string msg, params object[] parameters)
        {
            LogWrite(string.Format(msg, parameters));
        }

        public void LogWrite(string msg)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => { LogWrite(msg); }));
                return;
            }
            LogTextBox.AppendText($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {msg}");
            LogTextBox.ScrollToCaret();
        }

        private void SendTaskMsgHandler(string message)
        {
            byte[] sarr = Encoding.Default.GetBytes(message);
            Win32.COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)CefProtocol.CopyDataId;
            cds.lpData = message;
            cds.cbData = sarr.Length + 1;
            const uint abortIfHung = 0x0002;
            Win32.User.SendMessageTimeout(
                this.hMainWnd,
                Win32.User.WM_COPYDATA,
                IntPtr.Zero,
                ref cds,
                abortIfHung,
                5000,
                out _);
        }
        private void OnTaskLogHandler(string message)
        {
            Task.Run(() =>
            {
#if DEBUG
                LogWriteLine(message);
#endif
                var data = CefProtocol.Serialize(
                    CefProtocol.Messages.TaskLog,
                    JObject.FromObject(new { Message = message }),
                    clientId);
                SendTaskMsgHandler(data);
            });

        }
        private void OnTaskDspHandler(int taskid, int type = 1, int count = 1)
        {
            var data = CefProtocol.Serialize(
                CefProtocol.Messages.TaskDsp,
                JObject.FromObject(new { TaskId = taskid, Type = type, Count = count }),
                clientId);
            SendTaskMsgHandler(data);
        }
        private void OnTaskCountHandler(int count)
        {
            var data = CefProtocol.Serialize(
                CefProtocol.Messages.TaskCount,
                JToken.FromObject(count),
                clientId);
            SendTaskMsgHandler(data);
        }


        #endregion

        private void ResolveMessage(string value)
        {
            Task.Run(() =>
            {
                if (!CefProtocol.TryParse(value, out var message, out var msgName))
                    return;
                if (msgName.Equals(CefProtocol.Messages.Load, StringComparison.Ordinal))
                {
                    var args = (JObject)JsonConvert.DeserializeObject(message["Data"].ToString());
                    var taskId = args.SelectToken("task.id").Value<int>();
                    OnTaskCountHandler(Interlocked.Increment(ref taskCount));
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        var form = new WebViewForm(args, (s, e) =>
                        {
                            OnTaskLogHandler(e);
                        })
                        {
                            Size = new Size(960, 1000),
                        };

                       // OnTaskLogHandler($"{Screen.PrimaryScreen.Bounds.Width}");

                        form.OnDspEventHandler += (s, e) =>
                        {
                            OnTaskDspHandler(taskId, 1, e);
                        };
                        form.OnDspClickEventHandler += (s, e) =>
                        {
                            OnTaskDspHandler(taskId, 2, e);
                        };
                        form.FormClosed += (s, arg) =>
                        {
                            OnTaskCountHandler(Interlocked.Decrement(ref taskCount));
                        };

                        form.Show();
                    }));
                }
                else if (msgName.Equals(CefProtocol.Messages.Stop, StringComparison.Ordinal))
                {
                    LogWriteLine("5秒后退出该进程");
                    _ = StopAsync();
                }
                else if (msgName.Equals(CefProtocol.Messages.Show, StringComparison.Ordinal))
                {
                    this.isHiddenMode = false;
                }
                else if (msgName.Equals(CefProtocol.Messages.Hide, StringComparison.Ordinal))
                {
                    this.isHiddenMode = true;
                }
            });

        }

        private async Task StopAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(Close);
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case Win32.User.WM_COPYDATA:
                    Win32.COPYDATASTRUCT data = new Win32.COPYDATASTRUCT();
                    Type myType = data.GetType();
                    data = (Win32.COPYDATASTRUCT)m.GetLParam(myType);
                    var value = data.lpData;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        ResolveMessage(value);
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            this.sync = SynchronizationContext.Current;
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            foreach (var c in commandLineArgs)
            {
                if (c.StartsWith("mainWnd="))
                {
                    this.hMainWnd = Convert.ToInt32(c.Split('=')[1]);
                }
                else if (c.StartsWith("isHiddenMode="))
                {
                    this.isHiddenMode = Convert.ToBoolean(c.Split('=')[1]);
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    SetVisibleCore(false);
                }
                else if (c.StartsWith("clientId="))
                {
                    this.clientId = c.Split('=')[1];
                }
            }
            SendRegMessage();
            LogWriteLine($"ProcessId={Process.GetCurrentProcess().Id},Handle={this.Handle},RootCachePath={CefCachePaths.RootCachePath},isHiddenMode={this.isHiddenMode}");
        }

        protected override void SetVisibleCore(bool value)
        {
#if DEBUG
            value = true;
#else
            //value = value;
#endif
            base.SetVisibleCore(value);
        }


        private void SendRegMessage()
        {
            var currentProcess = Process.GetCurrentProcess();
            var message = CefProtocol.Create(CefProtocol.Messages.Register);
            message["WindowHandle"] = (int)this.Handle;
            message["ClientId"] = this.clientId;
            message["ProcessId"] = currentProcess.Id;
            message["ProcessPath"] = currentProcess.MainModule?.FileName;
            SendTaskMsgHandler(message.ToString(Formatting.None));
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }




        private async Task<string> GetIp(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        private async Task<string> GetDev(string type = "android")
        {
            var client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://117.21.200.18:9000/api/getdev.php?type={type}&count=1&t={System.DateTime.Now.Ticks}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
        private async Task<string> GetTask(string taskName)
        {
            var client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://117.21.200.19/client-v5.php?type=1&action=getTask&task={taskName}&test=0&_t={System.DateTime.Now.Ticks}");
                response.EnsureSuccessStatusCode();
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }





        private int i = 0;
        private void buttonStart_Click(object sender, EventArgs e)
        {

            var primaryScreen = Screen.PrimaryScreen;
            int screenWidth = primaryScreen.Bounds.Width;
            int screenHeight = primaryScreen.Bounds.Height;

            Task.Run(async () =>
            {
                var vast = JProperty.Parse(Properties.Resources.js_vast);
                var task = ((JObject)JsonConvert.DeserializeObject(await GetTask("txtest")))["task"][0];
                var dev = ((JObject)JsonConvert.DeserializeObject(await GetDev("android")))["data"][0];
                var referer = task["referer"].ToString();
                var isProxyMode = false;
                string proxy_server = string.Empty;
                var realIp = string.Empty;
                if (isProxyMode)
                {
                    proxy_server = await GetIp("https://service.ipzan.com/core-extract?num=1&no=20250819576712695526&minute=3&format=txt&repeat=1&protocol=3&pool=quality&mode=whitelist&secret=c6ooub2f39339hg");
                    proxy_server = $"socks5://{proxy_server}";

                    var iptester = await new ProxyTester().TestAsync(proxy_server);
                    if (!iptester.IsValid)
                    {
                        LogWriteLine($"IP异常,{proxy_server}");
                        return;
                    }
                    var ipinfo = JObject.Parse(iptester.Data!);
                    if (ipinfo != null)
                        realIp = ipinfo["query"]?.Value<string>();
                }





                var args = new JObject();
                args["task"] = task;
                args["dev"] = dev;
                args["vast"] = vast;
                args["disableLoadImage"] = false;
                args["disableUserCache"] = false;
                args["isProxyMode"] = isProxyMode;
                args["isHiddenMode"] = false;
                args["proxy_server"] = proxy_server?.Trim();
                args["clickJump"] = false;
                args["cacheIndex"] = "1";
                args["url"] = null;
                args["referer"] = referer;
                args["os"] = 1;
                args["isShowLog"] = true;
                args["showDevTools"] = false;
                args["useCacheImg"] = false;
                args["useCacheVideo"] = false;
                args["useCacheCss"] = false;
                args["useCacheJS"] = false;
                args["clearDataForOrigin"] = "local_storage";// "cache_storage,cookies,local_storage";
                this.BeginInvoke(() =>
                {
                    var form = new WebViewForm(args, (s, e) =>
                    {
                        LogWriteLine(e);
                        //OnTaskLogHandler(e);
                    })
                    {
 
                        Size = new Size(960, 1000),
                    };
                    form.FormClosed += (s, arg) =>
                    {
                        LogWriteLine("FormClosed");
                        //OnTaskLogHandler("FormClosed");
                    };
                    form.Show();
                });
            });

        }
    }

}
