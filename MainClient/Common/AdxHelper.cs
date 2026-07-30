using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MainClient.Common
{
    public class AdxHelper
    {

        private readonly ILogger _logger;
        private readonly IWritableOptions<AppSettings> _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdxHelper(IWritableOptions<AppSettings> appSettings, IHttpClientFactory httpClientFactory, ILogger<AdxHelper> logger)
        {
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        /// <summary>
        /// 获取任务
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<string?> GetTaskAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using HttpResponseMessage response = await client.GetAsync(address, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取任务失败：{Address}", address);
            }
            return null;
        }


        private ConcurrentDictionary<int, TaskStatItem> task_stat_dict = new ConcurrentDictionary<int, TaskStatItem>();

        public TaskStatItem GetOrAddTaskStatus(int taskid)
        {
            return task_stat_dict.GetOrAdd(taskid, new TaskStatItem());
        }
        public TaskStatItem UpdateTaskAll(int taskid, int all = 1)
        {
            return task_stat_dict.AddOrUpdate(taskid, new TaskStatItem(), (k, v) => v.AddAllCount(all));
        }
        public TaskStatItem UpdateTaskDsp(int taskid, int dsp = 1)
        {
            return task_stat_dict.AddOrUpdate(taskid, new TaskStatItem(), (k, v) => v.AddDspCount(dsp));
        }
        public TaskStatItem UpdateTaskDspClick(int taskid, int click = 1)
        {
            return task_stat_dict.AddOrUpdate(taskid, new TaskStatItem(0), (k, v) => v.AddDspClick(click));
        }


        #region 任务更新
        /// <summary>
        /// 更新总执行数量
        /// </summary>
        /// <param name="taskid"></param>
        /// <returns></returns>
        public async Task UpdateTaskStat()
        {
            await Task.CompletedTask;
        }



        /// <summary>
        /// 更新总的点击数量
        /// </summary>
        /// <param name="taskid"></param>
        /// <returns></returns>
        public async Task<string> UpdateTaskClickNum(string taskid)
        {
            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{_appSettings.Value.TaskApiUrl}?action=update-task-click-num&taskid={taskid}&_t={System.DateTime.Now.Ticks}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 更新字段及获取值
        /// </summary>
        /// <param name="taskid"></param>
        /// <returns></returns>
        public async Task<string> UpdateTaskStateNum(string taskid, string keys, string value = "1", string fields = "*")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await client.GetAsync($"{_appSettings.Value.TaskApiUrl}?action=updateTaskstate&taskid={taskid}&keys={keys}&value={value}&fields={fields}&_t={System.DateTime.Now.Ticks}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return null;
            }

        }

        #endregion





        /// <summary>
        ///短信通知
        /// </summary>
        /// <param name="name"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        public async Task<string> SendSMS(string name, string phone)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var formData = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("name",name),
                    new KeyValuePair<string, string>("phone", phone)
                });
                var response = await client.PostAsync("http://111.73.45.147/sendsms.php", formData);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    return result;
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine(ex.Message);

            }
            return null;
        }

        static double CalculatePPI(double widthPixels, double heightPixels, double diagonalInches)
        {
            if (diagonalInches <= 0)
            {
                throw new ArgumentException("对角线尺寸必须大于 0");
            }
            // 使用勾股定理计算像素对角线长度
            double diagonalPixels = Math.Sqrt(Math.Pow(widthPixels, 2) + Math.Pow(heightPixels, 2));

            // 计算 PPI
            return diagonalPixels / diagonalInches;
        }


        //public static SemaphoreSlim _mutex = new SemaphoreSlim(System.Environment.ProcessorCount);


        /// <summary>
        /// 协议头固定长度。
        /// </summary>
        private const int HeaderLength = 16;

        /// <summary>
        /// 构造：16字节协议头 + UTF-8 JSON正文。
        /// </summary>
        public static byte[] BuildRequestPacket(
            string json,
            byte env = 0,
            byte dataType = 2,
            byte protocolVersion = 1,
            byte sdkVersion = 1,
            ushort rsaSectionCount = 0)
        {
            ArgumentNullException.ThrowIfNull(json);

            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

            byte[] packet = new byte[HeaderLength + bodyBytes.Length];

            /*
             * 协议头：
             *
             * offset 0，1 byte：ENV
             * 0 = 不加密
             * 2 = RSA加密
             */
            packet[0] = env;

            /*
             * offset 1，1 byte：DATA_TYPE
             * 2 = JSON
             */
            packet[1] = dataType;

            /*
             * offset 2，1 byte：VER
             */
            packet[2] = protocolVersion;

            /*
             * offset 3，1 byte：SDK_VER
             * 1表示1.0
             */
            packet[3] = sdkVersion;

            /*
             * offset 4，2 bytes：RSA_SEC_NUM
             * 网络字节序，即大端序。
             *
             * 未使用RSA时填0。
             */
            BinaryPrimitives.WriteUInt16BigEndian(
                packet.AsSpan(4, 2),
                rsaSectionCount);

            /*
             * offset 6，4 bytes：SOURCE_LEN
             * 未加密前的JSON正文UTF-8字节长度。
             * 网络字节序，即大端序。
             */
            BinaryPrimitives.WriteUInt32BigEndian(
                packet.AsSpan(6, 4),
                checked((uint)bodyBytes.Length));

            /*
             * offset 10～15：6字节预留字段。
             * byte[]创建时默认已经全部为0，无需手动赋值。
             */

            /*
             * offset 16开始：JSON正文。
             */
            bodyBytes.CopyTo(packet.AsSpan(HeaderLength));

            return packet;
        }

        /// <summary>
        /// 发送原生广告请求。
        /// </summary>
        public static async Task<JObject?> SendAsync(
            string url,
            object bidRequest,
            string? userAgent,
            bool isProxyMode,
            string? proxyAddress,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            ArgumentNullException.ThrowIfNull(bidRequest);

            using var handler = CreateHandler(
                isProxyMode,
                proxyAddress);

            using var client = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            string json = JsonConvert.SerializeObject(bidRequest);

            byte[] packet = BuildRequestPacket(
                json: json,

                // 0表示不加密
                env: 0,

                // 2表示JSON
                dataType: 2,

                // 协议版本
                protocolVersion: 1,

                // SDK 1.0
                sdkVersion: 1,

                // 不使用RSA，分段数为0
                rsaSectionCount: 0);

            using var content = new ByteArrayContent(packet);

            // 保持与Java示例一致。
            content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                url)
            {
                Content = content,

                // 对应Java中的HttpVersion.HTTP_1_0
                Version = HttpVersion.Version10,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                request.Headers.TryAddWithoutValidation(
                    "User-Agent",
                    userAgent);
            }

            using HttpResponseMessage response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            byte[] responseBytes =
                await response.Content.ReadAsByteArrayAsync(
                    timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                string errorText = TryReadResponseText(responseBytes);

                throw new HttpRequestException(
                    $"请求失败，HTTP状态码：{(int)response.StatusCode} " +
                    $"{response.StatusCode}，响应内容：{errorText}");
            }

            if (responseBytes.Length <= HeaderLength)
            {
                return null;
            }

            // 跳过响应前面的16字节协议头。
            string responseJson = Encoding.UTF8.GetString(
                responseBytes,
                HeaderLength,
                responseBytes.Length - HeaderLength);

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return null;
            }

            return JObject.Parse(responseJson);
        }

        private static HttpClientHandler CreateHandler(
            bool isProxyMode,
            string? proxyAddress)
        {
            var handler = new HttpClientHandler
            {
                UseProxy = isProxyMode,

                MaxConnectionsPerServer = 50,

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate |
                    DecompressionMethods.Brotli
            };

            if (!isProxyMode)
            {
                return handler;
            }

            if (string.IsNullOrWhiteSpace(proxyAddress))
            {
                throw new ArgumentException(
                    "已经开启代理模式，但代理地址为空。",
                    nameof(proxyAddress));
            }

            var webProxy = new WebProxy(
                proxyAddress,
                BypassOnLocal: false);


            handler.Proxy = webProxy;

            return handler;
        }

        /// <summary>
        /// 尝试解析带有16字节协议头的响应。
        /// 如果响应不足16字节，则直接解析全部内容。
        /// </summary>
        private static string TryReadResponseText(
            byte[] responseBytes)
        {
            if (responseBytes.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                if (responseBytes.Length > HeaderLength)
                {
                    return Encoding.UTF8.GetString(
                        responseBytes,
                        HeaderLength,
                        responseBytes.Length - HeaderLength);
                }

                return Encoding.UTF8.GetString(responseBytes);
            }
            catch
            {
                return Convert.ToHexString(responseBytes);
            }
        }


        public async Task<JObject?> GetAdRequest(JObject task, JObject adParam, JObject dev, OSType os, string realIp, string proxy, JObject ipLocation, bool isProxyMode, CancellationToken token = default)
        {
            try
            {
                var ua = dev["ua"].ToString();
                var st = CommonHelper.UnixTimeNowSecond();
                JObject bidRequest = new JObject();

                var ad_device_info = new JObject();
                var ad_app_info = new JObject();
                var request_id = Guid.NewGuid().ToString("N");


                bidRequest["request_id"] = request_id;

                ad_device_info["sw"] = dev["sw"].Value<int>();
                ad_device_info["sh"] = dev["sh"].Value<int>();
                ad_device_info["client_ip"] = realIp;

 

                if (os == OSType.ANDROID)
                {
                    var osv_values = dev["osv"].Value<string>().Split('.');

                    ad_device_info["os"] = "android";
                    ad_device_info["osv"] = dev["osv"];

                    if (int.TryParse(osv_values[0], out int first_ver) && first_ver > 10)
                    {
                        var oaid = dev["oaid"]?.Value<string>();
                        if (string.IsNullOrWhiteSpace(oaid))
                            oaid = CommonHelper.MD5Hash(Guid.NewGuid().ToString());
                        ad_device_info["oaid"] = oaid;
                        ad_device_info["oaid_md5"] = CommonHelper.MD5Hash(oaid);
                    }
                    else
                    {
                        var imei = dev["imei"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(imei))
                        {
                            ad_device_info["imei"] = imei;
                            ad_device_info["imei_md5"] = CommonHelper.MD5Hash(imei);
                        }
                        var androidid = dev["androidid"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(androidid))
                        {
                            ad_device_info["android_id"] = dev["androidid"];
                        }

                        var mac = dev["mac"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(mac))
                        {
                            ad_device_info["mac"] = mac;
                        }
                    }
                    ad_device_info["brand"] = dev["make"];
                    //ad_device_info["devid"] = dev["imei"];
                    //ad_device_info["device"] = dev["imei"];
                    ad_app_info["fr"] = "android";
                    //ad_app_info["is_ssl"] = "1";

                }
                else if (os == OSType.IOS)
                {
                    ad_device_info["os"] = "ios";
                    ad_device_info["osv"] = dev["osv"];

                    var idfa = dev["idfa"]?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(idfa))
                    {
                        ad_device_info["idfa"] = idfa;
                        ad_device_info["idfa_md5"] = CommonHelper.MD5Hash(idfa);
                    }

                    ad_app_info["fr"] = "ios";
                    //ad_app_info["is_ssl"] = "1";
                }

                ad_device_info["is_jb"] = 2;
                ad_device_info["access"] = "Wi-Fi";

                ad_app_info["app_name"] = adParam.SelectToken("ad_app_info.app_name").Value<string>();
                ad_app_info["pkg_name"] = adParam.SelectToken("ad_app_info.pkg_name").Value<string>();
                ad_app_info["pkg_ver"] = adParam.SelectToken("ad_app_info.pkg_ver").Value<string>();
                ad_app_info["category"] = adParam.SelectToken("ad_app_info.category");
                ad_app_info["ua"] = dev["ua"].Value<string>();


                //ad_app_info["app_country"] = "China";
                //ad_app_info["lang"] = "zh_cn";
                //ad_app_info["timezone"] = "Asia/Shanghai";

                bidRequest["ad_device_info"] = ad_device_info;
                bidRequest["ad_app_info"] = ad_app_info;

                #region geo
                if (ipLocation != null)
                {
                    var ad_gps_info = new JObject();
                    ad_gps_info["lat"] = ipLocation["lat"]?.Value<float>();
                    ad_gps_info["lon"] = ipLocation["lon"]?.Value<float>();
                    bidRequest["ad_gps_info"] = ad_gps_info;
                }
                #endregion

                #region ad_pos_info
                var ad_pos_info = new JObject();
                ad_pos_info["slot_id"] = adParam.SelectToken("ad_pos_info.slot_id").Value<int>();
                ad_pos_info["slot_type"] = adParam.SelectToken("ad_pos_info.slot_type").Value<int>();
                ad_pos_info["req_cnt"] = adParam.SelectToken("ad_pos_info.req_cnt").Value<int>();
                ad_pos_info["aw"] = dev["sw"].Value<int>();
                ad_pos_info["ah"] = dev["sh"].Value<int>();
                ad_pos_info["media_slot_id"] = adParam.SelectToken("ad_pos_info.media_slot_id")?.Value<string>() ?? "";

                //"slot_id":       cfg["slot_id"],
                //"slot_type":     0,
                //"req_cnt":       1,
                //"aw":            cfg["device"]["sw"],
                //"ah":            cfg["device"]["sh"],
                //"media_slot_id": cfg["media_slot_id"],

                bidRequest["ad_pos_info"] = JArray.FromObject(new[] { ad_pos_info });

                #endregion


                JObject? result = await SendAsync(
                url: task["url"]!.Value<string>()!,
                bidRequest: bidRequest,
                userAgent: ua,
                isProxyMode: isProxyMode,
                proxyAddress: proxy);

                if(result != null)
                {
                    result["request_id"] = request_id;
                }
                return result;
            }
            catch (Exception)
            {
                //Debug.WriteLine(ex.Message);
                throw;
                //_logger.LogError($"GetAdRequest => {ex.InnerException?.Message}");
            }
            finally
            {
                // _mutex.Release();
            }
            return null;
        }

    }

    public class TaskStatItem
    {
        public TaskStatItem(int all = 0, int dsp = 0, int click = 0, int pending = 0)
        {
            allCount = all;
            dspCount = dsp;
            dspClick = click;
            pendingClick = pending;
            adxCount = 0;

        }
        public int allCount;
        public int adxCount;
        public int dspCount;
        public int dspClick;
        public int pendingClick;

        public TaskStatItem AddAllCount(int value)
        {
            Interlocked.Add(ref allCount, value);
            return this;
        }
        public TaskStatItem AddAdxCount(int value)
        {
            Interlocked.Add(ref adxCount, value);
            return this;
        }
        public TaskStatItem AddDspCount(int value)
        {
            Interlocked.Add(ref dspCount, value);
            return this;
        }
        public TaskStatItem AddDspClick(int value)
        {
            Interlocked.Add(ref dspClick, value);
            return this;
        }
        public TaskStatItem AddPendingClick(int value)
        {
            Interlocked.Add(ref pendingClick, value);
            return this;
        }
    }
}
