using Firewall_Status_Display.Services;
using Firewall_Status_Display.ViewModels;
using Firewall_Status_Display.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
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
            // Set theme here
            StyleManager.ApplicationTheme = new Office2016Theme();

            //this.InitializeComponent();
            AppHost = Host.CreateDefaultBuilder()
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
            await AppHost.StopAsync();

            base.OnExit(e);
        }
    }
}
