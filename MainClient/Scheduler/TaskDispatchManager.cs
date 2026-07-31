using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace MainClient.Scheduler
{
    public sealed class TaskDispatchStartOptions
    {
        public int ConsumerCount { get; set; }

        public Func<ChannelWriter<JToken>, CancellationToken, Task> Producer { get; set; } = default!;

        public Func<int, JToken, CancellationToken, Task> Consumer { get; set; } = default!;

        public CancellationToken ExternalToken { get; set; } = default;
    }

    public sealed class TaskDispatchStopOptions
    {
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// 本次停止是否落盘。null 表示使用 TaskDispatchManagerOptions.PersistPendingOnStop。
        /// </summary>
        public bool? PersistPending { get; set; }

        /// <summary>
        /// 本次停止使用的落盘文件。null 表示使用 TaskDispatchManagerOptions.PersistenceFilePath。
        /// </summary>
        public string? PersistenceFilePath { get; set; }
    }

    public sealed class TaskDispatchStopResult
    {
        public bool TimedOut { get; init; }

        public int PendingCount { get; init; }

        public int PersistedCount { get; init; }

        public string? PersistenceFilePath { get; init; }

        public IReadOnlyList<JToken> PendingTasks { get; init; } = Array.Empty<JToken>();
    }

    public sealed class TaskDispatchToggleResult
    {
        public TaskDispatchToggleAction Action { get; init; }

        public RunnerState State { get; init; }

        public TaskDispatchStopResult? StopResult { get; init; }
    }

    public sealed class RunnerStateChangedEventArgs : EventArgs
    {
        public RunnerStateChangedEventArgs(
            RunnerState oldState,
            RunnerState newState,
            Exception? exception = null)
        {
            OldState = oldState;
            NewState = newState;
            Exception = exception;
            OccurredAt = DateTimeOffset.Now;
        }

        public RunnerState OldState { get; }

        public RunnerState NewState { get; }

        public Exception? Exception { get; }

        public DateTimeOffset OccurredAt { get; }
    }

    public sealed class DispatchLogEventArgs : EventArgs
    {
        public DispatchLogEventArgs(
            DispatchLogLevel level,
            string source,
            string message,
            Exception? exception = null)
        {
            Level = level;
            Source = source;
            Message = message;
            Exception = exception;
            OccurredAt = DateTimeOffset.Now;
        }

        public DispatchLogLevel Level { get; }

        public string Source { get; }

        public string Message { get; }

        public Exception? Exception { get; }

        public DateTimeOffset OccurredAt { get; }

        public override string ToString()
        {
            return $"[{OccurredAt:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Source}] {Message}";
        }
    }

    public sealed class DispatchTaskEventArgs : EventArgs
    {
        public DispatchTaskEventArgs(
            DispatchTaskEventKind kind,
            JToken task,
            int? consumerId = null,
            Exception? exception = null,
            TimeSpan? elapsed = null)
        {
            Kind = kind;
            Task = task;
            ConsumerId = consumerId;
            Exception = exception;
            Elapsed = elapsed;
            OccurredAt = DateTimeOffset.Now;
        }

        public DispatchTaskEventKind Kind { get; }

        public JToken Task { get; }

        public int? ConsumerId { get; }

        public Exception? Exception { get; }

        public TimeSpan? Elapsed { get; }

        public DateTimeOffset OccurredAt { get; }

        public string? TaskId => TaskDispatchManager.GetTaskId(Task);
    }

    public sealed class PendingTasksPersistedEventArgs : EventArgs
    {
        public PendingTasksPersistedEventArgs(string filePath, int count)
        {
            FilePath = filePath;
            Count = count;
            OccurredAt = DateTimeOffset.Now;
        }

        public string FilePath { get; }

        public int Count { get; }

        public DateTimeOffset OccurredAt { get; }
    }

    public sealed class PersistedTasksLoadedEventArgs : EventArgs
    {
        public PersistedTasksLoadedEventArgs(string filePath, int count)
        {
            FilePath = filePath;
            Count = count;
            OccurredAt = DateTimeOffset.Now;
        }

        public string FilePath { get; }

        public int Count { get; }

        public DateTimeOffset OccurredAt { get; }
    }

    public sealed class TaskDispatchSnapshot
    {
        public RunnerState State { get; init; }

        public int ConsumerCount { get; init; }

        public int QueueCount { get; init; }

        public long EnqueuedCount { get; init; }

        public long DequeuedCount { get; init; }

        public long StartedCount { get; init; }

        public long SucceededCount { get; init; }

        public long FailedCount { get; init; }

        public long CanceledCount { get; init; }

        public long DroppedCount { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public DateTimeOffset? StoppedAt { get; init; }

        public TimeSpan RunElapsed { get; init; }

        public Exception? LastException { get; init; }
    }

    public sealed class TaskDispatchManager : IAsyncDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly TaskDispatchManagerOptions _options;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private Channel<JToken> _channel = default!;
        private ChannelWriter<JToken> _writer = default!;

        private readonly List<Task> _consumerTasks = new List<Task>();

        private TaskDispatchStartOptions? _startOptions;

        private Task? _producerTask;
        private Task<TaskDispatchStopResult>? _stopTask;

        private CancellationTokenSource? _internalCts;
        private CancellationTokenRegistration _externalTokenRegistration;

        private volatile RunnerState _state = RunnerState.Stopped;

        private DateTimeOffset? _startedAt;
        private DateTimeOffset? _stoppedAt;
        private Exception? _lastException;

        private long _enqueuedCount;
        private long _dequeuedCount;
        private long _startedCount;
        private long _succeededCount;
        private long _failedCount;
        private long _canceledCount;
        private long _droppedCount;

        // 定时任务相关
        private readonly List<ScheduledTaskRun> _scheduledRuns = new();
        private int _scheduledExecutionCount;

        public TaskDispatchManager(TaskDispatchManagerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (_options.Capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(options.Capacity), "Capacity 必须大于 0");

            CreateNewChannel();
        }

        public TaskDispatchManager(int capacity)
            : this(new TaskDispatchManagerOptions { Capacity = capacity })
        {
        }

        public event EventHandler<RunnerStateChangedEventArgs>? StateChanged;

        public event EventHandler<DispatchLogEventArgs>? LogEmitted;

        public event EventHandler<Exception>? Faulted;

        public event EventHandler<DispatchTaskEventArgs>? TaskEnqueued;

        public event EventHandler<DispatchTaskEventArgs>? TaskDequeued;

        public event EventHandler<DispatchTaskEventArgs>? TaskStarted;

        public event EventHandler<DispatchTaskEventArgs>? TaskSucceeded;

        public event EventHandler<DispatchTaskEventArgs>? TaskFailed;

        public event EventHandler<DispatchTaskEventArgs>? TaskCanceled;

        public event EventHandler<DispatchTaskEventArgs>? TaskDropped;

        public event EventHandler<PendingTasksPersistedEventArgs>? PendingTasksPersisted;

        public event EventHandler<PersistedTasksLoadedEventArgs>? PersistedTasksLoaded;

        public event EventHandler<TaskDispatchSnapshot>? StatisticsChanged;

        public event EventHandler<ScheduledTaskExecutedEventArgs>? ScheduledTaskExecuted;

        public event EventHandler<ScheduledTaskFailedEventArgs>? ScheduledTaskFailed;

        public ChannelWriter<JToken> Writer => _writer;

        public RunnerState State => _state;

        public bool IsStarted => State == RunnerState.Running;

        public bool IsStopping => State == RunnerState.Stopping;

        public int ConsumerCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _consumerTasks.Count;
                }
            }
        }

        public TaskDispatchSnapshot Snapshot => GetSnapshot();

        public void ConfigureStart(TaskDispatchStartOptions startOptions)
        {
            if (startOptions == null)
                throw new ArgumentNullException(nameof(startOptions));

            if (startOptions.ConsumerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(startOptions.ConsumerCount), "ConsumerCount 必须大于 0");

            if (startOptions.Producer == null)
                throw new ArgumentNullException(nameof(startOptions.Producer));

            if (startOptions.Consumer == null)
                throw new ArgumentNullException(nameof(startOptions.Consumer));

            lock (_syncRoot)
            {
                if (_state == RunnerState.Running || _state == RunnerState.Stopping)
                    throw new InvalidOperationException("运行中不能修改启动配置。");

                _startOptions = startOptions;
            }
        }

        public Task StartAsync()
        {
            TaskDispatchStartOptions startOptions;

            lock (_syncRoot)
            {
                if (_startOptions == null)
                    throw new InvalidOperationException("请先调用 ConfigureStart 配置启动参数。");

                startOptions = _startOptions;
            }

            Start(
                consumerCount: startOptions.ConsumerCount,
                producer: startOptions.Producer,
                consumer: startOptions.Consumer,
                externalToken: startOptions.ExternalToken);

            return Task.CompletedTask;
        }

        public void Start(
            int consumerCount,
            Func<ChannelWriter<JToken>, CancellationToken, Task> producer,
            Func<int, JToken, CancellationToken, Task> consumer,
            CancellationToken externalToken = default)
        {
            if (consumerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(consumerCount), "consumerCount 必须大于 0");

            if (producer == null)
                throw new ArgumentNullException(nameof(producer));

            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));

            lock (_syncRoot)
            {
                if (_state == RunnerState.Running || _state == RunnerState.Stopping)
                    throw new InvalidOperationException("TaskDispatchManager 已经启动，不能重复 Start。");

                CleanupTokenOnly_NoLock();

                ResetCounters_NoLock();
                CreateNewChannel();

                _internalCts = new CancellationTokenSource();
                _stopTask = null;
                _lastException = null;
                _startedAt = DateTimeOffset.Now;
                _stoppedAt = null;

                _stopwatch.Reset();
                _stopwatch.Start();

                SetState(RunnerState.Running);

                var token = _internalCts.Token;

                if (externalToken.CanBeCanceled)
                {
                    _externalTokenRegistration = externalToken.Register(() =>
                    {
                        TryLog(
                            DispatchLogLevel.Warning,
                            "Runner",
                            "外部 CancellationToken 已取消，自动请求停止。");

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await StopAsync().ConfigureAwait(false);
                            }
                            catch
                            {
                            }
                        });
                    });
                }

                _consumerTasks.Clear();

                for (int i = 1; i <= consumerCount; i++)
                {
                    int consumerId = i;

                    var task = Task.Run(
                        () => RunConsumerLoopAsync(consumerId, consumer, token),
                        CancellationToken.None);

                    _consumerTasks.Add(task);
                }

                _producerTask = Task.Run(
                    () => RunProducerFlowAsync(producer, token),
                    CancellationToken.None);

                // 启动所有定时任务
                if (_options.ScheduledTasks is { Count: > 0 } tasks)
                {
                    foreach (var opt in tasks)
                    {
                        if (opt.Callback == null && opt.Task == null)
                            throw new InvalidOperationException(
                                $"定时任务 [{opt.Name}] 必须提供 Callback 或 Task。");

                        if (opt.Interval <= TimeSpan.Zero)
                            throw new InvalidOperationException(
                                $"定时任务 [{opt.Name}] 的 Interval 必须大于 0。");

                        var cts = new CancellationTokenSource();
                        var runTask = Task.Run(
                            () => RunScheduledLoopAsync(opt, cts.Token),
                            CancellationToken.None);

                        _scheduledRuns.Add(new ScheduledTaskRun(opt, runTask, cts));
                    }

                    TryLog(DispatchLogLevel.Info, "ScheduledTask",
                        $"已启动 {tasks.Count} 个定时任务。");
                }

                TryLog(
                    DispatchLogLevel.Info,
                    "Runner",
                    $"TaskDispatchManager 已启动，consumerCount={consumerCount}, capacity={_options.Capacity}");

                RaiseStatisticsChanged();
            }
        }

        public async Task<TaskDispatchToggleResult> ToggleAsync(
            TaskDispatchStopOptions? stopOptions = null)
        {
            var state = State;

            if (state == RunnerState.Running || state == RunnerState.Stopping)
            {
                var stopResult = await StopAsync(stopOptions).ConfigureAwait(false);

                return new TaskDispatchToggleResult
                {
                    Action = TaskDispatchToggleAction.Stopped,
                    State = State,
                    StopResult = stopResult
                };
            }

            await StartAsync().ConfigureAwait(false);

            return new TaskDispatchToggleResult
            {
                Action = TaskDispatchToggleAction.Started,
                State = State
            };
        }

        public Task<TaskDispatchStopResult> StopAsync()
        {
            return StopAsync(null);
        }

        public Task<TaskDispatchStopResult> StopAsync(TaskDispatchStopOptions? stopOptions)
        {
            Task<TaskDispatchStopResult> taskToWait;

            lock (_syncRoot)
            {
                if (_state == RunnerState.Stopped)
                {
                    return Task.FromResult(new TaskDispatchStopResult
                    {
                        TimedOut = false,
                        PendingCount = 0,
                        PersistedCount = 0,
                        PersistenceFilePath = null,
                        PendingTasks = Array.Empty<JToken>()
                    });
                }

                if (_stopTask != null)
                    return _stopTask;

                var options = stopOptions ?? new TaskDispatchStopOptions();

                if (_state != RunnerState.Faulted)
                    SetState(RunnerState.Stopping);

                TryLog(DispatchLogLevel.Info, "Runner", "TaskDispatchManager 开始停止。");

                TryCancel();

                // 取消所有定时任务
                foreach (var run in _scheduledRuns)
                {
                    try
                    {
                        run.Cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                _channel.Writer.TryComplete();

                var tasks = new List<Task>();

                if (_producerTask != null)
                    tasks.Add(_producerTask);

                if (_consumerTasks.Count > 0)
                    tasks.AddRange(_consumerTasks);

                // 将定时任务也加入等待列表
                foreach (var run in _scheduledRuns)
                {
                    if (!run.RunTask.IsCompleted)
                        tasks.Add(run.RunTask);
                }

                _stopTask = StopCoreAsync(tasks, options);
                taskToWait = _stopTask;
            }

            return taskToWait;
        }

        private void CreateNewChannel()
        {
            var channelOptions = new BoundedChannelOptions(_options.Capacity)
            {
                FullMode = _options.FullMode,
                SingleWriter = _options.SingleWriter,
                SingleReader = _options.SingleReader,
                AllowSynchronousContinuations = _options.AllowSynchronousContinuations
            };

            _channel = Channel.CreateBounded<JToken>(channelOptions);
            _writer = new NotifyingChannelWriter(_channel.Writer, OnTaskWritten);
        }

        private async Task RunProducerFlowAsync(
            Func<ChannelWriter<JToken>, CancellationToken, Task> producer,
            CancellationToken token)
        {
            try
            {
                if (_options.LoadPersistedOnStart)
                {
                    await LoadPersistedTasksIfNeededAsync(token).ConfigureAwait(false);
                }

                await producer(_writer, token).ConfigureAwait(false);

                TryLog(DispatchLogLevel.Info, "Producer", "Producer 已正常结束。");

                if (_options.AutoCompleteWriterWhenProducerEnds)
                    _channel.Writer.TryComplete();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                TryLog(DispatchLogLevel.Info, "Producer", "Producer 已取消。");

                if (_options.AutoCompleteWriterWhenProducerEnds)
                    _channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                TryLog(DispatchLogLevel.Error, "Producer", "Producer 异常。", ex);

                if (_options.FaultOnProducerException)
                    Fault(ex);

                TryCancel();

                _channel.Writer.TryComplete(ex);
            }
        }

        private async Task RunConsumerLoopAsync(
            int consumerId,
            Func<int, JToken, CancellationToken, Task> consumer,
            CancellationToken token)
        {
            try
            {
                TryLog(DispatchLogLevel.Info, $"Consumer-{consumerId}", "Consumer 已启动。");

                while (await _channel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out var item))
                    {
                        Interlocked.Increment(ref _dequeuedCount);
                        RaiseTaskEvent(TaskDequeued, DispatchTaskEventKind.Dequeued, item, consumerId);
                        RaiseStatisticsChanged();

                        if (token.IsCancellationRequested)
                        {
                            Interlocked.Increment(ref _canceledCount);
                            RaiseTaskEvent(TaskCanceled, DispatchTaskEventKind.Canceled, item, consumerId);
                            RaiseStatisticsChanged();
                            return;
                        }

                        await ExecuteOneAsync(consumerId, item, consumer, token).ConfigureAwait(false);
                    }
                }

                TryLog(DispatchLogLevel.Info, $"Consumer-{consumerId}", "Consumer 已正常结束。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                TryLog(DispatchLogLevel.Info, $"Consumer-{consumerId}", "Consumer 已取消。");
            }
            catch (ChannelClosedException)
            {
                TryLog(DispatchLogLevel.Info, $"Consumer-{consumerId}", "Channel 已关闭。");
            }
            catch (Exception ex)
            {
                var wrapped = new Exception($"Consumer-{consumerId} 循环异常退出。", ex);

                TryLog(DispatchLogLevel.Error, $"Consumer-{consumerId}", "Consumer 循环异常。", wrapped);

                if (_options.FaultOnConsumerLoopException)
                    Fault(wrapped);

                TryCancel();
                _channel.Writer.TryComplete(wrapped);
            }
        }

        private async Task ExecuteOneAsync(
            int consumerId,
            JToken item,
            Func<int, JToken, CancellationToken, Task> consumer,
            CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            Interlocked.Increment(ref _startedCount);
            RaiseTaskEvent(TaskStarted, DispatchTaskEventKind.Started, item, consumerId);
            RaiseStatisticsChanged();

            try
            {
                await consumer(consumerId, item, token).ConfigureAwait(false);

                sw.Stop();

                Interlocked.Increment(ref _succeededCount);
                RaiseTaskEvent(
                    TaskSucceeded,
                    DispatchTaskEventKind.Succeeded,
                    item,
                    consumerId,
                    elapsed: sw.Elapsed);

                RaiseStatisticsChanged();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                sw.Stop();

                Interlocked.Increment(ref _canceledCount);
                RaiseTaskEvent(
                    TaskCanceled,
                    DispatchTaskEventKind.Canceled,
                    item,
                    consumerId,
                    elapsed: sw.Elapsed);

                RaiseStatisticsChanged();
            }
            catch (Exception ex)
            {
                sw.Stop();

                Interlocked.Increment(ref _failedCount);

                RaiseTaskEvent(
                    TaskFailed,
                    DispatchTaskEventKind.Failed,
                    item,
                    consumerId,
                    ex,
                    sw.Elapsed);

                TryLog(
                    DispatchLogLevel.Error,
                    $"Consumer-{consumerId}",
                    $"任务执行失败，TaskId={GetTaskId(item)}, Elapsed={sw.ElapsedMilliseconds}ms",
                    ex);

                RaiseStatisticsChanged();

                if (!_options.ContinueOnTaskError)
                {
                    Fault(new Exception($"任务执行失败，TaskId={GetTaskId(item)}", ex));
                    TryCancel();
                    _channel.Writer.TryComplete(ex);
                }
            }
        }

        /// <summary>
        /// 定时任务循环。每个定时任务独立运行此方法，按配置的 Interval 周期执行回调或接口。
        /// </summary>
        private async Task RunScheduledLoopAsync(
            ScheduledTaskOptions options,
            CancellationToken token)
        {
            var name = options.Name ?? "(未命名)";
            int executionCount = 0;

            TryLog(DispatchLogLevel.Info, "ScheduledTask",
                $"定时任务 [{name}] 已启动，interval={options.Interval.TotalSeconds:F1}s");

            while (!token.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    // 优先使用委托，其次使用接口
                    if (options.Callback != null)
                    {
                        await options.Callback(token).ConfigureAwait(false);
                    }
                    else if (options.Task != null)
                    {
                        await options.Task.ExecuteAsync(token).ConfigureAwait(false);
                    }

                    sw.Stop();
                    var count = Interlocked.Increment(ref executionCount);
                    Interlocked.Increment(ref _scheduledExecutionCount);

                    RaiseScheduledTaskExecuted(name, sw.Elapsed, count);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    var count = Interlocked.Increment(ref executionCount);
                    Interlocked.Increment(ref _scheduledExecutionCount);

                    RaiseScheduledTaskFailed(name, ex, sw.Elapsed, count);

                    TryLog(
                        DispatchLogLevel.Error,
                        "ScheduledTask",
                        $"定时任务 [{name}] 执行异常，count={count}, elapsed={sw.ElapsedMilliseconds}ms",
                        ex);

                    if (!options.ContinueOnError)
                    {
                        TryLog(
                            DispatchLogLevel.Warning,
                            "ScheduledTask",
                            $"定时任务 [{name}] 因 ContinueOnError=false 停止调度。");
                        break;
                    }
                }

                // 等待下一个周期
                try
                {
                    await Task.Delay(options.Interval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            TryLog(DispatchLogLevel.Info, "ScheduledTask",
                $"定时任务 [{name}] 已停止，totalExecutions={executionCount}");
        }

        private async Task<TaskDispatchStopResult> StopCoreAsync(
            List<Task> tasks,
            TaskDispatchStopOptions stopOptions)
        {
            bool timedOut = false;
            var pending = new List<JToken>();
            int persistedCount = 0;
            string? persistenceFilePath = null;

            try
            {
                var timeout = stopOptions.Timeout ?? _options.DefaultStopTimeout;

                if (timeout <= TimeSpan.Zero)
                    timeout = TimeSpan.FromSeconds(10);

                if (tasks.Count > 0)
                {
                    var allTask = Task.WhenAll(tasks);
                    var delayTask = Task.Delay(timeout);

                    var completedTask = await Task.WhenAny(allTask, delayTask).ConfigureAwait(false);

                    if (completedTask == delayTask)
                    {
                        timedOut = true;

                        TryLog(
                            DispatchLogLevel.Warning,
                            "Runner",
                            $"TaskDispatchManager 停止超时，timeout={timeout.TotalMilliseconds}ms");
                    }
                    else
                    {
                        try
                        {
                            await allTask.ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            TryLog(
                                DispatchLogLevel.Error,
                                "Runner",
                                "等待任务停止时发生异常。",
                                ex);

                            Fault(ex);
                        }
                    }
                }

                pending = DrainPending();

                if (pending.Count > 0)
                {
                    foreach (var item in pending)
                    {
                        Interlocked.Increment(ref _droppedCount);
                        RaiseTaskEvent(TaskDropped, DispatchTaskEventKind.Dropped, item);
                    }

                    RaiseStatisticsChanged();
                }

                var shouldPersist = stopOptions.PersistPending ?? _options.PersistPendingOnStop;

                if (shouldPersist && pending.Count > 0)
                {
                    persistenceFilePath = string.IsNullOrWhiteSpace(stopOptions.PersistenceFilePath)
                        ? _options.PersistenceFilePath
                        : stopOptions.PersistenceFilePath;

                    await PersistPendingTasksAsync(pending, persistenceFilePath).ConfigureAwait(false);

                    persistedCount = pending.Count;

                    RaisePendingTasksPersisted(persistenceFilePath, persistedCount);
                }

                TryLog(
                    DispatchLogLevel.Info,
                    "Runner",
                    $"TaskDispatchManager 已停止，timedOut={timedOut}, pending={pending.Count}, persisted={persistedCount}");
            }
            finally
            {
                CleanupState();
            }

            return new TaskDispatchStopResult
            {
                TimedOut = timedOut,
                PendingCount = pending.Count,
                PersistedCount = persistedCount,
                PersistenceFilePath = persistenceFilePath,
                PendingTasks = pending
            };
        }

        public List<JToken> DrainPending()
        {
            var remaining = new List<JToken>();

            while (_channel.Reader.TryRead(out var item))
            {
                remaining.Add(item);
            }

            return remaining;
        }

        private async Task LoadPersistedTasksIfNeededAsync(CancellationToken token)
        {
            var path = _options.PersistenceFilePath;

            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!File.Exists(path))
                return;

            var loadingPath = path + ".loading";

            try
            {
                if (File.Exists(loadingPath))
                {
                    try
                    {
                        File.Delete(loadingPath);
                    }
                    catch
                    {
                    }
                }

                // 先改名，避免原文件被下次重复读取
                File.Move(path, loadingPath);

                var tasks = await ReadPersistedTasksAsync(loadingPath, token)
                    .ConfigureAwait(false);

                if (tasks.Count <= 0)
                {
                    TryDeleteFile(loadingPath);
                    return;
                }

                foreach (var item in tasks)
                {
                    token.ThrowIfCancellationRequested();

                    await _writer.WriteAsync(item, token).ConfigureAwait(false);
                }

                TryLog(
                    DispatchLogLevel.Info,
                    "Persistence",
                    $"已加载落盘任务，count={tasks.Count}, file={loadingPath}");

                RaisePersistedTasksLoaded(loadingPath, tasks.Count);

                if (_options.DeletePersistenceFileAfterLoad)
                {
                    TryDeleteFile(loadingPath);

                    TryLog(
                        DispatchLogLevel.Info,
                        "Persistence",
                        $"已删除落盘任务文件，file={loadingPath}");
                }
            }
            catch (Exception ex)
            {
                TryLog(
                    DispatchLogLevel.Error,
                    "Persistence",
                    $"加载落盘任务失败，file={path}",
                    ex);

                if (!_options.IgnorePersistenceErrors)
                    throw;
            }
        }
        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private async Task<List<JToken>> ReadPersistedTasksAsync(
            string path,
            CancellationToken token)
        {
            var text = await File.ReadAllTextAsync(path, Encoding.UTF8, token)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
                return new List<JToken>();

            try
            {
                var parsed = JToken.Parse(text);

                if (parsed is JArray arr)
                    return arr.Select(x => x.DeepClone()).ToList();

                return new List<JToken> { parsed.DeepClone() };
            }
            catch
            {
                var list = new List<JToken>();

                using var reader = new StringReader(text);

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    list.Add(JToken.Parse(line));
                }

                return list;
            }
        }

        private async Task PersistPendingTasksAsync(
            IReadOnlyList<JToken> tasks,
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                var directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var array = new JArray(tasks.Select(x => x.DeepClone()));
                var json = array.ToString(_options.PersistenceFormatting);

                var tempFile = filePath + ".tmp";

                await File.WriteAllTextAsync(tempFile, json, Encoding.UTF8)
                    .ConfigureAwait(false);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempFile, filePath);

                TryLog(
                    DispatchLogLevel.Info,
                    "Persistence",
                    $"剩余任务已落盘，count={tasks.Count}, file={filePath}");
            }
            catch (Exception ex)
            {
                TryLog(
                    DispatchLogLevel.Error,
                    "Persistence",
                    $"剩余任务落盘失败，file={filePath}",
                    ex);

                if (!_options.IgnorePersistenceErrors)
                    throw;
            }
        }

        private void OnTaskWritten(JToken item)
        {
            Interlocked.Increment(ref _enqueuedCount);
            RaiseTaskEvent(TaskEnqueued, DispatchTaskEventKind.Enqueued, item);
            RaiseStatisticsChanged();
        }

        private void TryCancel()
        {
            try
            {
                _internalCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void CleanupState()
        {
            lock (_syncRoot)
            {
                _externalTokenRegistration.Dispose();

                CleanupTokenOnly_NoLock();

                _producerTask = null;
                _consumerTasks.Clear();

                // 清理定时任务
                foreach (var run in _scheduledRuns)
                {
                    try
                    {
                        run.Cts.Dispose();
                    }
                    catch
                    {
                    }
                }

                _scheduledRuns.Clear();

                if (_stopwatch.IsRunning)
                    _stopwatch.Stop();

                _stoppedAt = DateTimeOffset.Now;

                SetState(RunnerState.Stopped);

                _stopTask = null;

                RaiseStatisticsChanged();
            }
        }

        private void CleanupTokenOnly_NoLock()
        {
            // 防御性清理定时任务 CTS（通常此时列表应为空）
            foreach (var run in _scheduledRuns)
            {
                try
                {
                    run.Cts.Dispose();
                }
                catch
                {
                }
            }

            try
            {
                _externalTokenRegistration.Dispose();
            }
            catch
            {
            }

            try
            {
                _internalCts?.Dispose();
            }
            catch
            {
            }

            _internalCts = null;
            _externalTokenRegistration = default;
        }

        private void ResetCounters_NoLock()
        {
            _enqueuedCount = 0;
            _dequeuedCount = 0;
            _startedCount = 0;
            _succeededCount = 0;
            _failedCount = 0;
            _canceledCount = 0;
            _droppedCount = 0;
        }

        private void Fault(Exception ex)
        {
            _lastException = ex;

            TryLog(
                DispatchLogLevel.Error,
                "Runner",
                "TaskDispatchManager 进入 Faulted 状态。",
                ex);

            SetState(RunnerState.Faulted, ex);
            RaiseFaulted(ex);
        }

        private void SetState(RunnerState newState, Exception? exception = null)
        {
            RunnerState oldState;

            lock (_syncRoot)
            {
                oldState = _state;

                if (oldState == newState)
                    return;

                _state = newState;
            }

            RaiseStateChanged(oldState, newState, exception);
        }

        private TaskDispatchSnapshot GetSnapshot()
        {
            int queueCount = 0;

            try
            {
                if (_channel.Reader.CanCount)
                    queueCount = _channel.Reader.Count;
            }
            catch
            {
                queueCount = 0;
            }

            return new TaskDispatchSnapshot
            {
                State = _state,
                ConsumerCount = ConsumerCount,
                QueueCount = queueCount,
                EnqueuedCount = Interlocked.Read(ref _enqueuedCount),
                DequeuedCount = Interlocked.Read(ref _dequeuedCount),
                StartedCount = Interlocked.Read(ref _startedCount),
                SucceededCount = Interlocked.Read(ref _succeededCount),
                FailedCount = Interlocked.Read(ref _failedCount),
                CanceledCount = Interlocked.Read(ref _canceledCount),
                DroppedCount = Interlocked.Read(ref _droppedCount),
                StartedAt = _startedAt,
                StoppedAt = _stoppedAt,
                RunElapsed = _stopwatch.Elapsed,
                LastException = _lastException
            };
        }

        public static string? GetTaskId(JToken item)
        {
            try
            {
                return item["id"]?.ToString()
                       ?? item["task_id"]?.ToString()
                       ?? item["TaskId"]?.ToString()
                       ?? item["uuid"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private void TryLog(
            DispatchLogLevel level,
            string source,
            string message,
            Exception? ex = null)
        {
            try
            {
                LogEmitted?.Invoke(this, new DispatchLogEventArgs(level, source, message, ex));
            }
            catch
            {
            }
        }

        private void RaiseStateChanged(
            RunnerState oldState,
            RunnerState newState,
            Exception? exception)
        {
            try
            {
                StateChanged?.Invoke(
                    this,
                    new RunnerStateChangedEventArgs(oldState, newState, exception));
            }
            catch
            {
            }
        }

        private void RaiseFaulted(Exception ex)
        {
            try
            {
                Faulted?.Invoke(this, ex);
            }
            catch
            {
            }
        }

        private void RaiseTaskEvent(
            EventHandler<DispatchTaskEventArgs>? handler,
            DispatchTaskEventKind kind,
            JToken item,
            int? consumerId = null,
            Exception? exception = null,
            TimeSpan? elapsed = null)
        {
            try
            {
                handler?.Invoke(
                    this,
                    new DispatchTaskEventArgs(
                        kind,
                        item,
                        consumerId,
                        exception,
                        elapsed));
            }
            catch
            {
            }
        }

        private void RaisePendingTasksPersisted(string filePath, int count)
        {
            try
            {
                PendingTasksPersisted?.Invoke(
                    this,
                    new PendingTasksPersistedEventArgs(filePath, count));
            }
            catch
            {
            }
        }

        private void RaisePersistedTasksLoaded(string filePath, int count)
        {
            try
            {
                PersistedTasksLoaded?.Invoke(
                    this,
                    new PersistedTasksLoadedEventArgs(filePath, count));
            }
            catch
            {
            }
        }

        private void RaiseStatisticsChanged()
        {
            try
            {
                StatisticsChanged?.Invoke(this, GetSnapshot());
            }
            catch
            {
            }
        }

        private void RaiseScheduledTaskExecuted(
            string? taskName,
            TimeSpan elapsed,
            int executionCount)
        {
            try
            {
                ScheduledTaskExecuted?.Invoke(
                    this,
                    new ScheduledTaskExecutedEventArgs(taskName, elapsed, executionCount));
            }
            catch
            {
            }
        }

        private void RaiseScheduledTaskFailed(
            string? taskName,
            Exception exception,
            TimeSpan elapsed,
            int executionCount)
        {
            try
            {
                ScheduledTaskFailed?.Invoke(
                    this,
                    new ScheduledTaskFailedEventArgs(taskName, exception, elapsed, executionCount));
            }
            catch
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(new TaskDispatchStopOptions
            {
                PersistPending = _options.PersistPendingOnStop,
                Timeout = _options.DefaultStopTimeout
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 定时任务运行状态，内部使用。
        /// </summary>
        private sealed class ScheduledTaskRun
        {
            public ScheduledTaskRun(
                ScheduledTaskOptions options,
                Task runTask,
                CancellationTokenSource cts)
            {
                Options = options;
                RunTask = runTask;
                Cts = cts;
            }

            public ScheduledTaskOptions Options { get; }

            public Task RunTask { get; }

            public CancellationTokenSource Cts { get; }
        }

        private sealed class NotifyingChannelWriter : ChannelWriter<JToken>
        {
            private readonly ChannelWriter<JToken> _inner;
            private readonly Action<JToken> _onWritten;

            public NotifyingChannelWriter(
                ChannelWriter<JToken> inner,
                Action<JToken> onWritten)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _onWritten = onWritten ?? throw new ArgumentNullException(nameof(onWritten));
            }

            public override bool TryComplete(Exception? error = null)
            {
                return _inner.TryComplete(error);
            }

            public override bool TryWrite(JToken item)
            {
                var written = _inner.TryWrite(item);

                if (written)
                    SafeOnWritten(item);

                return written;
            }

            public override ValueTask<bool> WaitToWriteAsync(
                CancellationToken cancellationToken = default)
            {
                return _inner.WaitToWriteAsync(cancellationToken);
            }

            public override async ValueTask WriteAsync(
                JToken item,
                CancellationToken cancellationToken = default)
            {
                await _inner.WriteAsync(item, cancellationToken)
                    .ConfigureAwait(false);

                SafeOnWritten(item);
            }

            private void SafeOnWritten(JToken item)
            {
                try
                {
                    _onWritten(item);
                }
                catch
                {
                    // 写入成功后，事件异常不能影响 Channel。
                }
            }
        }
    }
}
