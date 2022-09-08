using Firewall_Status_Display.Services;
using Firewall_Status_Display.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public Dictionary<string, UserControl> ViewModelDict { get; set; }
        public string Contents { get; set; }

        public MainViewModel()
        {
            Contents = "Hello (from designer)";

            ViewModelDict = new Dictionary<string, UserControl>();
        }

        private object currentView;

        public object CurrentView
        {
            get { return currentView; }
            set { currentView = value; RaisePropertyChanged(); }
        }

        public Guid IconGuid
        {
            get { 
#if DEBUG
                var guid = "a6d76166-f2ac-4c9e-b86a-9f1825d08f6c";
#else
                var guid = "a0cee5f3-0d1b-4cea-9692-04d4d05a047c";
#endif
                return new Guid(guid);
            }
        }
    }
}
