using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{

    /// <summary>
    /// 单个定时任务的配置。
    /// </summary>
    public sealed class ScheduledTaskOptions
    {
        /// <summary>任务名称（用于日志和事件标识）。</summary>
        public string? Name { get; set; }

        /// <summary>执行间隔。必须大于 TimeSpan.Zero。</summary>
        public TimeSpan Interval { get; set; }

        /// <summary>委托方式：直接传回调（与 Task 属性二选一，优先使用 Callback）。</summary>
        public Func<CancellationToken, Task>? Callback { get; set; }

        /// <summary>接口方式：传入 IScheduledTask 实例（与 Callback 二选一）。</summary>
        public IScheduledTask? Task { get; set; }

        /// <summary>单次执行异常是否继续后续调度。默认 true。</summary>
        public bool ContinueOnError { get; set; } = true;
    }

}
