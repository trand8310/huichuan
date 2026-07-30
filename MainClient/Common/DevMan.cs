using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{ 
    public class DevMan
    {
        static string[] android_screens = { "720*1280", "960*540", "960*640", "960*720", "1280*800", "1280*720", "1024*768", "1024*600", "1024*576", "1080*1920", "1080*1920", "2560*1440", "640*960", "640*1136", "750*1334", "1242*2208", "1125*2436" };
        static string[] iphone_screens = { "640*960", "640*1136", "750*1334", "1080*1920", "1125*2436", "1242*2688", "828*1792" };
        static string[] ipad_screens = { "768*1024", "1536*2048", "2048*2732", "1668*2224", "1668*2388", "1536*2160" };
        static string[] desktop_screens = { "1920*1080", "1366*768", "1024*768", "1024*600", "1280*1024", "1600*900", "1440*1050", "1600*1200", "1280*800", "1280*854", "1440*900", "1600*1024", "1680*1050", "1920*1200", "2048*1080", "2560*1980" };


        public static JObject GetDevWithUa(string userAgent, OSType os)
        {
            string screen = string.Empty;
            var result = new JObject();
            result.Add(new JProperty("ua", userAgent));
            result.Add(new JProperty("os", (int)os));
            if (userAgent.Contains("iPad;"))
            {
                screen = ipad_screens[new Random().Next(0, ipad_screens.Length - 1)];
            }
            else if (userAgent.Contains("iPhone;"))
            {
                if (userAgent.Contains("iPhone OS 7"))
                {
                    screen = iphone_screens[new Random().Next(0, 1)];
                }
                else if (userAgent.Contains("iPhone OS 8"))
                {
                    screen = iphone_screens[new Random().Next(1, 3)];
                }
                else if (userAgent.Contains("iPhone OS 9"))
                {
                    screen = iphone_screens[new Random().Next(1, 3)];
                }
                else if (userAgent.Contains("iPhone OS 10"))
                {
                    screen = iphone_screens[new Random().Next(3, 6)];
                }
                else if (userAgent.Contains("iPhone OS 11"))
                {
                    screen = iphone_screens[new Random().Next(3, 6)];
                }
                else if (userAgent.Contains("iPhone OS 12"))
                {
                    screen = iphone_screens[new Random().Next(3, 6)];
                }
                else
                    screen = iphone_screens[new Random().Next(3, 6)];
            }
            else if (userAgent.Contains("Android;"))
            {
                screen = android_screens[new Random().Next(0, android_screens.Length - 1)];

            }
            else
            {
                screen = desktop_screens[new Random().Next(0, desktop_screens.Length - 1)];
            }
            var values = screen.Split('*');
            result.Add(new JProperty("sw", values[0]));
            result.Add(new JProperty("sh", values[1]));
            return result;
        }
        public static JObject GetDevScreenWithUa(string userAgent, OSType os)
        {
            var result = new JObject();
            result.Add(new JProperty("user-agent", userAgent));
            result.Add(new JProperty("os", (int)os));
            if (userAgent.Contains("iPad;"))
            {
                result.Add(new JProperty("screen", ipad_screens[new Random().Next(0, ipad_screens.Length - 1)]));
            }
            else if (userAgent.Contains("iPhone;"))
            {
                if (userAgent.Contains("iPhone OS 7"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(0, 1)]));
                }
                else if (userAgent.Contains("iPhone OS 8"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(1, 3)]));
                }
                else if (userAgent.Contains("iPhone OS 9"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(1, 3)]));
                }
                else if (userAgent.Contains("iPhone OS 10"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(3, 6)]));
                }
                else if (userAgent.Contains("iPhone OS 11"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(3, 6)]));
                }
                else if (userAgent.Contains("iPhone OS 12"))
                {
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(3, 6)]));
                }
                else
                    result.Add(new JProperty("screen", iphone_screens[new Random().Next(3, 6)]));
            }
            else if (userAgent.Contains("Android;"))
            {
                result.Add(new JProperty("screen", android_screens[new Random().Next(0, android_screens.Length - 1)]));
            }
            else
            {
                result.Add(new JProperty("screen", desktop_screens[new Random().Next(0, desktop_screens.Length - 1)]));
            }
            return result;
        }
        //
        //__OS__//1位数字,取0~3。0表示Android，1表示iOS，2表示Windows Phone，3表示其他
        public static OSType GetOS(string userAgent)
        {
            var tmp = userAgent.ToLower();
            if (tmp.Contains("android"))
                return OSType.ANDROID;
            else if (tmp.Contains("iphone") || tmp.Contains("ipad"))
                return OSType.IOS;
            else if (tmp.ToLower().Contains("windows phone"))
                return OSType.WINDOWS_PHONE;
            return OSType.OTHER;
        }

        /// <summary>
        /// 获取IDFA
        /// </summary>
        /// <returns></returns>
        public static string GetIdfa()
        {
            string uuid = Guid.NewGuid().ToString("D");
            if (uuid[14] != '4')
            {
                char[] ch = uuid.ToCharArray();
                ch[14] = '4';
                uuid = new string(ch);
            }
            return uuid;
        }

        public static string GetAndroidId()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
                i *= ((int)b + 1);
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }
        public static string GetMacAddress()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            var result = String.Concat(buffer.Select(x => string.Format("{0}:", x.ToString("X2"))).ToArray());
            return result.TrimEnd(':');
        }
        /// <summary>
        /// 不带分隔符的Mac地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressv2()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            var result = String.Concat(buffer.Select(x => string.Format("{0}", x.ToString("X2"))).ToArray());
            return result.TrimEnd();
        }
        public static string GetImsi()
        {
            int r1 = 10000 + new Random().Next(0, 90000);
            int r2 = 10000 + new Random().Next(0, 90000);
            return $"46004{r1}{r2}";
        }
        public static string GetImei()
        {
            var imei = CreateIMEI(System.DateTime.Now.Ticks % 100000000000000).ToString();
            if (imei.Length > 15)
            {
                imei = imei.Substring(0, 15);
            }
            return imei;
        }
        private static long CreateIMEI(long imei)
        {
            var current = imei;
            var checksum = 0;
            for (int i = 0; i < 7; i++)
            {
                var d1 = (int)(current % 10) * 2;
                current = current / 10;
                var d0 = (int)(current % 10);
                current = current / 10;
                checksum += +d0 + d1 / 10 + d1 % 10;
            }
            checksum = 10 - (checksum % 10);
            if (checksum == 10)
                checksum = 0;
            return imei * 10 + checksum;
        }
    }
}
