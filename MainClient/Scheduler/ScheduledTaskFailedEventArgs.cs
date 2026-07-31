using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{

    /// <summary>
    /// 定时任务执行失败事件参数。
    /// </summary>
    public sealed class ScheduledTaskFailedEventArgs : EventArgs
    {
        public ScheduledTaskFailedEventArgs(
            string? taskName,
            Exception exception,
            TimeSpan elapsed,
            int executionCount)
        {
            TaskName = taskName;
            Exception = exception;
            Elapsed = elapsed;
            ExecutionCount = executionCount;
            OccurredAt = DateTimeOffset.Now;
        }

        /// <summary>任务名称</summary>
        public string? TaskName { get; }

        /// <summary>异常信息</summary>
        public Exception Exception { get; }

        /// <summary>失败前的执行耗时</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>该任务累计执行次数</summary>
        public int ExecutionCount { get; }

        /// <summary>事件发生时间</summary>
        public DateTimeOffset OccurredAt { get; }
    }
}
