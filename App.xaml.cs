using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Repeated_Files.Data;
using Repeated_Files.Services;
using Repeated_Files.ViewModels.Pages;
using Repeated_Files.ViewModels.Windows;
using Repeated_Files.Views.Pages;
using Repeated_Files.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace Repeated_Files
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        // .NET 通用主机提供依赖注入、配置、日志记录和其他服务。
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                // 添加数据库上下文
                services.AddDbContext<AppDbContext>(options =>
                {
                    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
                    options.UseSqlite($"Data Source={dbPath}");
                });

                // 注册数据库服务
                services.AddScoped<IDatabaseService, DatabaseService>();

                // 注册文件扫描服务
                services.AddScoped<IFileScanService, FileScanService>();

                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // 主题操作
                services.AddSingleton<IThemeService, ThemeService>();

                // 任务栏操作
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // 包含导航的服务，与 INavigationWindow 相同...但没有窗口
                services.AddSingleton<INavigationService, NavigationService>();



                // 带导航的主窗口
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                // todo 在这里注册，
            }).Build();

        /// <summary>
        /// 获取服务。
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// 当应用程序正在加载时发生。
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        /// <summary>
        /// 当应用程序正在关闭时发生。
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// 当应用程序抛出异常但未处理时发生。
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 有关更多信息，请参阅 https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
