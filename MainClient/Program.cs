using MainClient.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;

namespace MainClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var builder = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 读取配置文件
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .Build();
                    services.ConfigureWritable<AppSettings>(configuration.GetSection("App"));
                    services.AddHttpClient();
                    services.AddSingleton(configuration);
                    services.AddSingleton<ProxyTester>();
                    services.AddSingleton<AdxHelper>();
                    services.AddSingleton<DevHelper>();
                    services.AddSingleton<UrlHelper>();
                    services.AddSingleton<IpHelper>();
                    services.AddTransient<MainForm>();

                }).ConfigureLogging(logBuilder =>
                {
                    logBuilder.SetMinimumLevel(LogLevel.Trace);
                    logBuilder.AddLog4Net("log4net.config");
                });
            var host = builder.Build();
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                try
                {
                    ApplicationConfiguration.Initialize();
                    Application.Run(services.GetRequiredService<MainForm>());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
    }
}
