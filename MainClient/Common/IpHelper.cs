using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly IWritableOptions<AppSettings> _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public IpHelper(IWritableOptions<AppSettings> appSettings, IHttpClientFactory httpClientFactory, ILogger<IpHelper> logger)
        {
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private static ConcurrentQueue<IpEntity> ipQueues = new ConcurrentQueue<IpEntity>();
        public async Task<IpEntity> GetProxyIpAsync(JObject task, int count = 0)
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


        private string GetIpUrl(JObject task, out IPFormat format, int count = 0)
        {
            format = IPFormat.TXT;
            var url = _appSettings.Value.ProxyIpUrl;
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

                    if (_appSettings.Value.RealIp)
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



    }
}
