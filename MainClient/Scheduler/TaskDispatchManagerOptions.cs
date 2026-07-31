using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{

    public sealed class TaskDispatchManagerOptions
    {
        /// <summary>
        /// 队列容量。队列满了后 WriteAsync 会等待，避免无限堆内存。
        /// </summary>
        public int Capacity { get; set; } = 1000;

        /// <summary>
        /// 队列满时策略。建议使用 Wait。
        /// </summary>
        public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

        /// <summary>
        /// 单一生产者
        /// </summary>
        public bool SingleWriter { get; set; } = true;

        /// <summary>
        /// 单一消费者
        /// </summary>
        public bool SingleReader { get; set; } = false;

        public bool AllowSynchronousContinuations { get; set; } = false;

        /// <summary>
        /// Producer 正常结束后是否自动 Complete Writer。
        /// 如果 Producer 是一直循环取任务，一般保持 true 即可。
        /// </summary>
        public bool AutoCompleteWriterWhenProducerEnds { get; set; } = true;

        /// <summary>
        /// 单个任务异常是否继续整体调度。
        /// true：单任务失败只触发 TaskFailed，整体继续。
        /// false：单任务失败会让调度器 Faulted。
        /// </summary>
        public bool ContinueOnTaskError { get; set; } = true;

        /// <summary>
        /// Producer 异常是否让调度器进入 Faulted。
        /// </summary>
        public bool FaultOnProducerException { get; set; } = true;

        /// <summary>
        /// Consumer 循环异常是否让调度器进入 Faulted。
        /// </summary>
        public bool FaultOnConsumerLoopException { get; set; } = true;

        /// <summary>
        /// 默认停止超时时间。
        /// </summary>
        public TimeSpan DefaultStopTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Stop 时是否把 Channel 队列中还没被取出的任务落盘。
        /// 注意：已经被 Consumer 取出的任务不在这里。
        /// </summary>
        public bool PersistPendingOnStop { get; set; } = false;

        /// <summary>
        /// Start 时是否加载上次落盘任务。
        /// </summary>
        public bool LoadPersistedOnStart { get; set; } = false;

        /// <summary>
        /// 成功加载落盘任务后，是否删除落盘文件。
        /// </summary>
        public bool DeletePersistenceFileAfterLoad { get; set; } = true;

        /// <summary>
        /// 落盘文件路径。
        /// </summary>
        public string PersistenceFilePath { get; set; } =
            Path.Combine(AppContext.BaseDirectory, "task_dispatch_pending.json");

        /// <summary>
        /// 落盘 JSON 格式。
        /// </summary>
        public Formatting PersistenceFormatting { get; set; } = Formatting.None;

        /// <summary>
        /// 持久化异常是否只记录日志，不中断调度器。
        /// </summary>
        public bool IgnorePersistenceErrors { get; set; } = true;

        /// <summary>
        /// 定时任务列表。为空或 null 则不启动任何定时任务。
        /// 每个定时任务按各自的 Interval 周期执行，生命周期与调度器绑定。
        /// </summary>
        public IReadOnlyList<ScheduledTaskOptions> ScheduledTasks { get; set; }
            = Array.Empty<ScheduledTaskOptions>();
    }
}
