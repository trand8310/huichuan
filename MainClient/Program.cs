using MainClient.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

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

                })
                .UseSerilog((_, _, loggerConfiguration) => loggerConfiguration
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .WriteTo.File(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "main-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 20,
                        fileSizeLimitBytes: 200L * 1024 * 1024,
                        rollOnFileSizeLimit: true,
                        shared: true,
                        outputTemplate: "记录时间：{Timestamp:yyyy-MM-dd HH:mm:ss} 线程ID:[{ThreadId}] 等级：[{Level:u3}] 操作信息：{Message:lj}{NewLine}{Exception}"));
            using var host = builder.Build();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
                CreateProgramLogger(host).LogCritical(e.Exception, "未处理的 UI 线程异常");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                CreateProgramLogger(host).LogCritical(e.ExceptionObject as Exception, "未处理的应用程序异常");
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
                    services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Program")
                        .LogCritical(ex, "MainClient 异常退出");
                }
            }
        }

        private static Microsoft.Extensions.Logging.ILogger CreateProgramLogger(IHost host) =>
            host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    }
}
