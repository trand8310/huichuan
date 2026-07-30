using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public class CommonHelper
    {
        public static long UnixTimeNow()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
        public static long UnixTimeNowSecond()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }
        public static long UnixTimeNow(DateTime dt)
        {
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }
        public static long UnixTimeNowSecond(DateTime dt)
        {
            return new DateTimeOffset(dt).ToUnixTimeSeconds();
        }


        public static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "");
            }
        }

        public static HttpClient CreateSocks5HttpClient(string proxyAddress)
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = new WebProxy($"{proxyAddress}"),
                UseProxy = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            };
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public static HttpClient CreateProxyHttpClient(string proxyAddress)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                Proxy = new WebProxy(proxyAddress),
                UseProxy = true,
            };
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

    }
}
