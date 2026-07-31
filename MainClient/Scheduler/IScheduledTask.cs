using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Scheduler
{
    /// <summary>
    /// 定时任务接口。实现此接口的类可被 TaskDispatchManager 按周期调度执行。
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// 定时执行的方法。调度器停止时会通过 cancellationToken 发出取消信号。
        /// </summary>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

}
