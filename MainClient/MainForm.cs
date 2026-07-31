using Huichuan.Protocol;
using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Logging;
using MainClient.LogViewer;
using MainClient.Models;
using MainClient.Properties;
using MainClient.Scheduler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Win32;

namespace MainClient
{
    public partial class MainForm : Form
    {
        private int MainFormHandle = 0;
        private CancellationTokenSource? cts;

        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly DevHelper _devHelper;
        private readonly AdxHelper _adxHelper;
        private readonly IpHelper _ipHelper;
        private readonly ProxyTester _ipTester;
        private readonly CopyDataMessageQueue messageQueue;
        private readonly TaskMetricsService _aggregator;



        #region 任务调度管理
        private TaskDispatchManager _taskManager = default!;
        private void InitTaskDispatchManager()
        {
            _taskManager = new TaskDispatchManager(new TaskDispatchManagerOptions
            {
                // 队列容量,表示最多提前缓存 指定数量 任务
                Capacity = _appSettings.MaxConcurrency,

                // 停止时，把队列里还没被取出的任务落盘
                PersistPendingOnStop = true,

                // 下次启动时，先加载上次落盘的任务
                LoadPersistedOnStart = false,

                // 加载成功后删除落盘文件，避免重复执行
                DeletePersistenceFileAfterLoad = true,

                PersistenceFilePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "pending_tasks.json"),

                // 单个任务失败，不影响整体继续跑
                ContinueOnTaskError = true,

                // 停止最多等待 8 秒
                DefaultStopTimeout = TimeSpan.FromSeconds(8),

                ScheduledTasks = new List<ScheduledTaskOptions>()
                {
                    new ScheduledTaskOptions(){
                        Name="定时更新UI",
                        Interval = TimeSpan.FromSeconds(1),
                      // 初始化 Callback
                        Callback = async (cancellationToken) =>
                        {
                            try
                            {
                              //LogWriteLine($"定时触发: {DateTime.Now}");

                              await this.UiInvokeAsync(() =>{
                                  RefreshTrafficStatsToUi();
                              });
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine("任务被取消");
                            }
                        },
                        ContinueOnError = true
                    }
                }
            });

            _taskManager.ConfigureStart(new TaskDispatchStartOptions
            {
                // TrafficTaskStateKind者数量
                ConsumerCount = _appSettings.MaxConcurrency,
                // 生产者方法
                Producer = ProducerAsync,
                // 消费者方法
                Consumer = ConsumerAsync
            });
            _taskManager.StateChanged += TaskManager_StateChanged;
            _taskManager.LogEmitted += TaskManager_LogEmitted;
            _taskManager.TaskEnqueued += TaskManager_TaskEnqueued;
            _taskManager.TaskDequeued += TaskManager_TaskDequeued;
            _taskManager.TaskStarted += TaskManager_TaskStarted;
            _taskManager.TaskSucceeded += TaskManager_TaskSucceeded;
            _taskManager.TaskFailed += TaskManager_TaskFailed;
            _taskManager.TaskCanceled += TaskManager_TaskCanceled;
            _taskManager.TaskDropped += TaskManager_TaskDropped;
            _taskManager.PendingTasksPersisted += TaskManager_PendingTasksPersisted;
            _taskManager.PersistedTasksLoaded += TaskManager_PersistedTasksLoaded;
            _taskManager.StatisticsChanged += TaskManager_StatisticsChanged;

            RefreshStartStopButton(_taskManager.State);

            this.FormClosing += async (s, e) =>
            {
                if (_taskManager == null)
                    return;
                if (_taskManager.State == RunnerState.Running ||
                    _taskManager.State == RunnerState.Stopping)
                {
                    e.Cancel = true;

                    btnStartStop.Enabled = false;
                    btnStartStop.Text = "停止中...";

                    try
                    {
                        await _taskManager.StopAsync(new TaskDispatchStopOptions
                        {
                            Timeout = TimeSpan.FromSeconds(8),
                            PersistPending = true
                        });
                    }
                    catch
                    {
                    }

                    e.Cancel = false;
                    Close();
                }

            };
        }
        #endregion


        #region 状态变化事件：更新按钮文本
        private void TaskManager_StateChanged(
        object? sender,
        RunnerStateChangedEventArgs e)
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                RefreshStartStopButton(e.NewState);
            });
        }
        private void RefreshStartStopButton(RunnerState state)
        {
            switch (state)
            {
                case RunnerState.Stopped:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "开始";
                    break;

                case RunnerState.Running:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "停止";
                    break;

                case RunnerState.Stopping:
                    btnStartStop.Enabled = false;
                    btnStartStop.Text = "停止中...";
                    break;

                case RunnerState.Faulted:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "重新开始";
                    break;
            }
        }

        #endregion



        #region 日志事件
        private void TaskManager_LogEmitted(
        object? sender,
        DispatchLogEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog(e.ToString());

            //    if (e.Exception != null)
            //    {
            //        AddLog(e.Exception.ToString());
            //    }
            //});
        }
        #endregion

        #region 任务事件
        private void TaskManager_TaskEnqueued(
        object? sender,
        DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务入队: {e.TaskId}");
            //});
        }

        private void TaskManager_TaskDequeued(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务出队: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskStarted(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务开始: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskSucceeded(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务成功: Consumer={e.ConsumerId}, TaskId={e.TaskId}, 耗时={e.Elapsed?.TotalMilliseconds:0}ms");
            //});
        }

        private void TaskManager_TaskFailed(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务失败: Consumer={e.ConsumerId}, TaskId={e.TaskId}, Error={e.Exception?.Message}");
            //});
        }

        private void TaskManager_TaskCanceled(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务取消: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskDropped(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //BeginInvokeSafe(() =>
            //{
            //    AddLog($"任务丢弃/待落盘: TaskId={e.TaskId}");
            //});
        }
        #endregion

        #region 任务队列的落盘/恢复
        private void TaskManager_PendingTasksPersisted(
        object? sender,
        PendingTasksPersistedEventArgs e)
        {
            LogWriteLine($"剩余任务已落盘: Count={e.Count}, File={e.FilePath}");
        }

        private void TaskManager_PersistedTasksLoaded(
            object? sender,
            PersistedTasksLoadedEventArgs e)
        {
            LogWriteLine($"落盘任务已恢复: Count={e.Count}, File={e.FilePath}");
        }
        #endregion

        #region 任务执行状态统计
        private void TaskManager_StatisticsChanged(
        object? sender,
        TaskDispatchSnapshot snapshot)
        {
            // 当前界面统计由 _statsTimer 周期性拉取 TrafficAggregator 快照统一刷新，
            // 避免高频事件直接更新 UI 造成界面抖动或跨线程访问。
        }

        /// <summary>
        /// 更新UI上的统计数据
        /// </summary>
        private void RefreshTrafficStatsToUi()
        {
            try
            {
                var host = _aggregator.GetHostSnapshot();
                var taskSnapshot = _taskManager.Snapshot;
                label_request.Text = $"请求数量:{host.Request}";
                label_start.Text = $"提交数量:{host.Start}";
                label_dsp.Text = $"曝光数量:{host.Dsp}";
                label_click.Text = $"点击数量:{host.Clickthrough} ({host.ClickRatio:P2})";
                label_time.Text = $"运行时间:{FormatElapsed(taskSnapshot.RunElapsed)}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RefreshTrafficStatsToUi failed.");
            }
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

            return elapsed.ToString(@"mm\:ss");
        }
        #endregion

        #region 生产任务
        private async Task ProducerAsync(
        ChannelWriter<JToken> writer,
        CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<JToken> taskOfList;

                    try
                    {
                        taskOfList = await _adxHelper.GetTasksAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogWriteLine($"拉取任务异常: {ex}");

                        await Task.Delay(_appSettings.TaskPullIntervalMs, token).ConfigureAwait(false);
                        continue;
                    }

                    if (taskOfList.Count == 0)
                    {
                        int interval = _appSettings.TaskPullIntervalMs <= 0
                            ? 500
                            : _appSettings.TaskPullIntervalMs;

                        await Task.Delay(interval, token).ConfigureAwait(false);
                        continue;
                    }

                    int multiple = _appSettings.Multiple <= 0
                        ? 1
                        : _appSettings.Multiple;

                    int writeCount = 0;

                    int fetchCount = taskOfList.Count();

                    foreach (var task in taskOfList)
                    {
                        token.ThrowIfCancellationRequested();

                        for (int i = 0; i < multiple; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var cloned = task.DeepClone();

                            if (cloned is JObject obj)
                            {
                                //obj["_copyIndex"] = i + 1;
                                //obj["_copyTotal"] = multiple;
                                //obj["_dispatchTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            }

                            // 重点：
                            // 正常运行时，如果 Channel 满了，这里会等待。
                            // 点击停止时，token 取消，这里会立即退出。
                            await writer.WriteAsync(cloned, token).ConfigureAwait(false);

                            writeCount++;
                        }
                    }

                    LogWriteLine($"本轮取回={fetchCount}，倍率={multiple}，写入队列={writeCount}");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                LogWriteLine("Producer 已取消。");
            }
            catch (ChannelClosedException)
            {
                LogWriteLine("Producer 检测到 Channel 已关闭。");
            }
            catch (Exception ex)
            {
                LogWriteLine($"Producer 主循环异常: {ex}");
            }
            finally
            {
                writer.TryComplete();
            }
        }

        #endregion

        #region 执行任务


        /// <summary>
        /// 安全消费 Channel 中的任务。
        /// 主要职责仅保留：读取任务、确保子进程存在、处理任务、回收子进程。
        /// </summary>
        public async Task ConsumerAsync(
        int consumerId,
        JToken task,
        CancellationToken token)
        {
            ClientRuntime? runtime = null;
            var processLifetime = GetProcessLifetime();
            try
            {
                runtime = await EnsureClientRuntimeAsync(
                    runtime,
                    consumerId,
                    token);

                if (runtime is null)
                {
                    LogWriteLine($"消费者[{consumerId}]无法启动 CefClient，跳过任务。");
                    return;
                }

                await ProcessOneTaskAsync(
                    task,
                    runtime,
                    consumerId,
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

            }
            catch (Exception ex)
            {
                // 不要只记录 ex.Message，否则会丢失堆栈和内部异常。
                LogWriteLine($"消费者[{consumerId}]处理任务异常：{ex}");

                if (runtime is not null && !IsProcessAlive(runtime.Process))
                {
                    await StopClientRuntimeAsync(runtime, "异常后发现子进程不可用");
                    runtime = null;
                }
            }
            finally
            {
                if (runtime is not null)
                {
                    await StopClientRuntimeAsync(runtime, "消费者退出");
                }
            }
            return;

        }
        #endregion

 
 





        #region LogWrite

        private readonly ConcurrentQueue<UiLogItem> _uiLogBuffer = new();
        private readonly System.Windows.Forms.Timer _uiTimer = new();
        private CancellationTokenSource _uiLogCts = new();
        private int _flushing = 0;
        private const int MaxFlushCount = 500;
        // 新控件
        private LogViewerUltra logViewer;
        private void StartLogConsumer()
        {
            // 初始化新控件
            logViewer = new LogViewerUltra()
            {
                Dock = DockStyle.Fill
            };
            groupBox4.Controls.Add(logViewer);

            // 后台读取日志
            Task.Run(async () =>
            {
                var reader = UiLogChannel.Channel.Reader;

                try
                {
                    await foreach (var item in reader.ReadAllAsync(_uiLogCts.Token))
                    {
                        if (_uiLogCts.IsCancellationRequested)
                            break;

                        _uiLogBuffer.Enqueue(item);
                    }
                }
                catch (OperationCanceledException) { }

            }, _uiLogCts.Token);

            // UI Timer
            _uiTimer.Interval = 200;
            _uiTimer.Tick += (_, __) =>
            {
                if (Interlocked.Exchange(ref _flushing, 1) == 1)
                    return;

                try
                {
                    FlushLogsToUi();
                }
                finally
                {
                    Interlocked.Exchange(ref _flushing, 0);
                }
            };
            _uiTimer.Start();

            this.FormClosing += (s, e) =>
            {
                try
                {
                    _uiTimer.Stop();
                    _uiLogCts.Cancel();
                    UiLogChannel.Channel.Writer.TryComplete();
                }
                catch { }
            };
        }
        private void FlushLogsToUi()
        {
            if (IsDisposed || Disposing)
                return;

            if (!IsHandleCreated || logViewer.IsDisposed)
                return;

            if (_uiLogBuffer.IsEmpty)
                return;

            int count = 0;

            while (_uiLogBuffer.TryDequeue(out var item))
            {
                logViewer.WriteLog(item.Message, ConvertLevel(item.Level));

                if (++count >= MaxFlushCount)
                    break;
            }
        }
        // 日志级别映射
        private LogLevel ConvertLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        public void LogWriteLine(string message)
        {
            _logger.LogInformation(message);
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
                    _aggregator.EnqueueTaskState(new TrafficTaskStateEvent(taskId, TrafficTaskStateKind.Clickthrough, 1));
                }
                else
                {
                    _aggregator.EnqueueTaskState(new TrafficTaskStateEvent(taskId, TrafficTaskStateKind.DSP, 1));
                }
            }
            else if (msgName.Equals(CefProtocol.Messages.TaskLog, StringComparison.Ordinal))
            {
                if (_appSettings.IsDetailLog)
                {
                    LogWriteLine(message.SelectToken("Data.Message").Value<string>());
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
            var sent = Win32Api.SendMessageTimeout(
                consumer.ClientWindowHandle,
                Win32Api.WM_COPYDATA,
                IntPtr.Zero,
                ref cds,
                abortIfHung,
                5000,
                out var handled);
            if (sent == IntPtr.Zero || handled == IntPtr.Zero)
                throw new TimeoutException($"向 CefClient 窗口 {consumer.ClientWindowHandle} 发送消息失败、超时或被拒绝。");
        }


        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg != Win32Api.WM_COPYDATA)
            {
                base.DefWndProc(ref m);
                return;
            }

            try
            {
                var data = (COPYDATASTRUCT)m.GetLParam(typeof(COPYDATASTRUCT));
                if (data.dwData == (IntPtr)CefProtocol.CopyDataId &&
                    data.cbData > 1 &&
                    messageQueue.TryEnqueue(data.lpData))
                    m.Result = (IntPtr)1;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "接收 WM_COPYDATA 失败");
                m.Result = IntPtr.Zero;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            messageQueue.Dispose();
            base.OnFormClosed(e);
        }
        #endregion



        public MainForm(
            TaskMetricsService aggregator,
            DevHelper devHelper,
            AdxHelper adxHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            AppSettings appSettings,
            ILogger<MainForm> logger)
        {
            _aggregator = aggregator;
            _devHelper = devHelper;
            _adxHelper = adxHelper;
            _ipHelper = ipHelper;
            _ipTester = ipTester;
            _appSettings = appSettings;
            _logger = logger;

            messageQueue = new CopyDataMessageQueue(
                ResolveMessage,
                exception => _logger.LogError(exception, "处理 WM_COPYDATA 失败"),
                Math.Clamp(Environment.ProcessorCount / 2, 1, 4));


            InitializeComponent();
            FormClosing += MainForm_FormClosing;
            this.Text += $"{AppConsts.AppVersion}";
            LoadAppSetting();
            InitTaskDispatchManager();

            #region 数据初始化
            this.textBox_SmsName.Text = CommonHelper.GetHostName();
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                toolStripStatusLabel1.Text = $"CPU:{item["NumberOfLogicalProcessors"]}";
            }
            #endregion
        }
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
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
            this.MainFormHandle = (int)this.Handle;
            StartLogConsumer();
            _logger.LogInformation("应用已启动");
            Task.Run(() =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {

                    #region 控件初始化
                    var controls = new List<Control>() { groupBox2 };
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
                            else if (c is ComboBox)
                            {
                                (c as ComboBox).SelectedIndexChanged += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                        }
                    }
                    #endregion

                });
            });

        }

        #region 应用设置
        private void LoadAppSetting()
        {

            textBox_ProxyIpUrl.Text = _appSettings.ProxyIpUrl;
            textBox_TaskApiUrl.Text = _appSettings.TaskApiUrl;
            textBox_DevApiUrl.Text = _appSettings.DevApiUrl;
            numericUpDown_FetchTaskInterval.Value = _appSettings.TaskPullIntervalMs;
            numericUpDown_UVInterval.Value = _appSettings.UvExecutionIntervalMs;
            numericUpDown_MaxConcurrency.Value = _appSettings.MaxConcurrency;
            numericUpDown_PageLoadingTimeout.Value = _appSettings.PageLoadTimeout;
            textBox_TaskName.Text = _appSettings.TaskName;
            numericUpDown_Multiple.Value = _appSettings.Multiple;
            numericUpDown_MainResetTimeout.Value = _appSettings.MainResetTimeout;
            numericUpDown_SubResetTimeout.Value = _appSettings.SubResetTimeout;
            checkBox_IsHiddenMode.Checked = _appSettings.IsHiddenMode;
            checkBox_IsProxyMode.Checked = _appSettings.IsProxyMode;
            checkBox_IsRealIp.Checked = _appSettings.IsRealIp;
            checkBox_IsCheckIp.Checked = _appSettings.IsCheckIp;
            checkBox_DisableUserCache.Checked = _appSettings.DisableUserCache;
            checkBox_SendSms.Checked = _appSettings.SendSms;
            textBox_SmsName.Text = _appSettings.SmsName;
            textBox_SmsPhone.Text = _appSettings.SmsPhone;
            numericUpDown_SendSmsTimeout.Value = _appSettings.SendSmsTimeout;
            var usingDevIndex = _appSettings.UsingDevIndex;
            if (usingDevIndex == 2)
                radioButton_UsingRealDev.Checked = true;
            else if (usingDevIndex == 3)
                radioButton_UseLocalDev.Checked = true;
            else
                radioButton_UseSystemDev.Checked = true;
            checkBox_IsDetailLog.Checked = _appSettings.IsDetailLog;

            numericUpDown_IpTtl.Value = _appSettings.IpTtl;
            numericUpDown_DspBidPrice.Value = _appSettings.DspBidPrice;

            checkBox_UVsTriggerOne.Checked = _appSettings.UVsTriggerOne;
            checkBox_PersistAdx.Checked = _appSettings.PersistAdx;
        }
        private void UpdateAppSetting()
        {
            _appSettings.ProxyIpUrl = textBox_ProxyIpUrl.Text;
            _appSettings.TaskApiUrl = textBox_TaskApiUrl.Text;
            _appSettings.DevApiUrl = textBox_DevApiUrl.Text;
            _appSettings.TaskPullIntervalMs = (int)numericUpDown_FetchTaskInterval.Value;
            _appSettings.UvExecutionIntervalMs = (int)numericUpDown_UVInterval.Value;
            _appSettings.MaxConcurrency = (int)numericUpDown_MaxConcurrency.Value;
            _appSettings.PageLoadTimeout = (int)numericUpDown_PageLoadingTimeout.Value;
            _appSettings.TaskName = textBox_TaskName.Text;
            _appSettings.Multiple = (int)numericUpDown_Multiple.Value;
            _appSettings.MainResetTimeout = (int)numericUpDown_MainResetTimeout.Value;
            _appSettings.SubResetTimeout = (int)numericUpDown_SubResetTimeout.Value;
            _appSettings.IsHiddenMode = checkBox_IsHiddenMode.Checked;
            _appSettings.IsProxyMode = checkBox_IsProxyMode.Checked;
            _appSettings.IsRealIp = checkBox_IsRealIp.Checked;
            _appSettings.IsCheckIp = checkBox_IsCheckIp.Checked;
            _appSettings.DisableUserCache = checkBox_DisableUserCache.Checked;
            _appSettings.SendSms = checkBox_SendSms.Checked;
            _appSettings.SmsName = textBox_SmsName.Text;
            _appSettings.SmsPhone = textBox_SmsPhone.Text;
            _appSettings.SendSmsTimeout = (int)numericUpDown_SendSmsTimeout.Value;
            if (radioButton_UsingRealDev.Checked)
                _appSettings.UsingDevIndex = 2;
            else if (radioButton_UseLocalDev.Checked)
                _appSettings.UsingDevIndex = 3;
            else
                _appSettings.UsingDevIndex = 1;
            _appSettings.IsDetailLog = checkBox_IsDetailLog.Checked;


            _appSettings.IpTtl = (int)numericUpDown_IpTtl.Value;
            _appSettings.DspBidPrice = (int)numericUpDown_DspBidPrice.Value;
            _appSettings.UVsTriggerOne = checkBox_UVsTriggerOne.Checked;
            _appSettings.PersistAdx = checkBox_PersistAdx.Checked;

            UserConfigService.Save("AppSettings", _appSettings);
        }
        #endregion

        private readonly ConcurrentDictionary<string, ConsumerModel> processOfList = new();


        private async void btnStartStop_Click(object sender, EventArgs e)
        {

            btnStartStop.Enabled = false;
            try
            {
                await _taskManager.ToggleAsync(new TaskDispatchStopOptions
                {
                    Timeout = TimeSpan.FromSeconds(8),
                    // 停止时保存队列中还没取出的任务
                    PersistPending = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "任务调度异常",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                RefreshStartStopButton(_taskManager.State);
            }
        }
        private void buttonClear_Click(object sender, EventArgs e)
        {

            buttonClear.Enabled = false;
            btnStartStop.Enabled = false;
            Task.Factory.StartNew(() =>
            {
                CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
            }).ContinueWith(t =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    btnStartStop.Enabled = true;
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
            double ClickRate,
            string DeviceClientId,
            string Url,
            JToken AdParam,
            JToken RawTask);

        private sealed record NetworkContext(
            string ProxyServer,
            string RealIp,
            JToken IpInfo);




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
                        LogWriteLine($"退出进程：clientId={clientId}, path={executablePath}");
                    };

                    if (!process.Start())
                    {
                        throw new InvalidOperationException($"Process.Start 返回 false：{executablePath}");
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

                    LogWriteLine($"创建进程完成：PID={process.Id}, path={executablePath}");

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
            startInfo.ArgumentList.Add($"mainWnd={MainFormHandle}");
            startInfo.ArgumentList.Add($"isHiddenMode={_appSettings.IsHiddenMode}");
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
            JToken task,
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


            var network = await TryGetNetworkContextAsync(
                parsed.RawTask,
                processIndex,
                token);

            if (network is null)
            {
                LogWriteLine($"任务[{parsed.TaskId}_{processIndex}]获取可用网络失败。");
                return;
            }


            var os = ResolveOs(parsed.DeviceClientId);
            var ipTtlSeconds = Math.Max(1, _appSettings.IpTtl);
            var uvIntervalMs = Math.Max(0, _appSettings.UvExecutionIntervalMs);
            var ipDeadline = DateTime.UtcNow.AddSeconds(ipTtlSeconds);

            for (var uv = 0; uv < parsed.TotalUv; uv++)
            {
                token.ThrowIfCancellationRequested();

                if (!IsProcessAlive(runtime.Process) || HasExceededLifetime(runtime, processLifetime))
                {
                    LogWriteLine($"停止任务[{parsed.TaskId}_{processIndex}]：客户端不可用或已到重启时间。");
                    break;
                }

                var triggeredClick = await ExecuteUvAsync(uv);

                if (_appSettings.UVsTriggerOne && triggeredClick)
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
                        LogWriteLine($"跳过UV[{parsed.TaskId}_{processIndex}_{uv}]，" + $"预计执行时间超出IP有效期 {ipTtlSeconds}s。");
                        return false;
                    }
                    await Task.Delay(uvIntervalMs, token);
                }

                if (DateTime.UtcNow > ipDeadline || !IsProcessAlive(runtime.Process))
                {
                    return false;
                }

                JObject dev;
                try
                {
                    var devResult = await _devHelper.GetDevByOS(os, 200);
                    dev = devResult as JObject ?? throw new InvalidOperationException("设备数据不是 JObject。");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine($"获取设备信息失败[{parsed.TaskId}_{processIndex}_{uv}]：{ex}");
                    return false;
                }

                JObject? adx;

                try
                {
                    _aggregator.EnqueueTaskState(new TrafficTaskStateEvent(parsed.TaskId, TrafficTaskStateKind.Request, 1));


                    //adx = await _adxHelper.GetAdRequest(
                    //    parsed.RawTask,
                    //    parsed.AdParam,
                    //    dev,
                    //    os,
                    //    network.RealIp,
                    //    network.ProxyServer,
                    //    network.IpInfo,
                    //    _appSettings.IsProxyMode);
                    adx = JObject.Parse(Resources.adx_json);


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

                if (_appSettings.PersistAdx)
                    await SaveAdx(adx!, 0);

                if (!HasAcceptableBid(adx!))
                {
                    LogWriteLine(
                        $"请求广告[{parsed.TaskId}_{processIndex}_{uv}]单价过低，" +
                        $"threshold={_appSettings.DspBidPrice}, " +
                        $"proxy={network.ProxyServer}");
                    return false;
                }

                if (_appSettings.PersistAdx)
                    await SaveAdx(adx!, 1);

                var cacheIndex = $"s{processIndex}_{uv}";
                var clickJump = await _aggregator.CanClickthroughAsync(parsed.TaskId, parsed.ClickRate);


                var args = new JObject
                {
                    ["task"] = parsed.RawTask,
                    ["dev"] = dev,
                    ["isShowLog"] = _appSettings.IsDetailLog,
                    ["isHiddenMode"] = _appSettings.IsHiddenMode,
                    ["isProxyMode"] = _appSettings.IsProxyMode,
                    ["proxy_server"] = network.ProxyServer,
                    ["ipinfo"] = network.IpInfo,
                    ["realip"] = network.RealIp,
                    ["vast"] = adx,
                    ["cacheIndex"] = cacheIndex,
                    ["url"] = parsed.Url,
                    ["referer"] = string.Empty,
                    ["os"] = (int)os,
                    ["clearDataForOrigin"] = "local_storage",
                    ["pageLoadingTimeout"] = _appSettings.PageLoadTimeout,
                    ["uv"] = uv
                };

                args["clickJump"] = clickJump;
                SendCefLoadMessage(runtime.Consumer, args);

                _aggregator.EnqueueTaskState(new TrafficTaskStateEvent(parsed.TaskId, TrafficTaskStateKind.Start, 1));
                var ctr = (await _aggregator.GetClickRatioAsync(parsed.TaskId, parsed.ClickRate)) * 100;

                LogWriteLine(
                    $"提交任务:{parsed.Title}" +
                    $"[{parsed.TaskId}_{processIndex}_{cacheIndex}]," +
                    $"activity={runtime.Consumer.TaskCount}," +
                    $"os={os},proxy={network.ProxyServer}," +
                    $"realIp={network.RealIp},click={clickJump}," +
                    $"点击比率={ctr:N2}%,{uv}/{parsed.TotalUv}");

                //statistics.Increment(TaskStatisticNames.Processed);

                return clickJump;
            }
        }

        private async Task<NetworkContext?> TryGetNetworkContextAsync(
            JToken task,
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

                    if (_appSettings.IsProxyMode)
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
                            if (_appSettings.IsRealIp)
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

                    var testResult = _appSettings.IsProxyMode
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

                    if (_appSettings.IsRealIp)
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
            JToken task,
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

                // Keep the fractional part: parsing as int silently reduced the
                // configurable CTR resolution to whole percentage points.
                var rawClickRate = task.Value<double?>("click_rate") ?? 0d;
                if (!double.IsFinite(rawClickRate))
                {
                    error = "click_rate 必须是有限数值";
                    return false;
                }

                var clickRate = Math.Clamp(rawClickRate, 0d, 100d);

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
                _appSettings.DspBidPrice,
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
            var minutes = _appSettings.SubResetTimeout;
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
