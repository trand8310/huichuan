

namespace MainClient.Scheduler
{
    /// <summary>
    /// 定时任务成功执行事件参数。
    /// </summary>
    public sealed class ScheduledTaskExecutedEventArgs : EventArgs
    {
        public ScheduledTaskExecutedEventArgs(
            string? taskName,
            TimeSpan elapsed,
            int executionCount)
        {
            TaskName = taskName;
            Elapsed = elapsed;
            ExecutionCount = executionCount;
            OccurredAt = DateTimeOffset.Now;
        }

        /// <summary>任务名称（来自 ScheduledTaskOptions.Name）</summary>
        public string? TaskName { get; }

        /// <summary>本次执行耗时</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>该任务累计执行次数</summary>
        public int ExecutionCount { get; }

        /// <summary>事件发生时间</summary>
        public DateTimeOffset OccurredAt { get; }
    }
}
