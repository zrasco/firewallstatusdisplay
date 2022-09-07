using Firewall_Status_Display.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            Contents = "Hello";

            ViewModelDict = new Dictionary<string, UserControl>();
        }


        private object currentView;

        public object CurrentView
        {
            get { return currentView; }
            set { currentView = value; RaisePropertyChanged(); }
        }
    }
}
