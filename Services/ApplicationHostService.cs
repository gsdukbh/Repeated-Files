using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Repeated_Files.Views.Pages;
using Repeated_Files.Views.Windows;
using Repeated_Files.Data;
using Wpf.Ui;
using Application = System.Windows.Application;

namespace Repeated_Files.Services
{
    /// <summary>
    /// 应用程序的托管主机。
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        private INavigationWindow _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 当应用程序主机准备好启动服务时触发。
        /// </summary>
        /// <param name="cancellationToken">表示启动过程已被中止。</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 初始化数据库
            await InitializeDatabaseAsync();
            
            await HandleActivationAsync();
        }

        /// <summary>
        /// 当应用程序主机正在执行优雅关闭时触发。
        /// </summary>
        /// <param name="cancellationToken">表示关闭过程不应再是优雅的。</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 在激活期间创建主窗口。
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _navigationWindow = (
                    _serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow
                )!;
                _navigationWindow!.ShowWindow();

                _navigationWindow.Navigate(typeof(Views.Pages.DashboardPage));
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // 确保数据库被创建
            await dbContext.Database.EnsureCreatedAsync();
            // 清空数据库中的数据（如果需要）
          
        }
    }
}
