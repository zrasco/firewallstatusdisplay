using MediaFoundation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GanttView;

namespace Firewall_Status_Display.ViewModels
{
    public class ViewModelLocator
    {
        private DependencyObject _dummy;
        public ViewModelLocator()
        {
            _dummy = new DependencyObject();
        }

        public MainViewModel MainViewModel => GetService<MainViewModel>();
        public StatusViewModel StatusViewModel => GetService<StatusViewModel>();
        public SyslogViewModel SyslogViewModel => GetService<SyslogViewModel>();
        public FirewallViewModel FirewallViewModel => GetService<FirewallViewModel>();
        public SettingsViewModel SettingsViewModel => GetService<SettingsViewModel>();
        private T GetService<T>() where T: ViewModelBase, new()
        {
            if (DesignerProperties.GetIsInDesignMode(_dummy))
                return new T();
            else
                return App.AppHost.Services.GetRequiredService<T>() as T;
        }

    }
}
