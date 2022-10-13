using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Services;
using Firewall_Status_Display.ViewModels;
using Firewall_Status_Display.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }
        public App()
        {

            var builder = Host.CreateDefaultBuilder();

            AppHost = builder
                .ConfigureServices((hostContext, services) =>
                {
                    // Windows & controls
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<StatusView>();
                    services.AddSingleton<SyslogView>();
                    services.AddSingleton<FirewallView>();
                    services.AddSingleton<SettingsView>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<StatusViewModel>();
                    services.AddSingleton<SyslogViewModel>();
                    services.AddSingleton<FirewallViewModel>();
                    services.AddSingleton<SettingsViewModel>();

                    // DB & repo
                    services.AddDbContext<FirewallDataContext>();
                    services.AddDbContext<GeolocationDataContext>();
                    services.AddSingleton<IGeolocationCache, GeolocationCache>();
                    services.AddHttpClient<IDataRepoService, DataRepoService>();
                    

                    // Other services
                    services.AddSingleton<ISyslogReciever, SyslogReciever>();

                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();

            startupForm.Show();

            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            var vm = AppHost.Services.GetService<MainViewModel>();

            await AppHost.StopAsync();

            base.OnExit(e);
        }
    }
}
