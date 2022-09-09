using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Firewall_Status_Display.Views
{
    public class ViewLocator
    {
        private DependencyObject _dummy;
        public ViewLocator()
        {
            _dummy = new DependencyObject();
        }

        public MainWindow MainWindow => GetService<MainWindow>();
        public StatusView StatusView => GetService<StatusView>();
        public FirewallView FirewallView => GetService<FirewallView>();
        public SyslogView SyslogView => GetService<SyslogView>();
        public SettingsView SettingsView => GetService<SettingsView>();

        private T GetService<T>() where T : ContentControl, new()
        {
            if (DesignerProperties.GetIsInDesignMode(_dummy))
                return new T();
            else
                return App.AppHost.Services.GetRequiredService<T>() as T;
        }
    }
}
