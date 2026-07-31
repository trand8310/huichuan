using MainClient.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MainClient.Common
{
    public enum IPFormat
    {
        TXT = 1,
        JSON = 2,
    }
    public class IpEntity
    {
        public string value { get; set; } = string.Empty;
        public JToken json { get; set; }
        public IPFormat format { get; set; } = IPFormat.TXT;
    }



    public class IpHelper
    {
        private static JArray region_1;
        private static JArray region_2;
        private static JArray region_3;
        private static JArray region_4_1;
        private static JArray region_4_2;
        private static JArray region_ipzan;
        static string[] delimiters = { "\r", "\n", System.Environment.NewLine };
        static SemaphoreSlim _mutex = new SemaphoreSlim(1);
        static IpHelper()
        {
            region_1 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_1);
            region_2 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_2);
            region_3 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_3);
            region_4_1 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_4_1);
            region_4_2 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_4_2);
            region_ipzan = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_ipzan);
        }
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public IpHelper(AppSettings appSettings, IHttpClientFactory httpClientFactory, ILogger<IpHelper> logger)
        {
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private static ConcurrentQueue<IpEntity> ipQueues = new ConcurrentQueue<IpEntity>();
        public async Task<IpEntity> GetProxyIpAsync(JToken task, int count = 0)
        {
            if (ipQueues.TryDequeue(out var value))
            {
                return value;
            }
            IPFormat iPFormat = IPFormat.TXT;
            var url = GetIpUrl(task, out iPFormat, count);
            var client = _httpClientFactory.CreateClient("IP_DATA");
            try
            {
                await _mutex.WaitAsync();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content) && !content.Contains("白名单") && !content.Contains("暂无") && !content.Contains("没有")  && !content.Contains("过多") )
                    {
                        if (iPFormat == IPFormat.TXT)
                        {
                            var values = content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var text in values)
                            {
                                ipQueues.Enqueue(new IpEntity() { format = iPFormat, value = text });
                            }
                        }
                        else if (iPFormat == IPFormat.JSON)
                        {
                            var json = JObject.Parse(content);
                            foreach (var data in json.SelectToken("data").Children())
                            {
                                ipQueues.Enqueue(new IpEntity() { format = iPFormat, json = data });
                            }
                        }

                        if (ipQueues.TryDequeue(out var entity))
                        {
                            return entity;
                        }
                    }
                    throw new Exception(content);

                }
            }
            catch
            {
                throw;
                //_logger.LogError($"GetIpUrl => {url},{ex.InnerException?.Message}");
            }
            finally
            {
                _mutex.Release();
            }
            return null;
        }


        private string GetIpUrl(JToken task, out IPFormat format, int count = 0)
        {
            format = IPFormat.TXT;
            var url = _appSettings.ProxyIpUrl;
            try
            {
                //四川[18]:成都[188],
                var query = System.Web.HttpUtility.ParseQueryString(url);
                if (url.Contains("api.test.myipproxy.com") || url.Contains("api.hailiangip.com") || url.Contains("111.73.45.100") || url.Contains("47.97.20.179"))
                {
                    //http://api.test.myipproxy.com:8422/api/getIp?type=1&num=1&orderId=O21081016192288073951&time=1628583680&sign=95d2880db7a7effe459df80ee80ba249&unbindTime=180&dataType=1&pid=&cid=
                    #region myipproxy & hailiangip & ...

                    if (query["dataType"] != null && Int32.TryParse(query["dataType"].ToString(), out int dataType) && dataType > 0)
                        format = IPFormat.TXT;
                    else
                        format = IPFormat.JSON;

                    if (count > 0)
                    {
                        if (Regex.IsMatch(url, @"num=[\d]*"))
                            url = Regex.Replace(url, @"num=[\d]*", $"num={count}");
                        else
                            url = url += $"&num={count}";
                    }

                    if (_appSettings.RealIp)
                    {
                        format = IPFormat.JSON;
                        if (Regex.IsMatch(url, @"dataType=[\d]*"))
                            url = Regex.Replace(url, @"dataType=[\d]*", $"dataType=0");
                        else
                            url = url += $"&dataType=0";
                    }
                    else
                    {
                        if (Regex.IsMatch(url, @"dataType=[\d]*"))
                            url = Regex.Replace(url, @"dataType=[\d]*", $"dataType=1");
                        else
                            url = url += $"&dataType=1";
                    }

                    if (task["address"] != null && !string.IsNullOrEmpty(task["address"].ToString()) && !task["address"].ToString().Equals("全部"))
                    {
                        var addrs = task["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\d+");
                        string pid = string.Empty, cid;
                        if (m1.Success)
                        {
                            pid = m1.Value;
                            if (Regex.IsMatch(url, @"pid=[\d]*"))
                                url = Regex.Replace(url, @"pid=[\d]*", $"pid={pid}");
                            else
                                url = url += $"&pid={pid}";
                        }
                        if (address.Count() > 1)
                        {
                            var m2 = Regex.Match(address[1], @"\d+");
                            if (m2.Success)
                            {
                                cid = m2.Value;
                                if (!string.IsNullOrWhiteSpace(pid) && !pid.Equals(cid))
                                {
                                    if (Regex.IsMatch(url, @"cid=[\d]*"))
                                        url = Regex.Replace(url, @"cid=[\d]*", $"cid={cid}");
                                    else
                                        url = url += $"&cid={cid}";
                                }
                            }
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return url;
        }




        public async Task<string> GetIpInfo(string proxy)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler() { Proxy = new WebProxy(proxy, BypassOnLocal: false), UseProxy = true };
            using (var client = new HttpClient(httpClientHandler))
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    HttpResponseMessage response = await client.GetAsync("http://ip-api.com/json");
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            };
            return await ipinfo_json(proxy);
        }
        private async Task<string> ipinfo_json(string proxy)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler() { Proxy = new WebProxy(proxy, BypassOnLocal: false), UseProxy = true };
            using (var client = new HttpClient(httpClientHandler))
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(data);
                        var loc = json["loc"].Value<string>().Split(',');
                        var new_json = new JObject();
                        new_json["status"] = "success";
                        new_json["country"] = json["country"];
                        new_json["region"] = json["region"];
                        new_json["city"] = json["city"];
                        new_json["lat"] = double.Parse(loc[0]);
                        new_json["lon"] = double.Parse(loc[1]);
                        new_json["timezone"] = json["timezone"];
                        new_json["query"] = json["ip"];
                        return JsonConvert.SerializeObject(new_json, Formatting.None);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            };
            return null;
        }







        public async Task<bool> PingIP(string proxy_server)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 1000;
            PingReply reply = await pingSender.SendPingAsync(proxy_server, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
            return false;
        }



        #region  Ip操作

        private static readonly string[] _ipApiUrls =
        {
            "http://211.154.24.179:9000/api/dash/ipinfo.php",
            "http://117.21.200.18:9000/api/dash/ipinfo.php",
            "http://117.21.200.221/api/dash/ipinfo.php",
            "http://ip-api.com/json/?lang=zh-CN",
            "https://ipinfo.io/json",
        };

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            UseProxy = false
        })
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// 判断是否内网IP
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static bool IsPrivateIPv4(IPAddress ip)
        {
            byte[] b = ip.GetAddressBytes();

            return
                b[0] == 10 ||
                (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                (b[0] == 192 && b[1] == 168) ||
                (b[0] == 169 && b[1] == 254) || // APIPA
                b[0] == 127;
        }

        /// <summary>
        /// 从单个接口获取 IP
        /// </summary>
        private static async Task<string> GetIpFromApiAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            var data = JsonConvert.DeserializeObject<IpInfoResponse>(json);

            if (data == null)
                return string.Empty;

            if (!string.Equals(data.Status, "success", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return data.Query?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 并发请求多个 IP 接口，哪个先成功返回就用哪个
        /// </summary>
        private static async Task<string> GetRealIpAsync(CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(6));

            var tasks = _ipApiUrls
                .Select(url => GetIpFromApiAsync(url, cts.Token))
                .ToList();

            while (tasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(tasks);
                tasks.Remove(finishedTask);

                try
                {
                    var ip = await finishedTask;
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        // 有一个成功了，取消其他请求
                        cts.Cancel();
                        return ip;
                    }
                }
                catch
                {
                    // 当前这个接口失败，继续等其他接口
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取本机网卡的IP地址
        /// </summary>
        /// <returns></returns>
        private static List<string> GetPublicIPv4Addresses()
        {
            var result = new List<string>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 必须启用
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // 排除虚拟/隧道/回环
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                // 必须有网关（否则一般是虚拟或离线网卡）
                var props = ni.GetIPProperties();
                if (!props.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(g.Address)))
                    continue;

                foreach (var ua in props.UnicastAddresses)
                {
                    var ip = ua.Address;

                    if (ip.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IsPrivateIPv4(ip))
                        continue;

                    result.Add(ip.ToString());
                }
            }

            return result;
        }
        private sealed class IpInfoResponse
        {
            public string? Status { get; set; }
            public string? Country { get; set; }
            public string? CountryCode { get; set; }
            public string? Province { get; set; }
            public string? City { get; set; }
            public string? District { get; set; }
            public string? Isp { get; set; }
            public string? Areacode { get; set; }
            public string? Lat { get; set; }
            public string? Lon { get; set; }
            public string? Query { get; set; }
        }

        private static string? _hostCache;
        private static readonly SemaphoreSlim _host_lock = new(1, 1);
        public static async Task<string> GetLocalHostAsync()
        {
            // 快速路径（无锁）
            if (!string.IsNullOrWhiteSpace(_hostCache))
                return _hostCache;
            await _host_lock.WaitAsync();
            try
            {
                // 双重检查
                if (!string.IsNullOrWhiteSpace(_hostCache))
                    return _hostCache;
                // ① 先尝试本机公网 IPv4
                try
                {
                    var localIp = GetPublicIPv4Addresses().FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(localIp))
                    {
                        _hostCache = localIp;
                        return _hostCache;
                    }
                }
                catch { }
                // ② 请求外部接口获取公网 IP
                try
                {
                    var realIp = await GetRealIpAsync();
                    if (!string.IsNullOrWhiteSpace(realIp))
                    {
                        _hostCache = realIp;
                        return _hostCache;
                    }
                }
                catch { }
                // ③ 最终兜底
                _hostCache = "";
                return _hostCache;
            }
            finally
            {
                _host_lock.Release();
            }
        }
        #endregion

    }
}
