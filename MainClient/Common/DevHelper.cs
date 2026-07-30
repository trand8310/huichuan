using MainClient.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MainClient.Common
{
    public class DevHelper
    {
        class client_using_entity
        {
            public client_using_entity(int _total)
            {
                total = _total;
                index = 0;
            }
            public int total;
            public int index;

            public client_using_entity Increment()
            {
                Interlocked.Increment(ref index);
                return this;
            }
        }

        static string[] android_screens = { "720*1280", "960*540", "960*640", "960*720", "1280*800", "1280*720", "1024*768", "1024*600", "1024*576", "1080*1920", "1080*1920", "2560*1440", "640*960", "640*1136", "750*1334", "1242*2208", "1125*2436" };
        static string[] iphone_screens = { "640*960", "640*1136", "750*1334", "1080*1920", "1125*2436", "1242*2688", "828*1792" };
        static string[] ipad_screens = { "768*1024", "1536*2048", "2048*2732", "1668*2224", "1668*2388", "1536*2160" };
        static string[] desktop_screens = { "1920*1080", "1366*768", "1024*768", "1024*600", "1280*1024", "1600*900", "1440*1050", "1600*1200", "1280*800", "1280*854", "1440*900", "1600*1024", "1680*1050", "1920*1200", "2048*1080", "2560*1980" };
        public static int[] android_ios = { 1, 4 };


        private readonly ILogger _logger;
        private readonly IWritableOptions<AppSettings> _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public static SemaphoreSlim _mutex = new SemaphoreSlim(1);

        public DevHelper(IWritableOptions<AppSettings> appSettings, IHttpClientFactory httpClientFactory, ILogger<DevHelper> logger)
        {
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private static ConcurrentDictionary<string, JObject[]> all_client_devs = new ConcurrentDictionary<string, JObject[]>();
        private static ConcurrentDictionary<string, client_using_entity> all_client_using_entitys = new ConcurrentDictionary<string, client_using_entity>();
        public async Task<JObject> GetDevByClient(string dev_client_id)
        {
            if (!all_client_devs.ContainsKey(dev_client_id))
            {
                try
                {
                    await _mutex.WaitAsync();
                    var fileName = $@"./Data/dev_{dev_client_id}.txt";
                    if (System.IO.File.Exists(fileName))
                    {
                        var data = (await System.IO.File.ReadAllLinesAsync(fileName)).Select(line => JsonConvert.DeserializeObject<JObject>(line)).ToArray();
                        all_client_devs.TryAdd(dev_client_id, data);
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    _mutex.Release();
                }
            }

            if (all_client_devs.TryGetValue(dev_client_id, out var list))
            {
                all_client_using_entitys.AddOrUpdate(dev_client_id, new client_using_entity(list.Length), (key, value) => value.Increment());
                var use_value = all_client_using_entitys[dev_client_id];
                return list[use_value.index % use_value.total];
            }
            return null;
        }




        #region 系统设备
        //ANDROID 设备参数
        private static ConcurrentQueue<JToken> android_devs = new ConcurrentQueue<JToken>();
        //IOS 设备参数
        private static ConcurrentQueue<JToken> ios_devs = new ConcurrentQueue<JToken>();
        //OTT
        private static ConcurrentQueue<JToken> ott_devs = new ConcurrentQueue<JToken>();
        private async Task<string> GetDevByOSInternal(OSType os, int count)
        {
            try
            {
                var devApiUrl = _appSettings.Value.DevApiUrl;
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                var url = $"{devApiUrl}?type={(os == OSType.IOS ? "ios" : (os == OSType.OTT) ? "ott" : "android")}&count={count}&t={System.DateTime.Now.Ticks}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }
        public async Task<JToken> GetDevByOS(OSType os, int count = 1)
        {
            if (os == OSType.IOS)
            {
                if (ios_devs.TryDequeue(out var result))
                {
                    return result;
                }
                var json = await GetDevByOSInternal(os, count);
                try
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(json);
                    if (jo != null)
                    {
                        foreach (var item in jo["data"])
                        {
                            ios_devs.Enqueue(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                if (ios_devs.TryDequeue(out result))
                {
                    return result;
                }
                return null;
            }
            else if (os == OSType.ANDROID)
            {
                if (android_devs.TryDequeue(out var result))
                {
                    return result;
                }
                var json = await GetDevByOSInternal(os, count);
                try
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(json);
                    if (jo != null)
                    {
                        foreach (var item in jo["data"])
                        {
                            android_devs.Enqueue(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (android_devs.TryDequeue(out result))
                {
                    return result;
                }
                return null;
            }

            else if (os == OSType.OTT)
            {
                if (ott_devs.TryDequeue(out var result))
                {
                    return result;
                }
                var json = await GetDevByOSInternal(os, count);
                try
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(json);
                    if (jo != null)
                    {
                        foreach (var item in jo["data"])
                        {
                            ott_devs.Enqueue(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (ott_devs.TryDequeue(out result))
                {
                    return result;
                }
                return null;
            }


            else
            {
                return null;
            }
        }
        #endregion

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="os"></param>
        /// <returns></returns>
        public JObject GetRandomDevByOS(OSType os)
        {
            if (os == OSType.IOS)
            {
                var jo = new JObject();
                jo.Add("idfa", DevMan.GetIdfa());
                jo.Add("imei", DevMan.GetImei());
                jo.Add("mac", CommonHelper.GetRandomMacAddress());
                return jo;
            }
            else if (os == OSType.ANDROID)
            {

                var jo = new JObject();
                jo.Add("android_id", DevMan.GetAndroidId());
                jo.Add("imei", DevMan.GetImei().ToLower());
                jo.Add("mac", CommonHelper.GetRandomMacAddress().ToUpper());
                return jo;
            }
            return null;
        }

        public string GetDevScreen(string ua)
        {
            var result = string.Empty;
            if (ua.Contains("iPad;"))
            {
                result = ipad_screens[new Random().Next(0, ipad_screens.Length - 1)];
            }
            else if (ua.Contains("iPhone;"))
            {
                if (ua.Contains("iPhone OS 7"))
                {
                    result = iphone_screens[new Random().Next(0, 1)];
                }
                else if (ua.Contains("iPhone OS 8"))
                {
                    result = iphone_screens[new Random().Next(1, 3)];
                }
                else if (ua.Contains("iPhone OS 9"))
                {
                    result = iphone_screens[new Random().Next(1, 3)];
                }
                else if (ua.Contains("iPhone OS 10"))
                {
                    result = iphone_screens[new Random().Next(3, 6)];
                }
                else if (ua.Contains("iPhone OS 11"))
                {
                    result = iphone_screens[new Random().Next(3, 6)];
                }
                else if (ua.Contains("iPhone OS 12"))
                {
                    result = iphone_screens[new Random().Next(3, 6)];
                }
                else
                    result = iphone_screens[new Random().Next(3, 6)];
            }
            else if (ua.Contains("Android;"))
            {
                result = android_screens[new Random().Next(0, android_screens.Length - 1)];
            }
            else
            {
                result = desktop_screens[new Random().Next(0, desktop_screens.Length - 1)];
            }
            return result;
        }
    }
}