using CefClient.Common;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class Cfx2ResourceHandler : ResourceHandler
    {

        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random(Guid.NewGuid().GetHashCode());
            char[] stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }


        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {

            if (request.Url.StartsWith("http://192.168."))
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        var content = "{\"error_code\": " + new string[] { "0", "-68990" }[new Random(Guid.NewGuid().GetHashCode()).Next(0, 2)] + ",\"stok\":\"%28%2Cn%241%24u%7" + GenerateRandomString(5) + "%2C%24B%2" + GenerateRandomString(7) + "%2" + GenerateRandomString(4) + "%2EeT%2C%2B%2B\"}";
                        var stream = new MemoryStream();
                        stream.Write(Encoding.UTF8.GetBytes(content));
                        stream.Position = 0;
                        ResponseLength = stream.Length;
                        MimeType = Cef.GetMimeType("json");
                        StatusCode = (int)HttpStatusCode.OK;
                        Stream = stream;
                        callback.Continue();
                    }
                });

            }
            else
            {
                Task.Run(() =>
                {
                    using (callback)
                    {
                        var content = "{\"error_code\": -83600}";
                        var stream = new MemoryStream();
                        stream.Write(Encoding.UTF8.GetBytes(content));
                        stream.Position = 0;
                        ResponseLength = stream.Length;
                        MimeType = Cef.GetMimeType("json");
                        StatusCode = (int)HttpStatusCode.OK;
                        Stream = stream;
                        callback.Continue();
                    }
                });
            }
            return CefReturnValue.ContinueAsync;
        }
    }
}
