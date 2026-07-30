using System.Net;
using System.Text;

namespace MainClient.Common
{
    public static class HttpClientUtil
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            // 无认证代理：
            // http://127.0.0.1:7890
            //
            // 带认证代理：
            // IP：127.0.0.1
            // 端口：7890
            // 用户名：proxyUser
            // 密码：proxyPassword

            var proxy = new WebProxy
            {
                Address = new Uri("http://127.0.0.1:7890"),

                // false：访问本机、局域网地址时也使用代理
                // true：本地地址不走代理
                BypassProxyOnLocal = false,

                // 不使用当前 Windows 用户凭据
                UseDefaultCredentials = false,

                // 代理不需要认证时删除这一行
                Credentials = new NetworkCredential(
                    "proxyUser",
                    "proxyPassword")
            };

            var handler = new HttpClientHandler
            {
                UseProxy = true,
                Proxy = proxy,

                MaxConnectionsPerServer = 50,

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate |
                    DecompressionMethods.Brotli
            };

            return new HttpClient(handler, disposeHandler: true)
            {
                // 每个请求使用 CancellationToken 单独控制超时
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public static async Task<byte[]?> SendPostAsync(
            string url,
            HttpContent requestContent,
            int timeoutMilliseconds,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            ArgumentNullException.ThrowIfNull(requestContent);

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCts.CancelAfter(timeoutMilliseconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Version = HttpVersion.Version10,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = requestContent
            };

            try
            {
                using HttpResponseMessage response =
                    await HttpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeoutCts.Token);

                // 保持与原 Java 代码一致：
                // 即使服务器返回 400、500，也读取响应内容
                return await response.Content.ReadAsByteArrayAsync(
                    timeoutCts.Token);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                Console.Error.WriteLine(
                    $"HTTP请求超时：{timeoutMilliseconds}ms");
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine(
                    $"HTTP请求失败：{ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine(
                        $"内部异常：{ex.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"发送HTTP请求异常：{ex}");
            }

            return null;
        }

        public static byte[] CreateProtocolRequest(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] requestBytes = new byte[jsonBytes.Length + 16];
            requestBytes[1] = 2;
            Buffer.BlockCopy(jsonBytes,0,requestBytes,16,jsonBytes.Length);
            return requestBytes;
        }

        public static string? ParseProtocolResponse(
            byte[]? responseBytes)
        {
            if (responseBytes == null ||
                responseBytes.Length <= 16)
            {
                return null;
            }

            return Encoding.UTF8.GetString(
                responseBytes,
                16,
                responseBytes.Length - 16);
        }
    }
}
