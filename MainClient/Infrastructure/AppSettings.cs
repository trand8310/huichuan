
namespace MainClient.Infrastructure
{
    public class AppSettings
    {
        public string ProxyIpUrl { get; set; }
        public string TaskApiUrl { get; set; }
        public string UpdateApiUrl { get; set; }
        public string DevApiUrl { get; set; }

        /// <summary>
        /// 任务提取间隔
        /// </summary>
        public int TaskPullIntervalMs { get; set; } = 1000;
 
        

        /// <summary>
        /// 单UV执行间隔
        /// </summary>
        public int UvExecutionIntervalMs { get; set; } = 1000;

        /// <summary>
        /// 并发数量
        /// </summary>
        public int MaxConcurrency { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public int PageLoadTimeout { get; set; }

        public string TaskName { get; set; }
        public bool IsHiddenMode { get; set; }
        public bool IsProxyMode { get; set; }
        public bool IsRealIp { get; set; }
        public bool IsCheckIp { get; set; }
        public int Multiple { get; set; }
        public bool RealIp { get; set; }
        /// <summary>
        /// 主进程重置
        /// </summary>
        public int MainProcessResetIntervalMinutes { get; set; }
        public int SubResetTimeout { get; set; }
        public bool SendSms { get; set; }
        public string SmsName { get; set; }
        public string SmsPhone { get; set; }
        public int SendSmsTimeout { get; set; }
        public int UsingDevIndex { get; set; }
        public bool CheckIp { get; set; }
        public bool DisableUserCache { get; set; }
        public bool IsDetailLog { get; set; }

        public int IpTtl { get; set; }
        public int DspBidPrice { get; set; }

        public bool UVsTriggerOne { get;set; }

        public bool PersistAdx { get;set; }
    }
}
