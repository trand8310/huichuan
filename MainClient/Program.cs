using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Logging;
using MainClient.Scheduler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Diagnostics;



namespace MainClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var launchOptions = new AppLaunchOptions
            {
                AutoStartTasks = args.Any(argument =>
                    string.Equals(argument, AppLaunchOptions.AutoStartArgument, StringComparison.OrdinalIgnoreCase))
            };
            var appSettings = new AppSettings();
            UserConfigService.Init(appSettings);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
                .Build();
            configuration.GetSection("AppSettings").Bind(appSettings);

            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Sink<UiLogSink>()
            .CreateLogger();



            var builder = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton(appSettings);
                    services.AddSingleton(launchOptions);
                    services.AddSingleton<TaskMetricsService>();
                    services.AddSingleton<ProxyTester>();
                    services.AddSingleton<AdxHelper>();
                    services.AddSingleton<DevHelper>();
                    services.AddSingleton<IpHelper>();
                    services.AddTransient<MainForm>();

                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .UseSerilog();

            using var host = builder.Build();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => CreateProgramLogger(host).LogCritical(e.Exception, "未处理的 UI 线程异常");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>   CreateProgramLogger(host).LogCritical(e.ExceptionObject as Exception, "未处理的应用程序异常");

            CleanupStaleCefProcesses(CreateProgramLogger(host));

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
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

        private static void CleanupStaleCefProcesses(Microsoft.Extensions.Logging.ILogger logger)
        {
            // Kill CefClient first with its process tree, then remove any browser
            // subprocesses that survived an earlier abnormal shutdown. A second pass
            // closes the small race where a dying CefClient creates one last child.
            var processNames = new[] { "CefClient", "CefSharp.BrowserSubprocess" };
            var terminatedProcessIds = new HashSet<int>();

            for (var pass = 1; pass <= 2; pass++)
            {
                foreach (var processName in processNames)
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                    {
                        using (process)
                        {
                            try
                            {
                                if (process.Id == Environment.ProcessId || process.HasExited)
                                    continue;

                                process.Kill(entireProcessTree: true);
                                terminatedProcessIds.Add(process.Id);
                                logger.LogWarning(
                                    "MainClient 启动清理残留进程：Name={ProcessName}, PID={ProcessId}, Pass={Pass}",
                                    process.ProcessName,
                                    process.Id,
                                    pass);
                            }
                            catch (InvalidOperationException)
                            {
                                // The process exited between enumeration and termination.
                            }
                            catch (System.ComponentModel.Win32Exception ex)
                            {
                                logger.LogWarning(
                                    ex,
                                    "MainClient 启动时无法清理残留进程：Name={ProcessName}, PID={ProcessId}",
                                    processName,
                                    SafeGetProcessId(process));
                            }
                        }
                    }
                }

                if (pass == 1)
                    Thread.Sleep(300);
            }

            logger.LogInformation(
                "MainClient 启动残留进程清理完成，共终止 {ProcessCount} 个进程",
                terminatedProcessIds.Count);
        }

        private static int SafeGetProcessId(Process process)
        {
            try
            {
                return process.Id;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }
    }
}
