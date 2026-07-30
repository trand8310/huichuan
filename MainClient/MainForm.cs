using MainClient.Common;
using MainClient.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Win32;
using Huichuan.Protocol;

namespace MainClient
{
    public partial class MainForm : Form
    {
        private readonly ILogger _logger;
        private readonly IWritableOptions<AppSettings> _appSettings;
        private readonly DevHelper _devHelper = null;
        private readonly AdxHelper _adxHelper = null;
        private readonly IpHelper _ipHelper = null;
        private readonly ProxyTester _ipTester;
        private int mainWnd = 0;
        private CancellationTokenSource? cts;
        private readonly SemaphoreSlim runStateGate = new(1, 1);
        private Task? runTask;
        private SynchronizationContext sync;
        /// <summary>
        /// 标记应用程序是否重启
        /// </summary>
        private bool isRestart = false;
        private bool isRunning = false;
        private Stopwatch sw = new Stopwatch();

        #region 任务计数属性
        /// <summary>
        /// 任务数量:
        /// </summary>
        private int GetTaskCount = 0;
        /// <summary>
        /// 任务总量
        /// </summary>
        private int TotalGetTaskCount = 0;
        /// <summary>
        /// 请求数量
        /// </summary>
        private int RequestCount = 0;
        /// <summary>
        /// 请求总量
        /// </summary>
        private int TotalRequestCount = 0;
        /// <summary>
        /// 提交数量
        /// </summary>
        private int SuccessCount = 0;
        /// <summary>
        /// 提交总量
        /// </summary>
        private int TotalSuccessCount = 0;
        /// <summary>
        /// 曝光次数
        /// </summary>
        private int DspCount = 0;
        /// <summary>
        /// 曝光总量
        /// </summary>
        private int TotalDspCount = 0;

        /// <summary>
        /// 点击次数
        /// </summary>
        private int DspClickCount = 0;
        /// <summary>
        /// 点击总量
        /// </summary>
        private int TotalDspClickCount = 0;
        #endregion

        #region  LogWrite

        private ConcurrentQueue<string> logBuffer = new ConcurrentQueue<string>();
        private bool isProcessingLogs = false;
        private const int MaxBatchSize = 50;
        private void ProcessLogs()
        {
            isProcessingLogs = true;
            Task.Run(async () =>
            {
                var logsToProcess = new StringBuilder();
                while (isProcessingLogs)
                {
                    bool logsProcessed = false;
                    int logCount = 0;
                    while (logCount < MaxBatchSize && logBuffer.TryDequeue(out string logMessage))
                    {
                        logsToProcess.Append(logMessage);
                        logsProcessed = true;
                    }
                    if (logsProcessed)
                    {
                        WriteToLogs(logsToProcess.ToString());
                        logsToProcess.Clear();
                    }
                    await Task.Delay(1000);
                }

            });
        }
        private void WriteToLogs(string logMessage)
        {
            if (LogTextBox.InvokeRequired)
            {
                LogTextBox.Invoke((MethodInvoker)(() => { WriteToLogs(logMessage); }));
                return;
            }
            LogTextBox.AppendText(logMessage);
            LogTextBox.ScrollToCaret();
        }
        public void LogWriteLine(string logMessage)
        {
            logBuffer.Enqueue(($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {logMessage}{System.Environment.NewLine}"));
        }

        public void LogDetailInfo(string message)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => { LogDetailInfo(message); }));
                return;
            }
            LogDetailTextBox.AppendText($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {message}{Environment.NewLine}");
            LogDetailTextBox.ScrollToCaret();
        }



        #endregion

        #region 消息解析
        private void ResolveMessage(string value)
        {
            if (!CefProtocol.TryParse(value, out var message, out var msgName))
            {
                LogWriteLine("忽略无效的 CefClient 消息");
                return;
            }
            if (msgName.Equals(CefProtocol.Messages.Register, StringComparison.Ordinal))
            {
                var clientId = message["ClientId"].ToString();
                var windowHandle = Convert.ToInt32(message["WindowHandle"].ToString());
                if (this.processOfList.TryGetValue(clientId, out var client))
                {
                    this.processOfList.AddOrUpdate(clientId, client, (key, oldValue) =>
                    {
                        oldValue.ClientWindowHandle = windowHandle;
                        return oldValue;
                    });
                }
            }
            else if (msgName.Equals(CefProtocol.Messages.TaskCount, StringComparison.Ordinal))
            {
                var clientId = message["ClientId"].ToString();
                if (this.processOfList.TryGetValue(clientId, out var client))
                {
                    this.processOfList.AddOrUpdate(clientId, client, (key, oldValue) =>
                    {
                        oldValue.TaskCount = message["Data"].Value<int>();
                        return oldValue;
                    });
                }
            }
            else if (msgName.Equals(CefProtocol.Messages.TaskDsp, StringComparison.Ordinal))
            {
                var taskId = message.SelectToken("Data.TaskId").Value<int>();
                if (message.SelectToken("Data.Type").Value<int>() == 2)
                {
                    _adxHelper.UpdateTaskDspClick(taskId, 1);
                    Interlocked.Increment(ref this.TotalDspClickCount);
                    Interlocked.Increment(ref this.DspClickCount);
                }
                else
                {
                    _adxHelper.UpdateTaskDsp(taskId, 1);
                    Interlocked.Increment(ref this.DspCount);
                    Interlocked.Increment(ref this.TotalDspCount);
                }
            }
            else if (msgName.Equals(CefProtocol.Messages.TaskLog, StringComparison.Ordinal))
            {
                if (_appSettings.Value.IsDetailLog)
                {
                    LogDetailInfo(message.SelectToken("Data.Message").Value<string>());
                }

            }
        }

        private static void SendCefLoadMessage(ConsumerModel consumer, JObject args)
        {
            var message = CefProtocol.Serialize(CefProtocol.Messages.Load, args);
            byte[] buffer = Encoding.Default.GetBytes(message);
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)CefProtocol.CopyDataId;
            cds.lpData = message;
            cds.cbData = buffer.Length + 1;
            const uint abortIfHung = 0x0002;
            var sent = NativeMethod.SendMessageTimeout(
                consumer.ClientWindowHandle,
                NativeMethod.WM_COPYDATA,
                IntPtr.Zero,
                ref cds,
                abortIfHung,
                5000,
                out _);
            if (sent == IntPtr.Zero)
                throw new TimeoutException($"向 CefClient 窗口 {consumer.ClientWindowHandle} 发送消息失败或超时。");
        }


        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case NativeMethod.WM_COPYDATA:
                    COPYDATASTRUCT data = new COPYDATASTRUCT();
                    Type myType = data.GetType();
                    data = (COPYDATASTRUCT)m.GetLParam(myType);
                    if (!string.IsNullOrWhiteSpace(data.lpData))
                    {
                        Task.Run(() => ResolveMessage(data.lpData));
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion



        public MainForm(
            DevHelper devHelper,
            AdxHelper adxHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            IWritableOptions<AppSettings> appSettings,
            ILogger<MainForm> logger)
        {
            InitializeComponent();
            this._devHelper = devHelper;
            this._adxHelper = adxHelper;
            this._ipHelper = ipHelper;
            this._ipTester = ipTester;
            this._appSettings = appSettings;
            this._logger = logger;
            FormClosing += MainForm_FormClosing;
            this.Text += $"{AppConsts.AppVertion}";
            this.sync = SynchronizationContext.Current;
            LoadAppSetting();
            #region 控件初始化
            var controls = new List<Control>() { groupBox2, groupBox5, groupBox6 };
            foreach (var control in controls)
            {
                foreach (var c in control.Controls)
                {
                    if (c is NumericUpDown)
                    {
                        (c as NumericUpDown).ValueChanged += (s, e) =>
                        {
                            UpdateAppSetting();
                        };
                    }
                    else if (c is TextBox)
                    {
                        (c as TextBox).TextChanged += (s, e) =>
                        {
                            UpdateAppSetting();
                        };
                    }
                    else if (c is CheckBox)
                    {
                        (c as CheckBox).Click += (s, e) =>
                        {
                            UpdateAppSetting();
                        };
                    }
                    else if (c is RadioButton)
                    {
                        (c as RadioButton).Click += (s, e) =>
                        {
                            UpdateAppSetting();
                        };
                    }
                }
            }
            #endregion

            #region 数据初始化
            this.textBox_SmsName.Text = CommonHelper.GetHostName();
            this._appSettings.Update(opt => opt.SmsName = CommonHelper.GetHostName());
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                toolStripStatusLabel1.Text = $"CPU:{item["NumberOfLogicalProcessors"]}";
            }
            #endregion
        }
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            isRunning = false;
            isProcessingLogs = false;
            cts?.Cancel();

            foreach (var consumer in processOfList.Values)
            {
                try
                {
                    using var process = Process.GetProcessById(consumer.ProcessId);
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    _logger.LogWarning(ex, "关闭窗口时终止 CefClient {ProcessId} 失败", consumer.ProcessId);
                }
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            var isRestart = System.Environment.GetCommandLineArgs().Any(p => p.StartsWith("restart"));
            if (isRestart)
            {
                LoadAppState();
                sync.Post((p) =>
                {
                    buttonStart.PerformClick();
                }, null);
            }


        }

        private void AddTaskInfo(JToken tasks)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                this.taskInfoListView.BeginUpdate();
                this.taskInfoListView.Items.Clear();
                try
                {
                    foreach (var task in tasks)
                    {
                        ListViewItem lvi = new ListViewItem();
                        lvi.Tag = task["id"].ToString();
                        lvi.Text = $"{task["type"].ToString()}-{task["title"].ToString()}";
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        this.taskInfoListView.Items.Add(lvi);
                    }
                }
                finally
                {
                    this.taskInfoListView.EndUpdate();
                }

            }));
        }

        #region 应用设置
        private void LoadAppSetting()
        {

            textBox_ProxyIpUrl.Text = _appSettings.Value.ProxyIpUrl;
            textBox_TaskApiUrl.Text = _appSettings.Value.TaskApiUrl;
            textBox_DevApiUrl.Text = _appSettings.Value.DevApiUrl;
            numericUpDown_FetchTaskInterval.Value = _appSettings.Value.FetchTaskInterval;
            numericUpDown_UVInterval.Value = _appSettings.Value.UVInterval;
            numericUpDown_MaximumConcurrency.Value = _appSettings.Value.MaximumConcurrency;
            numericUpDown_MaximumCacheCount.Value = _appSettings.Value.MaximumCacheCount;
            numericUpDown_PageLoadingTimeout.Value = _appSettings.Value.PageLoadingTimeout;
            textBox_TaskName.Text = _appSettings.Value.TaskName;
            numericUpDown_Multiple.Value = _appSettings.Value.Multiple;
            numericUpDown_MainResetTimeout.Value = _appSettings.Value.MainResetTimeout;
            numericUpDown_SubResetTimeout.Value = _appSettings.Value.SubResetTimeout;
            checkBox_IsHiddenMode.Checked = _appSettings.Value.IsHiddenMode;
            checkBox_IsProxyMode.Checked = _appSettings.Value.IsProxyMode;
            checkBox_IsRealIp.Checked = _appSettings.Value.IsRealIp;
            checkBox_IsCheckIp.Checked = _appSettings.Value.IsCheckIp;
            checkBox_DisableUserCache.Checked = _appSettings.Value.DisableUserCache;
            checkBox_DisableLoadImage.Checked = _appSettings.Value.DisableLoadImage;
            checkBox_UseCacheImg.Checked = _appSettings.Value.UseCacheImg;
            checkBox_UseCacheVideo.Checked = _appSettings.Value.UseCacheVideo;
            checkBox_UseCacheCss.Checked = _appSettings.Value.UseCacheCss;
            checkBox_UseCacheJS.Checked = _appSettings.Value.UseCacheJS;
            checkBox_SendSms.Checked = _appSettings.Value.SendSms;
            textBox_SmsName.Text = _appSettings.Value.SmsName;
            textBox_SmsPhone.Text = _appSettings.Value.SmsPhone;
            numericUpDown_SendSmsTimeout.Value = _appSettings.Value.SendSmsTimeout;
            var usingDevIndex = _appSettings.Value.UsingDevIndex;
            if (usingDevIndex == 2)
                radioButton_UsingRealDev.Checked = true;
            else if (usingDevIndex == 3)
                radioButton_UseLocalDev.Checked = true;
            else
                radioButton_UseSystemDev.Checked = true;
            checkBox_IsDetailLog.Checked = _appSettings.Value.IsDetailLog;

            numericUpDown_IpTtl.Value = _appSettings.Value.IpTtl;
            numericUpDown_DspBidPrice.Value = _appSettings.Value.DspBidPrice;

            checkBox_UVsTriggerOne.Checked = _appSettings.Value.UVsTriggerOne;
            checkBox_PersistAdx.Checked = _appSettings.Value.PersistAdx;
        }
        private void UpdateAppSetting()
        {
            _appSettings.Update(opt =>
            {
                opt.ProxyIpUrl = textBox_ProxyIpUrl.Text;
                opt.TaskApiUrl = textBox_TaskApiUrl.Text;
                opt.DevApiUrl = textBox_DevApiUrl.Text;
                opt.FetchTaskInterval = (int)numericUpDown_FetchTaskInterval.Value;
                opt.UVInterval = (int)numericUpDown_UVInterval.Value;
                opt.MaximumConcurrency = (int)numericUpDown_MaximumConcurrency.Value;
                opt.MaximumCacheCount = (int)numericUpDown_MaximumCacheCount.Value;
                opt.PageLoadingTimeout = (int)numericUpDown_PageLoadingTimeout.Value;
                opt.TaskName = textBox_TaskName.Text;
                opt.Multiple = (int)numericUpDown_Multiple.Value;
                opt.MainResetTimeout = (int)numericUpDown_MainResetTimeout.Value;
                opt.SubResetTimeout = (int)numericUpDown_SubResetTimeout.Value;
                opt.IsHiddenMode = checkBox_IsHiddenMode.Checked;
                opt.IsProxyMode = checkBox_IsProxyMode.Checked;
                opt.IsRealIp = checkBox_IsRealIp.Checked;
                opt.IsCheckIp = checkBox_IsCheckIp.Checked;
                opt.DisableUserCache = checkBox_DisableUserCache.Checked;
                opt.DisableLoadImage = checkBox_DisableLoadImage.Checked;
                opt.UseCacheImg = checkBox_UseCacheImg.Checked;
                opt.UseCacheVideo = checkBox_UseCacheVideo.Checked;
                opt.UseCacheCss = checkBox_UseCacheCss.Checked;
                opt.UseCacheJS = checkBox_UseCacheJS.Checked;
                opt.SendSms = checkBox_SendSms.Checked;
                opt.SmsName = textBox_SmsName.Text;
                opt.SmsPhone = textBox_SmsPhone.Text;
                opt.SendSmsTimeout = (int)numericUpDown_SendSmsTimeout.Value;
                if (radioButton_UsingRealDev.Checked)
                    opt.UsingDevIndex = 2;
                else if (radioButton_UseLocalDev.Checked)
                    opt.UsingDevIndex = 3;
                else
                    opt.UsingDevIndex = 1;
                opt.IsDetailLog = checkBox_IsDetailLog.Checked;


                opt.IpTtl = (int)numericUpDown_IpTtl.Value;
                opt.DspBidPrice = (int)numericUpDown_DspBidPrice.Value;
                opt.UVsTriggerOne = checkBox_UVsTriggerOne.Checked;
                opt.PersistAdx = checkBox_PersistAdx.Checked;
            });
        }
        #endregion

        private readonly ConcurrentDictionary<string, ConsumerModel> processOfList = new();



        /// <summary>
        /// 更新任务状态信息
        /// </summary>
        private void UpdateStatInfo()
        {

            this.BeginInvoke(new Action(() =>
            {
                label5.Text = $"请求数量:{this.RequestCount}";
                if (this.RequestCount > 0)
                    label6.Text = $"提交数量:{this.SuccessCount},{(this.SuccessCount / (double)this.RequestCount * 100):N1}%";

                label7.Text = $"曝光数量:{this.DspCount}";
                label8.Text = $"点击数量:{this.DspClickCount}";
                toolStripStatusLabel2.Text = $"进程：{this.processOfList.Count()}";
                toolStripStatusLabel3.Text = $"请求总量：{this.TotalRequestCount}";
                toolStripStatusLabel4.Text = $"提交总量：{this.TotalSuccessCount}";
                toolStripStatusLabel5.Text = $"曝光总量：{this.TotalDspCount}";
                toolStripStatusLabel6.Text = $"点击总量：{this.TotalDspClickCount}";
                if (sw.IsRunning)
                {
                    label9.Text = $"运行时间:{sw.Elapsed.Minutes}分{sw.Elapsed.Seconds}秒";
                }
            }));
        }

        /// <summary>
        /// 保存运行状态
        /// </summary>
        /// <returns></returns>
        private void LoadAppState()
        {
            var runDatPath = @"Logs/run_" + System.DateTime.Today.ToString("yyyyMMdd") + "_" + _appSettings.Value.TaskName + ".dat";
            if (System.IO.File.Exists(runDatPath))
            {
                var content = System.IO.File.ReadAllLines(runDatPath).LastOrDefault();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(content);
                    if (jo["Task"].ToString().Equals(_appSettings.Value.TaskName))
                    {
                        this.TotalDspCount = Convert.ToInt32(jo["TotalDspCount"].ToString());
                        if (jo.ContainsKey("TotalDspClickCount"))
                        {
                            this.TotalDspClickCount = Convert.ToInt32(jo["TotalDspClickCount"].ToString());
                        }
                        if (jo.ContainsKey("TotalRequestCount"))
                        {
                            this.TotalRequestCount = Convert.ToInt32(jo["TotalRequestCount"].ToString());
                        }
                        if (jo.ContainsKey("TotalSuccessCount"))
                        {
                            this.TotalSuccessCount = Convert.ToInt32(jo["TotalSuccessCount"].ToString());
                        }


                    }
                }
            }
        }
        /// <summary>
        /// 保存运行状态
        /// </summary>
        /// <returns></returns>
        private async Task SaveAppState()
        {
            Directory.CreateDirectory("./Logs");
            var rundatFile = @"./Logs/run_" + System.DateTime.Today.ToString("yyyyMMdd") + "_" + _appSettings.Value.TaskName + ".dat";
            var runData = JObject.FromObject(new
            {
                Task = _appSettings.Value.TaskName,
                GetTaskCount,
                TotalGetTaskCount,
                RequestCount,
                TotalRequestCount,
                SuccessCount,
                TotalSuccessCount,
                DspCount,
                TotalDspCount,
                DspClickCount,
                TotalDspClickCount,
                LastDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
            if (!System.IO.File.Exists(rundatFile))
            {
                runData = JObject.FromObject(new
                {
                    Task = _appSettings.Value.TaskName,
                    GetTaskCount,
                    TotalGetTaskCount = GetTaskCount,
                    RequestCount,
                    TotalRequestCount = RequestCount,
                    SuccessCount,
                    TotalSuccessCount = SuccessCount,
                    DspCount,
                    TotalDspCount = DspCount,
                    DspClickCount,
                    TotalDspClickCount = DspClickCount,
                    LastDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                await System.IO.File.WriteAllTextAsync(rundatFile, $"{JsonConvert.SerializeObject(runData, Newtonsoft.Json.Formatting.None)}{System.Environment.NewLine}");
            }
            else
                await System.IO.File.AppendAllTextAsync(rundatFile, $"{JsonConvert.SerializeObject(runData, Newtonsoft.Json.Formatting.None)}{System.Environment.NewLine}");
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            await runStateGate.WaitAsync();
            try
            {
                if (runTask is { IsCompleted: false })
                {
                    await RequestStopAsync(restart: false);
                    return;
                }

                await StartRunAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换运行状态失败");
                LogWriteLine($"切换运行状态失败：{ex}");
                SetRunUi(false);
            }
            finally
            {
                runStateGate.Release();
            }
        }

        private Task StartRunAsync()
        {
            if (!File.Exists(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "CefClient", "CefClient.exe")))
            {
                MessageBox.Show("CefClient.exe不存在!");
                return Task.CompletedTask;
            }

            var concurrency = Math.Max(1, _appSettings.Value.MaximumConcurrency);
            var capacity = Math.Max(concurrency, _appSettings.Value.MaximumCacheCount);
            ProcessLogs();
            isRestart = false;
            isRunning = true;
            CommonHelper.ClearAllErrorMsgDialog();
            this.GetTaskCount = 0;
            this.RequestCount = 0;
            this.SuccessCount = 0;
            this.DspCount = 0;
            this.DspClickCount = 0;
            this.mainWnd = (int)this.Handle;
            this.processOfList.Clear();
            sw.Reset();
            sw.Start();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            SetRunUi(true);
            runTask = RunSessionAsync(concurrency, capacity, cts.Token);
            return Task.CompletedTask;
        }

        private async Task RequestStopAsync(bool restart)
        {
            isRestart = restart;
            isRunning = false;
            isProcessingLogs = false;
            buttonStart.Enabled = false;
            buttonStart.Text = restart ? "重启中..." : "停止中...";
            buttonStart.ForeColor = Color.Black;
            cts?.Cancel();

            var completion = runTask;
            if (completion is not null)
            {
                await completion;
            }
        }

        private async Task RunSessionAsync(int concurrency, int capacity, CancellationToken token)
        {
            var channel = Channel.CreateBounded<JObject>(
                    new BoundedChannelOptions(capacity)
                    {
                        SingleWriter = true,
                        SingleReader = false,
                        FullMode = BoundedChannelFullMode.Wait
                    });

            try
            {
                var tasks = new List<Task>(concurrency + 3)
                {
                    ProduceWithWhileAndTryWrite(channel.Writer, token),
                    RefreshStatisticsAsync(token),
                    MonitorRunLifetimeAsync(token)
                };
                for (var index = 1; index <= concurrency; index++)
                {
                    tasks.Add(ConsumerAsync(channel.Reader, index, token));
                }

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 用户停止和计划重启均为正常控制流。
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "运行会话异常退出");
                LogWriteLine($"运行会话异常退出：{ex}");
            }
            finally
            {
                isRunning = false;
                isProcessingLogs = false;
                sw.Stop();
                try
                {
                    await _adxHelper.UpdateTaskStat();
                    await SaveAppState();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存运行状态失败");
                    LogWriteLine($"保存运行状态失败：{ex.Message}");
                }
                if (isRestart)
                {
                    sync.Post((p) =>
                    {
                        CommonHelper.ProcessRestart();
                    }, null);
                }
                else
                {
                    if (!IsDisposed && !Disposing)
                    {
                        this.sync.Post(p => SetRunUi(false), null);
                    }
                }
            }
        }

        private async Task RefreshStatisticsAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var ticks = 0;
            while (await timer.WaitForNextTickAsync(token))
            {
                UpdateStatInfo();
                if (++ticks % 5 == 0)
                    await _adxHelper.UpdateTaskStat();
            }
        }

        private async Task MonitorRunLifetimeAsync(CancellationToken token)
        {
            var minutes = _appSettings.Value.MainResetTimeout;
            if (minutes <= 0)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return;
            }

            var seconds = Math.Max(1, (long)minutes * 60 + Random.Shared.Next(-5, 6));
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);
            LogWriteLine("达到主进程重启周期，准备重启任务");
            isRestart = true;
            isRunning = false;
            isProcessingLogs = false;
            cts?.Cancel();
        }

        private void SetRunUi(bool running)
        {
            buttonClear.Enabled = !running;
            buttonStart.Enabled = true;
            buttonStart.Text = running ? "停止" : "开始";
            buttonStart.ForeColor = running ? Color.Blue : Color.Black;
        }
        private void buttonClear_Click(object sender, EventArgs e)
        {

            buttonClear.Enabled = false;
            buttonStart.Enabled = false;
            Task.Factory.StartNew(() =>
            {
                CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //foreach (Process process in Process.GetProcesses())
                //{
                //    try
                //    {
                //        NativeMethod.EmptyWorkingSet(process.Handle);
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                ////try
                ////{
                ////   // Directory.Delete(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data"), recursive: true);
                ////}
                ////catch (Exception ex)
                ////{
                ////    Debug.Write(ex.Message);
                ////}
            }).ContinueWith(t =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    buttonStart.Enabled = true;
                    buttonClear.Enabled = true;
                }));

            });
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.IO.DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            foreach (System.IO.FileInfo file in dir.GetFiles())
                file.Delete();
            Process.Start(new ProcessStartInfo { FileName = Environment.GetFolderPath(Environment.SpecialFolder.Startup), UseShellExecute = true });
            CommonHelper.CreateShortcut("曝光服务");
        }




        public async Task ProduceWithWhileAndTryWrite(ChannelWriter<JObject> writer, CancellationToken token)
        {
            try
            {
                while (this.isRunning && !this.isRestart && !token.IsCancellationRequested)
                {
                    try
                    {
                        var content = await this._adxHelper.GetTaskAsync(
                            $"{_appSettings.Value.TaskApiUrl}?type=1&action=getTask&task={_appSettings.Value.TaskName}&test=0&_t={System.DateTime.Now.Ticks}",
                            token);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            if (content.Equals("empty"))
                            {
                                sync.Post((p) =>
                                {
                                    this.taskInfoListView.Items.Clear();
                                }, null);
                                LogWriteLine($"共取到[0]条任务");
                            }
                            else
                            {
                                var tasks = (JObject)JsonConvert.DeserializeObject(content);
                                int taskCount = tasks["task"].Count();
                                if (taskCount > 0)
                                {
                                    AddTaskInfo(tasks["task"]);
                                    LogWriteLine($"新增加{tasks["task"].Count()}条任务");
                                    for (int i = 0; i < _appSettings.Value.Multiple; i++)
                                    {
                                        if (!this.isRunning || this.isRestart || token.IsCancellationRequested)
                                        {
                                            break;
                                        }

                                        foreach (JObject task in tasks["task"])
                                        {
                                            await writer.WriteAsync((JObject)task.DeepClone(), token);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取或分发任务失败");
                        LogWriteLine($"获取或分发任务失败：{ex.Message}");
                    }
                    await Task.Delay(_appSettings.Value.FetchTaskInterval, token);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                writer.TryComplete();
            }
        }

        static SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private async Task SaveAdx(JObject adx, int type = 1)
        {
            await _mutex.WaitAsync();
            try
            {
                Directory.CreateDirectory("./adx");
                await System.IO.File.AppendAllTextAsync($"./adx/adx_{type}.json", $"{JsonConvert.SerializeObject(adx, Formatting.None)}{System.Environment.NewLine}");

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private sealed record ClientRuntime(
            string ClientId,
            string ExecutablePath,
            Process Process,
            ConsumerModel Consumer,
            DateTime StartedUtc);

        private sealed record ParsedTask(
            int TaskId,
            string Title,
            int TotalUv,
            int ClickRate,
            string DeviceClientId,
            string Url,
            JObject AdParam,
            JObject RawTask);

        private sealed record NetworkContext(
            string ProxyServer,
            string RealIp,
            JObject IpInfo);

        /// <summary>
        /// 安全消费 Channel 中的任务。
        /// 主要职责仅保留：读取任务、确保子进程存在、处理任务、回收子进程。
        /// </summary>
        public async Task ConsumerAsync(
            ChannelReader<JObject> reader,
            int processIndex,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(reader);

            ClientRuntime? runtime = null;
            var processLifetime = GetProcessLifetime();

            try
            {
                await foreach (var task in reader.ReadAllAsync(token))
                {
                    try
                    {
                        runtime = await EnsureClientRuntimeAsync(
                            runtime,
                            processIndex,
                            token);

                        if (runtime is null)
                        {
                            LogWriteLine($"消费者[{processIndex}]无法启动 CefClient，跳过任务。");
                            continue;
                        }

                        await ProcessOneTaskAsync(
                            task,
                            runtime,
                            processIndex,
                            processLifetime,
                            token);

                        if (HasExceededLifetime(runtime, processLifetime))
                        {
                            await StopClientRuntimeAsync(
                                runtime,
                                $"运行时间超过 {processLifetime}");

                            runtime = null;
                        }
                        else if (!IsProcessAlive(runtime.Process))
                        {
                            await StopClientRuntimeAsync(runtime, "子进程已经退出");
                            runtime = null;
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 不要只记录 ex.Message，否则会丢失堆栈和内部异常。
                        LogWriteLine($"消费者[{processIndex}]处理任务异常：{ex}");

                        if (runtime is not null && !IsProcessAlive(runtime.Process))
                        {
                            await StopClientRuntimeAsync(runtime, "异常后发现子进程不可用");
                            runtime = null;
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 正常取消。
            }
            finally
            {
                if (runtime is not null)
                {
                    await StopClientRuntimeAsync(runtime, "消费者退出");
                }
            }
        }

        private async Task<ClientRuntime?> EnsureClientRuntimeAsync(
            ClientRuntime? current,
            int processIndex,
            CancellationToken token)
        {
            if (current is not null &&
                IsProcessAlive(current.Process) &&
                current.Consumer.ClientWindowHandle != 0)
            {
                return current;
            }

            if (current is not null)
            {
                await StopClientRuntimeAsync(current, "重新创建客户端");
            }

            const int maxStartAttempts = 3;

            for (var attempt = 1; attempt <= maxStartAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                Process? process = null;
                string? clientId = null;

                try
                {
                    var executablePath = GetSharedClientExecutable();

                    clientId = Guid.NewGuid().ToString("N");

                    var consumer = new ConsumerModel
                    {
                        ProcessId = 0,
                        ClientWindowHandle = 0,
                        ProcessPath = executablePath,
                        time = DateTime.Now
                    };

                    if (!processOfList.TryAdd(clientId, consumer))
                    {
                        throw new InvalidOperationException(
                            $"无法注册客户端状态：{clientId}");
                    }

                    var startInfo = BuildClientStartInfo(
                        executablePath,
                        clientId,
                        processIndex);

                    process = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    process.Exited += (_, _) =>
                    {
                        processOfList.TryRemove(clientId, out _);
                        LogDetailInfo(
                            $"退出进程：clientId={clientId}, path={executablePath}");
                    };

                    if (!process.Start())
                    {
                        throw new InvalidOperationException(
                            $"Process.Start 返回 false：{executablePath}");
                    }

                    consumer.ProcessId = process.Id;

                    // 防止极短命进程在注册前后退出，留下失效 consumer。
                    if (!IsProcessAlive(process))
                    {
                        throw new InvalidOperationException(
                            $"CefClient 启动后立即退出，PID={process.Id}");
                    }

                    var runtime = new ClientRuntime(
                        clientId,
                        executablePath,
                        process,
                        consumer,
                        DateTime.UtcNow);

                    var ready = await WaitForClientReadyAsync(
                        runtime,
                        TimeSpan.FromSeconds(30),
                        token);

                    if (!ready)
                    {
                        await StopClientRuntimeAsync(runtime, "等待窗口句柄超时");
                        process = null; // 已由 StopClientRuntimeAsync Dispose。
                        clientId = null;

                        throw new TimeoutException(
                            $"CefClient[{processIndex}] 30 秒内未完成初始化。");
                    }

                    LogDetailInfo(
                        $"创建进程完成：PID={process.Id}, path={executablePath}");

                    await Task.Delay(
                        Random.Shared.Next(500, 1001),
                        token);

                    return runtime;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    if (clientId is not null)
                    {
                        processOfList.TryRemove(clientId, out _);
                    }

                    TryKillAndDispose(process);
                    throw;
                }
                catch (Exception ex)
                {
                    if (clientId is not null)
                    {
                        processOfList.TryRemove(clientId, out _);
                    }

                    TryKillAndDispose(process);

                    LogWriteLine(
                        $"启动 CefClient[{processIndex}] 失败，" +
                        $"attempt={attempt}/{maxStartAttempts}：{ex}");

                    if (attempt < maxStartAttempts)
                    {
                        await Task.Delay(
                            GetRetryDelay(attempt, ex),
                            token);
                    }
                }
            }

            return null;
        }

        private ProcessStartInfo BuildClientStartInfo(
            string executablePath,
            string clientId,
            int processIndex)
        {
            var workingDirectory = Path.GetDirectoryName(executablePath)
                ?? throw new InvalidOperationException("无法取得 CefClient 工作目录。");
            var runtimeRoot = Path.Combine(
                AppContext.BaseDirectory,
                "chrome",
                "instances",
                $"consumer-{processIndex}");

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            // 使用 ArgumentList，避免手工拼接参数时的引号和转义问题。
            startInfo.ArgumentList.Add($"mainWnd={mainWnd}");
            startInfo.ArgumentList.Add(
                $"isHiddenMode={_appSettings.Value.IsHiddenMode}");
            startInfo.ArgumentList.Add($"clientId={clientId}");
            startInfo.ArgumentList.Add($"--consumer-id={processIndex}");
            startInfo.ArgumentList.Add($"--runtime-root={runtimeRoot}");

            return startInfo;
        }

        private static async Task<bool> WaitForClientReadyAsync(
            ClientRuntime runtime,
            TimeSpan timeout,
            CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                token.ThrowIfCancellationRequested();

                if (!IsProcessAlive(runtime.Process))
                {
                    return false;
                }

                if (runtime.Consumer.ClientWindowHandle != 0)
                {
                    return true;
                }

                // 异步等待，不占用线程池线程进行 SpinWait。
                await Task.Delay(50, token);
            }

            return false;
        }

        private async Task ProcessOneTaskAsync(
            JObject task,
            ClientRuntime runtime,
            int processIndex,
            TimeSpan processLifetime,
            CancellationToken token)
        {
            if (!TryParseTask(task, out var parsed, out var parseError))
            {
                LogWriteLine(
                    $"消费者[{processIndex}]任务参数错误：{parseError}；task={task}");
                return;
            }

            var exposure = _adxHelper.GetOrAddTaskStatus(parsed.TaskId);

            var network = await TryGetNetworkContextAsync(
                parsed.RawTask,
                processIndex,
                token);

            if (network is null)
            {
                LogWriteLine(
                    $"任务[{parsed.TaskId}_{processIndex}]获取可用网络失败。");
                return;
            }

            var os = ResolveOs(parsed.DeviceClientId);
            var ipTtlSeconds = Math.Max(1, _appSettings.Value.IpTtl);
            var uvIntervalMs = Math.Max(0, _appSettings.Value.UVInterval);
            var ipDeadline = DateTime.UtcNow.AddSeconds(ipTtlSeconds);
            var hasCheckedFirstAdxInCurrentTask = false;

            for (var uv = 0; uv < parsed.TotalUv; uv++)
            {
                token.ThrowIfCancellationRequested();

                if (!IsProcessAlive(runtime.Process) ||
                    HasExceededLifetime(runtime, processLifetime))
                {
                    LogWriteLine(
                        $"停止任务[{parsed.TaskId}_{processIndex}]：客户端不可用或已到重启时间。");
                    break;
                }

                var triggeredClick = await ExecuteUvAsync(uv);

                if (_appSettings.Value.UVsTriggerOne && triggeredClick)
                {
                    break;
                }
            }

            async Task<bool> ExecuteUvAsync(int uv)
            {
                if (uv > 0 && uvIntervalMs > 0)
                {
                    if (DateTime.UtcNow.AddMilliseconds(uvIntervalMs) > ipDeadline)
                    {
                        LogWriteLine(
                            $"跳过UV[{parsed.TaskId}_{processIndex}_{uv}]，" +
                            $"预计执行时间超出IP有效期 {ipTtlSeconds}s。");
                        return false;
                    }

                    await Task.Delay(uvIntervalMs, token);
                }

                if (DateTime.UtcNow > ipDeadline ||
                    !IsProcessAlive(runtime.Process))
                {
                    return false;
                }

                Interlocked.Increment(ref RequestCount);
                Interlocked.Increment(ref TotalRequestCount);
                exposure.AddAllCount(1);

                JObject dev;

                try
                {
                    var devResult = await _devHelper.GetDevByOS(os, 200);
                    dev = devResult as JObject
                        ?? throw new InvalidOperationException("设备数据不是 JObject。");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine(
                        $"获取设备信息失败[{parsed.TaskId}_{processIndex}_{uv}]：{ex}");
                    return false;
                }

                JObject? adx;

                try
                {
                    adx = await _adxHelper.GetAdRequest(
                        parsed.RawTask,
                        parsed.AdParam,
                        dev,
                        os,
                        network.RealIp,
                        network.ProxyServer,
                        network.IpInfo,
                        _appSettings.Value.IsProxyMode);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine(
                        $"请求广告失败[{parsed.TaskId}_{processIndex}_{uv}]：" +
                        $"{ex.Message}, proxy={network.ProxyServer}");
                    return false;
                }

                if (!IsValidAdxResponse(adx, out var reason))
                {
                    LogWriteLine(
                        $"请求广告[{parsed.TaskId}_{processIndex}_{uv}]没有有效填充，" +
                        $"reason={reason}, proxy={network.ProxyServer}, response={adx}");
                    return false;
                }

                if (_appSettings.Value.PersistAdx)
                    await SaveAdx(adx!, 0);

                if (!HasAcceptableBid(adx!))
                {
                    LogWriteLine(
                        $"请求广告[{parsed.TaskId}_{processIndex}_{uv}]单价过低，" +
                        $"threshold={_appSettings.Value.DspBidPrice}, " +
                        $"proxy={network.ProxyServer}");
                    return false;
                }

                if (_appSettings.Value.PersistAdx)
                    await SaveAdx(adx!, 1);

                var cacheIndex = $"s{processIndex}_{uv}";
                var clickJump = false;
                double ctr;

                var args = new JObject
                {
                    ["task"] = parsed.RawTask,
                    ["dev"] = dev,
                    ["isShowLog"] = _appSettings.Value.IsDetailLog,
                    ["isHiddenMode"] = _appSettings.Value.IsHiddenMode,
                    ["isProxyMode"] = _appSettings.Value.IsProxyMode,
                    ["proxy_server"] = network.ProxyServer,
                    ["ipinfo"] = network.IpInfo,
                    ["realip"] = network.RealIp,
                    ["vast"] = adx,
                    ["cacheIndex"] = cacheIndex,
                    ["url"] = parsed.Url,
                    ["referer"] = string.Empty,
                    ["os"] = (int)os,
                    ["clearDataForOrigin"] = "local_storage",
                    ["pageLoadingTimeout"] = _appSettings.Value.PageLoadingTimeout,
                    ["uv"] = uv
                };

                // 这里需要让“决定点击、发送消息、更新计数”形成一个一致操作。
                // 最佳做法是把这段原子逻辑移动到 exposure 类型内部。
                lock (exposure)
                {
                    var projectedAdxCount = exposure.adxCount + 1;
                    var projectedCtr = parsed.ClickRate > 0 && projectedAdxCount > 0
                        ? ((exposure.pendingClick + 1) / (double)projectedAdxCount) * 100
                        : 0;

                    if (!hasCheckedFirstAdxInCurrentTask && parsed.ClickRate > 0)
                    {
                        hasCheckedFirstAdxInCurrentTask = true;

                        if (parsed.ClickRate == 100 ||
                            exposure.pendingClick == 0 ||
                            exposure.adxCount == 0 ||
                            projectedCtr < parsed.ClickRate)
                        {
                            clickJump = true;
                        }
                    }

                    args["clickJump"] = clickJump;

                    // 如果此方法可能长时间阻塞，建议改为带 ACK 的异步发送，
                    // 再由 exposure 提供 Reserve/Commit/Rollback 方法。
                    SendCefLoadMessage(runtime.Consumer, args);

                    exposure.AddAdxCount(1);
                    if (clickJump)
                    {
                        exposure.AddPendingClick(1);
                    }

                    ctr = parsed.ClickRate > 0 && exposure.adxCount > 0
                        ? (exposure.pendingClick / (double)exposure.adxCount) * 100
                        : 0;
                }

                LogWriteLine(
                    $"提交任务:{parsed.Title}" +
                    $"[{parsed.TaskId}_{processIndex}_{cacheIndex}]," +
                    $"activity={runtime.Consumer.TaskCount}," +
                    $"os={os},proxy={network.ProxyServer}," +
                    $"realIp={network.RealIp},click={clickJump}," +
                    $"点击比率={ctr:N2}%,{uv}/{parsed.TotalUv}");

                _adxHelper.UpdateTaskAll(parsed.TaskId, 1);
                Interlocked.Increment(ref SuccessCount);
                Interlocked.Increment(ref TotalSuccessCount);

                if (runtime.Consumer.TaskCount > parsed.TotalUv)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(Random.Shared.Next(3, 5)),
                        token);
                }

                return clickJump;
            }
        }

        private async Task<NetworkContext?> TryGetNetworkContextAsync(
            JObject task,
            int processIndex,
            CancellationToken token)
        {
            const int maxAttempts = 6; // 首次 + 最多5次重试。

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var proxyServer = string.Empty;
                    var realIp = string.Empty;

                    if (_appSettings.Value.IsProxyMode)
                    {
                        var ipEntity = await _ipHelper.GetProxyIpAsync(task);

                        if (ipEntity is null)
                        {
                            throw new InvalidOperationException("代理服务返回空数据。");
                        }

                        if (ipEntity.format == IPFormat.JSON)
                        {
                            var host = ipEntity.json?["ip"]?.Value<string>();
                            var port = ipEntity.json?["port"]?.Value<int?>();

                            if (string.IsNullOrWhiteSpace(host) || port is null)
                            {
                                throw new FormatException("代理JSON缺少 ip 或 port。");
                            }

                            proxyServer = $"{host}:{port.Value}";

                            // 原方法同时出现 RealIp 与 IsRealIp，建议统一成一个配置项。
                            if (_appSettings.Value.IsRealIp)
                            {
                                realIp = ipEntity.json?["realIp"]?.Value<string>()
                                    ?? string.Empty;
                            }
                        }
                        else
                        {
                            proxyServer = ipEntity.value ?? string.Empty;
                        }

                        if (!TryNormalizeProxyServer(proxyServer, out proxyServer))
                        {
                            throw new FormatException(
                                $"代理地址格式无效：{proxyServer}");
                        }
                    }

                    var testResult = _appSettings.Value.IsProxyMode
                        ? await _ipTester.TestAsync(proxyServer)
                        : await _ipTester.TestAsync();

                    if (!testResult.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"IP可用性检测失败：{proxyServer}");
                    }

                    if (string.IsNullOrWhiteSpace(testResult.Data) ||
                        !TryParseObject(testResult.Data, out var ipInfo))
                    {
                        throw new JsonException("IP检测结果不是有效JSON对象。");
                    }

                    if (_appSettings.Value.IsRealIp)
                    {
                        var testedRealIp = ipInfo["query"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(testedRealIp))
                        {
                            realIp = testedRealIp;
                        }
                    }

                    return new NetworkContext(proxyServer, realIp, ipInfo);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine(
                        $"获取IP失败[{processIndex}]，" +
                        $"attempt={attempt}/{maxAttempts}：{ex.Message}");

                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(GetRetryDelay(attempt, ex), token);
                    }
                }
            }

            return null;
        }

        private bool TryParseTask(
            JObject task,
            out ParsedTask parsed,
            out string error)
        {
            parsed = null!;
            error = string.Empty;

            try
            {
                var taskId = task.Value<int?>("id");
                var totalUv = task.Value<int?>("uv");
                var title = task.Value<string>("title");
                var encodedJsText = task.Value<string>("jstext");
                var rawClient = task.Value<string>("client");
                var url = task.Value<string>("url") ?? string.Empty;

                if (taskId is null || taskId <= 0)
                {
                    error = "id 缺失或无效";
                    return false;
                }

                if (totalUv is null || totalUv <= 0)
                {
                    error = "uv 缺失或必须大于0";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(encodedJsText))
                {
                    error = "jstext 缺失";
                    return false;
                }

                var decodedJsText = System.Web.HttpUtility.UrlDecode(encodedJsText);
                var adParam = JsonConvert.DeserializeObject<JObject>(decodedJsText);

                if (adParam is null)
                {
                    error = "jstext 解码后不是JSON对象";
                    return false;
                }

                var deviceClientId = rawClient?
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault() ?? string.Empty;

                var clickRate = Math.Clamp(
                    task.Value<int?>("click_rate") ?? 0,
                    0,
                    100);

                parsed = new ParsedTask(
                    taskId.Value,
                    string.IsNullOrWhiteSpace(title) ? $"Task-{taskId}" : title,
                    totalUv.Value,
                    clickRate,
                    deviceClientId,
                    url,
                    adParam,
                    task);

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static OSType ResolveOs(string deviceClientId)
        {
            return deviceClientId switch
            {
                "4" => OSType.IOS,
                "7" => OSType.PC,
                "10" => OSType.OTT,
                _ => OSType.ANDROID
            };
        }

        private bool HasAcceptableBid(JObject adx)
        {
            var slotAd = adx["slot_ad"];
            if (slotAd is null)
            {
                return false;
            }

            var threshold = Convert.ToDecimal(
                _appSettings.Value.DspBidPrice,
                CultureInfo.InvariantCulture);

            foreach (var token in slotAd.SelectTokens("$..dsp_bid_price"))
            {
                if (decimal.TryParse(
                        token.ToString(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var price) &&
                    price > threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidAdxResponse(
            JObject? adx,
            out string reason)
        {
            reason = adx?["reason"]?.Value<string>() ?? string.Empty;
            var code = adx?["code"]?.Value<int?>();

            return adx is not null &&
                   code == 0 &&
                   !string.IsNullOrWhiteSpace(reason) &&
                   reason.StartsWith("ok!", StringComparison.Ordinal);
        }

        private static bool TryParseObject(
            string json,
            out JObject value)
        {
            try
            {
                value = JObject.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                value = null!;
                return false;
            }
        }

        private static bool TryNormalizeProxyServer(
            string? value,
            out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            var separatorIndex = value.LastIndexOf(':');

            if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
            {
                return false;
            }

            var hostText = value[..separatorIndex].Trim();
            var portText = value[(separatorIndex + 1)..].Trim();

            if (hostText.StartsWith('[') && hostText.EndsWith(']'))
            {
                hostText = hostText[1..^1];
            }

            if (!IPAddress.TryParse(hostText, out var address) ||
                !int.TryParse(portText, out var port) ||
                port is < 1 or > 65535)
            {
                return false;
            }

            normalized = address.AddressFamily == AddressFamily.InterNetworkV6
                ? $"[{address}]:{port}"
                : $"{address}:{port}";

            return true;
        }

        private TimeSpan GetProcessLifetime()
        {
            var minutes = _appSettings.Value.SubResetTimeout;
            if (minutes <= 0)
            {
                return Timeout.InfiniteTimeSpan;
            }

            var seconds = (long)minutes * 60 + Random.Shared.Next(-5, 6);
            return TimeSpan.FromSeconds(Math.Max(1, seconds));
        }

        private static bool HasExceededLifetime(
            ClientRuntime runtime,
            TimeSpan lifetime)
        {
            return lifetime != Timeout.InfiniteTimeSpan &&
                   DateTime.UtcNow - runtime.StartedUtc >= lifetime;
        }

        private static bool IsProcessAlive(Process? process)
        {
            if (process is null)
            {
                return false;
            }

            try
            {
                return !process.HasExited;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }

        }

        private async Task StopClientRuntimeAsync(
            ClientRuntime runtime,
            string reason)
        {
            processOfList.TryRemove(runtime.ClientId, out _);

            var process = runtime.Process;

            try
            {
                if (IsProcessAlive(process))
                {
                    LogWriteLine(
                        $"清理进程：PID={process.Id}, reason={reason}, " +
                        $"path={runtime.ExecutablePath}");

                    process.Kill(entireProcessTree: true);

                    using var timeoutCts =
                        new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    try
                    {
                        await process.WaitForExitAsync(timeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        LogWriteLine(
                            $"等待进程退出超时：PID={process.Id}");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // 已退出或尚未启动。
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LogWriteLine(
                    $"终止进程失败：PID={SafeGetProcessId(process)}, {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        private static int SafeGetProcessId(Process? process)
        {
            try
            {
                return process?.Id ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static void TryKillAndDispose(Process? process)
        {
            if (process is null)
            {
                return;
            }

            try
            {
                if (IsProcessAlive(process))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // 启动失败清理路径中不再覆盖原异常。
            }
            finally
            {
                process.Dispose();
            }
        }

        private static TimeSpan GetRetryDelay(
            int attempt,
            Exception exception)
        {
            if (exception.Message.Contains(
                    "没有满足您选择的条件IP",
                    StringComparison.Ordinal))
            {
                return TimeSpan.FromMilliseconds(
                    Random.Shared.Next(2000, 3001));
            }

            var exponentialBase = Math.Min(
                2000,
                200 * (1 << Math.Min(attempt - 1, 3)));

            return TimeSpan.FromMilliseconds(
                exponentialBase + Random.Shared.Next(100, 301));
        }

        /// <summary>
        /// 所有消费者共享同一份只读 CefClient 程序包。消费者之间仅隔离运行数据目录，
        /// 不复制 exe、dll、CEF resources 或 locales。
        /// </summary>
        private static string GetSharedClientExecutable()
        {
            token.ThrowIfCancellationRequested();
            var sourceRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "CefClient"));

            var sourceExecutable = Path.Combine(sourceRoot, "CefClient.exe");
            if (!File.Exists(sourceExecutable))
            {
                throw new FileNotFoundException(
                    "找不到源 CefClient.exe。",
                    sourceExecutable);
            }

            string[] requiredFiles =
            {
                "CefClient.exe",
                "CefClient.dll",
                "CefClient.runtimeconfig.json",
                "CefClient.deps.json"
            };
            foreach (var fileName in requiredFiles)
            {
                var path = Path.Combine(sourceRoot, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        $"CefClient 程序包缺少必要文件：{fileName}",
                        path);
                }
            }

            return sourceExecutable;
        }



    }

}
