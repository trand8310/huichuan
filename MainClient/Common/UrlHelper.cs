using MainClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Windows.Forms;

namespace MainClient.Common
{

    public class UrlHelper
    {
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger("URLLogging");

        /// <summary>
        /// 秒针URL处理
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        public static string FormatUrlText(string url, string ip, JToken param, OSType os, JToken dev)
        {
            if (url.Contains("miaozhen.com"))
            {
                url = miaozhen(url, ip, param, os, dev);
            }
            else if (url.Contains(".aty.sohu.com"))
            {
                url = souhu(url, ip, param, os, dev);
            }
            else if (url.Contains("gridsumdissector.com"))
            {
                url = gridsumdissector(url, ip, param, os, dev);
            }
            else if (url.Contains("ipinyou.com"))
            {
                url = ipinyou(url, ip, param, os, dev);
            }
            else if (url.Contains("stats.dmp.ghac.cn"))
            {
                url = dmpghac(url, ip, param, os, dev);
            }
            else
            {
                url = miaozhen(url, ip, param, os, dev);
            }
            return url;
        }





        /// <summary>
        /// 秒针URL处理
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private static string miaozhen(string url, string ip, JToken param, OSType os, JToken dev)
        {
            //__OS__//1位数字,取0~3。0表示Android，1表示iOS，2表示Windows Phone，3表示其他
            if (os == OSType.ANDROID)
            {
                url = url.Replace("__OS__", "0");
            }
            else if (os == OSType.IOS)
            {
                url = url.Replace("__OS__", "1");
            }

            else if (os == OSType.WINDOWS_PHONE)
            {
                url = url.Replace("__OS__", "2");
            }
            else
            {
                url = url.Replace("__OS__", "3");
            }

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {

                string mac = string.Empty;
                if (dev != null && dev["mac"] != null)
                {
                    mac = dev["mac"].ToString().ToUpper();
                }
                if (string.IsNullOrWhiteSpace(mac))
                {
                    mac = CommonHelper.GetRandomMacAddress();
                }
                var macmd51 = CommonHelper.MD5Hash(mac);
                var macmd52 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                url = url.Replace("__MAC1__", macmd51);
                url = url.Replace("__MAC__", macmd52);

                if (os == OSType.IOS)
                {
                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);

                }

                else if (os == OSType.ANDROID)
                {
                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);


                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToUpper();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);

                    url = url.Replace("__ANDROIDID__", androidId_md5);
                    url = url.Replace("__ANDROIDID1__", androidId);
                }
            }
            return url;
        }



        /// <summary>
        /// 国双URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private static string gridsumdissector(string url, string ip, JToken param, OSType os, JToken dev)
        {
            //https://i.gridsumdissector.com/v/?gscmd=impress&gid=gad_155_Y9RU7SQW&os=__OS__&if=__IDFA__&oid=__OPENUDID__&aid=__ANDROIDID__&im=__IMEI__&oa=__OAID__&m=__MAC__&ip=__IP__&ts=__TS__&did=__DUID__&aaid=__AAID__&uid=__UDID__&odin=__ODIN__&ua=__UA__&lbs=__LBS__

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }
            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                if (os == OSType.ANDROID)
                    url = url.Replace("__OS__", "0");
                else if (os == OSType.IOS)
                    url = url.Replace("__OS__", "1");
                else if (os == OSType.WINDOWS_PHONE)
                    url = url.Replace("__OS__", "2");
                else
                    url = url.Replace("__OS__", "3");



                string mac = string.Empty;
                if (dev != null && dev["mac"] != null)
                {
                    mac = dev["mac"].ToString().ToUpper();
                }
                if (string.IsNullOrWhiteSpace(mac))
                {
                    mac = CommonHelper.GetRandomMacAddress().ToUpper();
                }
                var macmd5 = CommonHelper.MD5Hash(mac);
                url = url.Replace("__MAC__", CommonHelper.MD5Hash(macmd5));


                if (os == OSType.IOS)
                {

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);

                }
                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei().ToLower();
                    }

                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);



                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToUpper();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
                url = url.Replace("__TS__", CommonHelper.UnixTimeNow().ToString());
            }
            return url;
        }

        /// <summary>
        /// 深演广告
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private static string ipinyou(string url, string ip, JToken param, OSType os, JToken dev)
        {
            //http://vt.ipinyou.com/IinK3066gI5vwOkVZ-.IcX5R_.sWLZhPIi7pbkvccpO3kUXEe5DrZWFlJbrDuAyySZ_T8kzY9epmcXfrEv_RzyW4f.txHx607mbPPtH8cJVys8k_?tmp=[timestamp]&mob_idfa=[idfa]&mob_imei=[imei]&mob_android=[androidid]&mob_os=[os]&mob_oaid=[oaid]&mob_mac=[mac]

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {

                url = url.Replace("[timestamp]", CommonHelper.UnixTimeNow().ToString());
                if (os == OSType.ANDROID)
                {
                    url = url.Replace("[os]", "0");
                }
                else if (os == OSType.IOS)
                {
                    url = url.Replace("[os]", "1");
                }
                else if (os == OSType.WINDOWS_PHONE)
                {
                    url = url.Replace("[os]", "2");
                }
                else
                {
                    url = url.Replace("[os]", "3");
                }

                if (os == OSType.IOS)
                {

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("[idfa]", idfa);

                }
                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei().ToLower();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("[imei]", imei_md5);

                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                    url = url.Replace("[mac]", macmd5);

                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToLower();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("[androidid]", androidId_md5);
                }
            }
            return url;
        }

        /// <summary>
        /// 广本DMP
        ///  https://stats.dmp.ghac.cn/imp/QGe3gXV_J7tzpt.MeAYoP?u=__URL__&os=__OS__&imei=__IMEI__&mac=__MAC__&mac1=__MAC1__&idfa=__IDFA__&oaid=__OAID__&aaid=__AAID__&openudid=__OPENUDID__&androidid=__ANDROIDID__&duid=__DUID__&ip=__IP__&ua=__UA__&ts=__TS__
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private static string dmpghac(string url, string ip, JToken param, OSType os, JToken dev)
        {
            //__OS__//1位数字,取0~3。0表示Android，1表示iOS，2表示Windows Phone，3表示其他
            if (os == OSType.ANDROID)
            {
                url = url.Replace("__OS__", "0");
            }
            else if (os == OSType.IOS)
            {
                url = url.Replace("__OS__", "1");
            }

            else if (os == OSType.WINDOWS_PHONE)
            {
                url = url.Replace("__OS__", "2");
            }
            else
            {
                url = url.Replace("__OS__", "3");
            }

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                url = url.Replace("__UA__", System.Web.HttpUtility.UrlEncode(dev["ua"].ToString()));
                url = url.Replace("__TS__", CommonHelper.UnixTimeNow().ToString());

                if (os == OSType.IOS)
                {
                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);
                }
                else if (os == OSType.ANDROID)
                {
                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);

                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd51 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                    var macmd52 = CommonHelper.MD5Hash(mac);
                    url = url.Replace("__MAC__", macmd51);
                    url = url.Replace("__MAC1__", macmd52);

                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
            }
            return url;
        }

        //wifi、4G、3G、2G、unkown
        static string[] wt_values = { "wifi", "4G", "unkown", "wifi", "4G", "wifi", "4G", "wifi", "4G", "wifi" };


        /// <summary>
        /// 秒针URL处理
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private static string souhu(string url, string ip, JToken param, OSType os, JToken dev)
        {
            //http://agn.aty.sohu.com/m?channeled=1000180001&gid=x010740202ff0f8fa9e783458000306d2273e31f8829&pt=oad&bssid=02:00:00:00:00:00&frontAdsTime=126&source=1000180001&imsi=460092690102601&playstyle=1&AndroidID=abd016d9eab78861&exten=1,2,3,4,5,6,7,8&ssid=%3Cunknown+ssid%3E&mac=DC:90:88:ED:51:67&manufacturer=HUAWEI&vid=3557693&localAareaCode=4125&du=2420.014&offline=0&prot=vast&tuv=0165450ed6cd899d66c1a61fbbc2ca32&isBgPlay=0&plat=6&UUID=dfe527a2-2feb-44fa-9d9f-84fe0be898061556174168188&wt=wifi&ext=2097151&c=tv&density=3.0&displayMetrics=1080*2310&sver=7.2.1&islocaltv=0&sysver=28&al=9142496&vc=101100;101104;101106&screenstate=1&poid=1&audited_level=-1&protv=3.0&site=1&partner=6581&adoriginal=sohu&build=7002001&appid=tv&guid=89a9afdcd0af8c07dfaa56d1097749fe&imei=NULL_IMEI&sdkVersion=tv7.4.1&pn=PCT-AL10&vu=0&ua=Mozilla%2F5.0%20(iPhone%3B%20CPU%20iPhone%20OS%2011_0_1%20like%20Mac%20OS%20X)%20AppleWebKit%2F602.2.14(KHTML%2C%20like%20Gecko)%20Version%2F8.0%20MQQBrowser%2F8.6.2%20Mobile%2F15E5189f

            var jsText = string.Empty;
            if (!string.IsNullOrWhiteSpace(param["jstext"].ToString()))
            {
                jsText = param["jstext"].ToString();
            }
            else
            {
                jsText = param["referer"].ToString();
            }

            var adParam = (JObject)JsonConvert.DeserializeObject(System.Web.HttpUtility.UrlDecode(jsText));
            var urlParams = new Dictionary<string, string>();
            var pt = adParam["pt"]?.Value<string>() ?? "oad";
            if (pt == "open")
            {
                #region open
                if (adParam.ContainsKey("gid") && !string.IsNullOrWhiteSpace(adParam["gid"].Value<string>()))
                    urlParams.Add("gid", adParam["gid"].Value<string>());
                else
                    urlParams.Add("gid", "x010740202ff124e9bae9c00f000eb1b547c74832253");

                if (adParam.ContainsKey("identifyid") && !string.IsNullOrWhiteSpace(adParam["identifyid"].Value<string>()))
                    urlParams.Add("identifyid", adParam["identifyid"].Value<string>());
                else
                    urlParams.Add("identifyid", "14");

                urlParams.Add("pt", "open");
                urlParams.Add("bssid", "02:00:00:00:00:00");
                urlParams.Add("forbid", "0");
                urlParams.Add("exten", "1,2,3,4,5,6,7,8");
                urlParams.Add("ssid", "%3Cunknown+ssid%3E");//%3Cunknown+ssid%3E
                if (dev["make"] != null && !string.IsNullOrWhiteSpace(dev["make"].ToString()))
                {
                    urlParams.Add("manufacturer", dev["make"].ToString());
                    urlParams.Add("brand", dev["make"].ToString());
                }
                urlParams.Add("offline", "0");
                urlParams.Add("expired", "0");
                urlParams.Add("prot", "vast");
                urlParams.Add("UUID", Guid.NewGuid().ToString("D"));
                urlParams.Add("wt", new string[] { "wifi", "wifi", "4G", "wifi", "wifi", "4G", "wifi", "wifi", "4G", "unkown", "unkown" }[new Random().Next(0, 9)]);//网络状况，例：wifi、4G、3G、2G、unkown 不允许为空
                if (dev["model"] != null && !string.IsNullOrWhiteSpace(dev["model"].ToString()))
                {
                    urlParams.Add("pn", dev["model"].ToString());
                }
                urlParams.Add("ext", "2097151");
                urlParams.Add("c", "tv");
                urlParams.Add("density", "3.0");
                urlParams.Add("displayMetrics", $"{dev["sw"]}*{dev["sh"]}");
                urlParams.Add("sver", "7.5.0");
                urlParams.Add("poid", "1");
                urlParams.Add("protv", "3.0");
                urlParams.Add("partner", "93");
                urlParams.Add("adoriginal", "sohu");
                urlParams.Add("build", "7005000");
                urlParams.Add("vidtab", "0");
                urlParams.Add("appid", "tv");
                urlParams.Add("visitor", "0");
                urlParams.Add("catecode", "ad7");
                urlParams.Add("warmup", "0");

                if (os == OSType.ANDROID)
                {
                    if (dev["androidid"] != null && !string.IsNullOrWhiteSpace(dev["androidid"].ToString()))
                        urlParams.Add("AndroidID", dev["androidid"].ToString());
                    else if (dev["android_id"] != null && !string.IsNullOrWhiteSpace(dev["android_id"].ToString()))
                        urlParams.Add("AndroidID", dev["android_id"].ToString());

                    if (dev["imei"] != null && !string.IsNullOrWhiteSpace(dev["imei"].ToString()))
                    {
                        urlParams.Add("imei", dev["imei"].ToString());
                        urlParams.Add("tuv", dev["imei"].ToString());
                    }
                    else
                        urlParams.Add("imei", "NULL_IMEI");

                    urlParams.Add("plat", "6");

                    if (dev["oaid"] != null && !string.IsNullOrWhiteSpace(dev["oaid"].ToString()))
                        urlParams.Add("oaid", dev["oaid"].ToString());

                    urlParams.Add("poscode", "op_aphone_1");
                    urlParams.Add("sysver", new string[] { "23", "24", "25", "26", "27", "28", "29", "30" }[new Random().Next(0, 7)]);
                    //if (dev["imsi"] != null && !string.IsNullOrWhiteSpace(dev["imsi"].ToString()))
                    //    urlParams.Add("imsi", dev["imsi"].ToString());
                    //else
                    //    urlParams.Add("imsi", "");

                    urlParams.Add("imsi", "");
                    //if (dev["mac"] != null && !string.IsNullOrWhiteSpace(dev["mac"].ToString()))
                    //    urlParams.Add("mac", dev["mac"].ToString().ToUpper());
                    //else
                    //    urlParams.Add("mac", "");
                    urlParams.Add("mac", "02:00:00:00:00:00");

                    urlParams.Add("sdkVersion", "tv7.5.15");
                }
                else if (os == OSType.IOS)
                {
                    if (dev["idfv"] != null && !string.IsNullOrWhiteSpace(dev["idfv"].ToString()))
                    {
                        urlParams.Add("idfv", dev["idfv"].ToString());
                        urlParams.Add("tuv", dev["idfv"].ToString());
                    }
                    else
                        urlParams.Add("idfv", "");
                    if (dev["idfa"] != null && !string.IsNullOrWhiteSpace(dev["idfa"].ToString()))
                        urlParams.Add("idfa", dev["idfa"].ToString());
                    else
                        urlParams.Add("idfa", "");
                    urlParams.Add("plat", "3");
                    urlParams.Add("poscode", "op_iphone_1");
                    urlParams.Add("imei", "NULL_IMEI");
                    urlParams.Add("sdkVersion", "12.6.1");
                }
                #endregion
            }
            else if (pt == "oad")
            {
                #region oad
                if (adParam.ContainsKey("channeled") && !string.IsNullOrWhiteSpace(adParam["channeled"].Value<string>()))
                    urlParams.Add("channeled", adParam["channeled"].Value<string>());
                else
                    urlParams.Add("channeled", "1000180001");

                if (adParam.ContainsKey("gid") && !string.IsNullOrWhiteSpace(adParam["gid"].Value<string>()))
                    urlParams.Add("gid", adParam["gid"].Value<string>());
                else
                    urlParams.Add("gid", "x010740202ff0f8fa9e783458000306d2273e31f8829");

                urlParams.Add("plat", "6");
                urlParams.Add("pt", "oad");

                if (adParam.ContainsKey("vid") && !string.IsNullOrWhiteSpace(adParam["vid"].Value<string>()))
                    urlParams.Add("vid", adParam["vid"].Value<string>());
                else
                    urlParams.Add("vid", "8281929");

                urlParams.Add("site", "1");
                urlParams.Add("wt", wt_values[new Random().Next(0, 10)]);
                urlParams.Add("offline", "0");
                urlParams.Add("prot", "vast");
                urlParams.Add("displayMetrics", $"{dev["sw"]}*{dev["sh"]}");
                urlParams.Add("manufacturer", dev["make"]?.ToString());
                urlParams.Add("pn", dev["model"]?.ToString());

                if (os == OSType.ANDROID)
                {
                    urlParams["plat"] = "6";
                    if (dev["androidid"] != null && !string.IsNullOrWhiteSpace(dev["androidid"].ToString()))
                        urlParams.Add("AndroidID", dev["androidid"].ToString());
                    else if (dev["android_id"] != null && !string.IsNullOrWhiteSpace(dev["android_id"].ToString()))
                        urlParams.Add("AndroidID", dev["android_id"].ToString());

                    if (dev["imei"] != null && !string.IsNullOrWhiteSpace(dev["imei"].ToString()))
                    {
                        urlParams.Add("imei", dev["imei"].ToString());
                        urlParams.Add("tuv", dev["imei"].ToString());
                    }
                    else
                    {
                        urlParams.Add("imei", "NULL_IMEI");
                        urlParams.Add("tuv", "");
                    }

                    //if (dev["mac"] != null && !string.IsNullOrWhiteSpace(dev["mac"].ToString()))
                    //    urlParams.Add("mac", dev["mac"].ToString().ToUpper());
                    //else
                    //    urlParams.Add("mac", "02:00:00:00:00:00");

                    urlParams.Add("mac", "02:00:00:00:00:00");
                    if (dev["imsi"] != null && !string.IsNullOrWhiteSpace(dev["imsi"].ToString()))
                        urlParams.Add("imsi", dev["imsi"].ToString());
                    else
                        urlParams.Add("imsi", "");

                    if (dev["oaid"] != null && !string.IsNullOrWhiteSpace(dev["oaid"].ToString()))
                        urlParams.Add("oaid", dev["oaid"].ToString());

                    urlParams.Add("sver", "8.2.0");
                    urlParams.Add("build", "8002000");
                    urlParams.Add("sysver", new string[] { "29", "30", "31", "32", "33" }[new Random().Next(0, 5)]);
                }
                else if (os == OSType.IOS)
                {
                    urlParams["plat"] = "3";
                    //urlParams.Add("mac", "02:00:00:00:00:00");
                    if (dev["idfv"] != null && !string.IsNullOrWhiteSpace(dev["idfv"].ToString()))
                    {
                        urlParams.Add("idfv", dev["idfv"].ToString());
                        urlParams.Add("tuv", dev["idfv"].ToString());
                    }
                    else
                    {
                        urlParams.Add("idfv", "");
                        urlParams.Add("tuv", "");
                    }
                    if (dev["idfa"] != null && !string.IsNullOrWhiteSpace(dev["idfa"].ToString()))
                        urlParams.Add("idfa", dev["idfa"].ToString());
                    else
                        urlParams.Add("idfa", "");
                    urlParams.Add("sver", "8.2.0");
                    urlParams.Add("build", "8002000");

                }
                else
                {
                    urlParams["plat"] = "pc";
                }
                urlParams.Add("guid", Guid.NewGuid().ToString("n"));
                urlParams.Add("density", "3.0");
                urlParams.Add("sdkVersion", "tv7.4.1");
                urlParams.Add("appid", "tv");
                urlParams.Add("UUID", Guid.NewGuid().ToString("D"));
                //urlParams.Add("ssid", "%3Cunknown+ssid%3E");
                //urlParams.Add("bssid", "02:00:00:00:00:00");
                urlParams.Add("ssid", "");
                urlParams.Add("bssid", "");
                urlParams.Add("adoriginal", "sohu");
                urlParams.Add("c", "tv");
                urlParams.Add("ext", "2097151");
                urlParams.Add("exten", "1,2,3,4,5,6,7,8");

                if (adParam.ContainsKey("identifyid") && !string.IsNullOrWhiteSpace(adParam["identifyid"].Value<string>()))
                    urlParams.Add("identifyid", adParam["identifyid"].Value<string>());
                else
                    urlParams.Add("identifyid", "14");

                urlParams.Add("rip", ip);
                urlParams.Add("ua", System.Web.HttpUtility.UrlEncode(dev.SelectToken("ua").Value<string>()));
                #endregion
            }
            else if (pt == "oral")
            {
                #region oral

                urlParams.Add("gid", adParam["gid"]?.Value<string>() ?? "x010740202ff124e9bae9c00f000eb1b547c74832253");
                urlParams.Add("identifyid", adParam["identifyid"]?.Value<string>() ?? "14");
                urlParams.Add("pt", "oral");
                urlParams.Add("bssid", "");
                urlParams.Add("forbid", "0");
                urlParams.Add("battery", "100");
                urlParams.Add("exten", "1,2,3,4,5,6,7,8");
                urlParams.Add("ssid", "");
                if (!string.IsNullOrWhiteSpace(dev["make"]?.Value<string>()))
                {
                    urlParams.Add("manufacturer", dev["make"].ToString());
                    urlParams.Add("brand", dev["make"].ToString());
                }
                urlParams.Add("pn", dev["model"]?.Value<string>() ?? "");
                urlParams.Add("offline", "0");
                urlParams.Add("expired", "0");
                urlParams.Add("prot", "json");
                urlParams.Add("UUID", Guid.NewGuid().ToString("D"));
                urlParams.Add("wt", new string[] { "wifi", "wifi", "4G", "wifi", "wifi", "4G", "wifi", "wifi", "4G", "unkown" }[new Random().Next(0, 10)]);//网络状况，例：wifi、4G、3G、2G、unkown 不允许为空
                urlParams.Add("ext", "2097151");
                urlParams.Add("c", "tv");
                urlParams.Add("density", "3.0");
                urlParams.Add("displayMetrics", $"{dev["sw"]}*{dev["sh"]}");
                urlParams.Add("sver", "7.5.0");
                urlParams.Add("sysver", "Build.VERSION.SDK");
                urlParams.Add("poid", "1");
                urlParams.Add("protv", "3.0");
                urlParams.Add("partner", "93");
                urlParams.Add("adoriginal", "sohu");
                urlParams.Add("build", "7005000");
                urlParams.Add("vidtab", "0");
                urlParams.Add("appid", "tv");
                urlParams.Add("visitor", "0");
                urlParams.Add("catecode", "ad7");
                urlParams.Add("warmup", "0");
                urlParams.Add("isroot", "false");
                if (os == OSType.ANDROID)
                {
                    urlParams["plat"] = "6";
                    if (dev["androidid"] != null && !string.IsNullOrWhiteSpace(dev["androidid"].ToString()))
                        urlParams.Add("AndroidID", dev["androidid"].ToString());
                    else if (dev["android_id"] != null && !string.IsNullOrWhiteSpace(dev["android_id"].ToString()))
                        urlParams.Add("AndroidID", dev["android_id"].ToString());
                    if (!string.IsNullOrWhiteSpace(dev["osv"]?.Value<string>()))
                    {
                        var osv_values = dev["osv"].Value<string>().Split('.');
                        if (osv_values.Length > 0 && Int32.TryParse(osv_values[0], out var osv) && osv < 10)
                        {
                            urlParams.Add("imei", dev["imei"]?.Value<string>() ?? "NULL_IMEI");
                            urlParams.Add("tuv", dev["imei"]?.Value<string>() ?? "");

                        }
                    }
                    else
                    {
                        urlParams.Add("imei", "NULL_IMEI");
                        urlParams.Add("tuv", "");
                    }
                    urlParams.Add("imsi", "");
                    urlParams.Add("mac", "02:00:00:00:00:00");
                    urlParams.Add("oaid", dev["oaid"]?.Value<string>() ?? "");
                    urlParams.Add("sdkVersion", "tv7.5.15");
                }
                else if (os == OSType.IOS)
                {
                    urlParams["plat"] = "3";
                    urlParams.Add("idfa", dev["idfa"]?.Value<string>() ?? "");
                    urlParams.Add("idfv", dev["idfv"]?.Value<string>() ?? "");
                    urlParams.Add("tuv", dev["idfv"]?.Value<string>() ?? "");
                    urlParams.Add("sdkVersion", "12.6.1");
                    urlParams.Add("imei", "NULL_IMEI");
                    urlParams.Add("imsi", "");
                }
                #endregion
            }


            var urlParamValue = string.Join("&", urlParams.Keys.Select(s => { return $"{s}={urlParams[s]}"; }).ToList());
            return $"{url}?{urlParamValue}";
        }
    }
}
