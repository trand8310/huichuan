using MainClient.Common;
using MainClient.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{
    public class CommonHelper
    {
        public static string HmacSha1Sign(byte[] input, byte[] key)
        {
            HMACSHA1 myhmacsha1 = new HMACSHA1(key);
            MemoryStream stream = new MemoryStream(input);
            return myhmacsha1.ComputeHash(stream).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
        }

        public static string ComputeSha1Hash(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static long UnixTimeNow()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
        public static long UnixTimeNowSecond()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }

        public static string CreateMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "").ToLower();
            }
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

        /// <summary>
        /// 比如，要获取-1000~+1000范围的随机数，总的数量为2001个，这样就可以通过代码
        /// Random(Guid.NewGuid().GetHashCode()).Next()%2001 使得到的结果限制在0-2000范围，再减去1000, 结果就是-1000~+1000之间了。
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int RandomRange(int min, int max)
        {
            int mod = max + Math.Abs(min) + 1;
            return new Random(Guid.NewGuid().GetHashCode()).Next() % mod - Math.Abs(min);
        }

        public static void ClearAllErrorMsgDialog()
        {

            string[] allTitles = [
                "CefClient.exe - 应用程序错误",
                "CefSharp.BrowserSubprocess.exe - 应用程序错误",
                "CefSharp.BrowserSubprocess.exe - 系统错误",
                "CefSharp.BrowserSubprocess.exe - 错误",
                "CefSharp.BrowserSubprocess.exe - 异常",
                "WerFault.exe - 应用程序错误",
            ];



            // 枚举所有窗口
            UnsafeNativeMethods.EnumWindows((hWnd, lParam) =>
            {
                // 检查窗口标题是否匹配
                string title = UnsafeNativeMethods.GetWindowTitle(hWnd);
                if (allTitles.Contains(title))
                {
                    UnsafeNativeMethods.SendMessage(hWnd, UnsafeNativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                return true; // 继续枚举下一个窗口
            }, IntPtr.Zero);

            //CommonHelper.ClearErrorMsgDialog("CefClient.exe - 应用程序错误");
            //CommonHelper.ClearErrorMsgDialog("CefSharp.BrowserSubprocess.exe - 应用程序错误");
            //CommonHelper.ClearErrorMsgDialog("CefSharp.BrowserSubprocess.exe - 系统错误");
            //CommonHelper.ClearErrorMsgDialog("CefSharp.BrowserSubprocess.exe - 错误");
            //CommonHelper.ClearErrorMsgDialog("CefSharp.BrowserSubprocess.exe - 异常");
            //CommonHelper.ClearErrorMsgDialog("WerFault.exe - 应用程序错误");
        }
        /// <summary>
        /// 系统重启
        /// </summary>
        public static void ProcessRestart()
        {
            Process.Start(Application.ExecutablePath, "restart");
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception)
            {
                CommonHelper.KillProcExec(Process.GetCurrentProcess().Id);
            }
        }




        public static Int16 Get16BitHash(string s)
        {
            return (Int16)(s.GetHashCode() & 0xFFFF);
        }

        public static string ComputeHash(string input)
        {
            byte[] bytes = Encoding.Default.GetBytes(input);
            var iSHA = SHA1.Create();
            bytes = iSHA.ComputeHash(bytes);
            StringBuilder buf = new StringBuilder();
            foreach (byte b in bytes)
            {
                buf.AppendFormat("{0:x2}", b);
            }
            return buf.ToString().ToUpper();
        }

        public static string GetHostName()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostName);
                var addressV = iPHostEntry.AddressList.FirstOrDefault(q => q.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);//ip4地址
                if (addressV != null)
                    return addressV.ToString();
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        public static long CreateIMEI(long imei)
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






        public static string CreateDeviceUUID()
        {
            Guid result = Guid.NewGuid();
            byte[] guidBytes = result.ToByteArray();
            for (int i = 0; i < 8; i++)
            {
                byte t = guidBytes[15 - i];
                guidBytes[15 - i] = guidBytes[i];
                guidBytes[i] = t;
            }

            return new Guid(guidBytes).ToString();
        }

        /// <summary>  
        /// 根据GUID获取16位的唯一字符串  
        /// </summary>  
        /// <param name=\"guid\"></param>  
        /// <returns></returns>  
        public static string GuidTo16String()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
                i *= ((int)b + 1);
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }
        /// <summary>  
        /// 根据GUID获取19位的唯一数字序列  
        /// </summary>  
        /// <returns></returns>  
        public static long GuidToLongID()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }
        public static string GetRandomWifiMacAddress()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            buffer[0] = 02;
            var result = string.Concat(buffer.Select(x => string.Format("{0}", x.ToString("X2"))).ToArray());
            return result.ToUpper().Insert(2, "-");
        }
        public static string GetRandomMacAddress()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            var result = String.Concat(buffer.Select(x => string.Format("{0}:", x.ToString("X2"))).ToArray());
            return result.TrimEnd(':');
        }


        public static int GetOS(string userAgent)
        {
            var tmp = userAgent.ToLower();
            if (tmp.Contains("android"))
                return 0;//Android
            else if (tmp.ToLower().Contains("windows phone"))
                return 2;//Windows Phone
            else if (tmp.Contains("iphone") || tmp.Contains("ipad"))
                return 1;//Iphone
            return 3;
        }

        public static void ClearProcesses(string[] processNames, string baseDir = null)
        {
            try
            {
                while (true)
                {
                    var windowPtr = UnsafeNativeMethods.FindWindowByCaption(IntPtr.Zero, "CefClient.exe - 应用程序错误");
                    if (windowPtr != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.SendMessage(windowPtr, UnsafeNativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    else
                    {
                        break;
                    }
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
            if (processNames.Count() > 0)
            {
                var Processes = Process.GetProcesses().Where(w => processNames.Contains(w.ProcessName));
                foreach (Process item in Processes)
                {
                    if (!item.HasExited)
                    {
                        try
                        {
                            item.Kill();
                        }
                        catch (Exception ex)
                        {
                            KillProcExec(item.Id);
                            Debug.WriteLine(ex.Message);
                        }

                    }
                }
            }


        }


        public static Process ExecCmd()
        {
            Process p = null;
            try
            {
                p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
            }
            catch (Exception)
            {
                throw;
            }
            return p;
        }
        public static bool KillProcExec(int procId)
        {
            string cmd = string.Format("taskkill /f /t /im {0}", procId); //强制结束指定进程
            Process ps = null;
            try
            {
                ps = ExecCmd();
                ps.Start();
                ps.StandardInput.WriteLine(cmd + "&exit");
                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                ps.Close();
            }
        }


        public static long IpToInt(string ip)
        {
            string[] items = ip.Split('.');
            return long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
        }
        public static void DeleteCookieFile(string dirRoot)
        {
            try
            {
                string[] rootDirs = Directory.GetDirectories(dirRoot);
                string[] rootFiles = Directory.GetFiles(dirRoot);
                foreach (string s2 in rootFiles)
                {
                    if (s2.Contains("Cookies"))
                    {
                        File.Delete(s2);
                    }
                }
                foreach (string s1 in rootDirs)
                {
                    DeleteCookieFile(s1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
        public static Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }


        //保存图片时设置质量
        public static void SaveImageWithQuality(Image bmp, long level)
        {
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp.Save(@"test.jpg", jgpEncoder, myEncoderParameters);
        }


        /// <summary>
        /// 图片尺寸压缩
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CompressImageWithSize(System.Drawing.Bitmap bitmap, int maxWidth = 1024, int maxHeight = 1024)
        {
            int actualWidth = bitmap.Width < maxWidth ? bitmap.Width : maxWidth;
            int actualHeight = int.Parse(Math.Round(bitmap.Height * (double)actualWidth / bitmap.Width).ToString());
            try
            {
                var actualBitmap = new System.Drawing.Bitmap(actualWidth, actualHeight);
                var g = System.Drawing.Graphics.FromImage(actualBitmap);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, actualWidth, actualHeight)
                    , new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height)
                    , System.Drawing.GraphicsUnit.Pixel);
                g.Dispose();
                return actualBitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }



        /// <summary>
        /// 图像质量压缩
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="encoding"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CompressImageWithQuality(System.Drawing.Bitmap bitmap, System.Drawing.Imaging.ImageCodecInfo encoding, int quality = 70)
        {
            var ps = new System.Drawing.Imaging.EncoderParameters(1);
            ps.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            var stream = new MemoryStream();
            bitmap.Save(stream, encoding, ps);
            var compressedBitmap = new System.Drawing.Bitmap(stream);
            return compressedBitmap;
        }

        public static Dictionary<string, System.Drawing.Imaging.ImageCodecInfo> GetImageEncoders()
        {
            var result = new Dictionary<string, System.Drawing.Imaging.ImageCodecInfo>();
            var encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().ToList();
            foreach (var encode in encoders)
                result.Add(encode.MimeType, encode);
            return result;
        }

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        public static void ClearErrorMsgDialog(string title)
        {
            try
            {
                var _wndRes = UnsafeNativeMethods.FindWindowByCaption(IntPtr.Zero, title);
                if (_wndRes != IntPtr.Zero)
                {
                    UnsafeNativeMethods.SendMessage(_wndRes, UnsafeNativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }



        public static void ClearCacheFile()
        {
            #region 删除物理文件
            ////for (int parallelIndex = 1; parallelIndex <= setting.MaximumParallel; parallelIndex++)
            ////{
            ////    try
            ////    {
            ////        Directory.Delete(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", parallelIndex.ToString()), recursive: true);
            ////    }
            ////    catch (Exception ex)
            ////    {
            ////        Console.WriteLine(ex.Message);
            ////    }
            ////    try
            ////    {
            ////        CommonHelper.DeleteCookieFile(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", parallelIndex.ToString()));
            ////    }
            ////    catch (Exception ex)
            ////    {
            ////        Console.WriteLine(ex.Message);
            ////    }
            ////}
            #endregion
        }



        public static void ClearCacheFile(int processIndex)
        {
            #region 删除物理文件

            try
            {
                string cachePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", processIndex.ToString());
                if (System.IO.Directory.Exists(cachePath))
                    Directory.Delete(cachePath, recursive: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion
        }




        public static HttpClient CreateHttpClient(string proxyAddress, bool isProxyMode = false)
        {
            return isProxyMode && string.IsNullOrWhiteSpace(proxyAddress) ? (proxyAddress.StartsWith("socks5") ? CreateSocks5HttpClient(proxyAddress) : CreateProxyHttpClient(proxyAddress)) : new HttpClient();
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

        public static void CreateShortcut(string shortcutName)
        {
            IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
            var shortcutPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{shortcutName}{string.Join("", AppConsts.AppVertion.Split('.').Skip(1).Take(2))}.lnk");
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutPath) as IWshRuntimeLibrary.IWshShortcut;
            shortcut.Arguments = "restart";
            shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
            shortcut.WindowStyle = 1;
            shortcut.Description = shortcutName;
            shortcut.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            shortcut.IconLocation = System.Windows.Forms.Application.ExecutablePath;
            shortcut.Save();
        }

    }
}
