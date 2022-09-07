using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display.ViewModels
{
    public class NavigationItemModel
    {
        public string Title { get; set; }
        public string IconGlyph { get; set; }
        public Type VMType { get; set; }
    }

    public class MainViewModel : ViewModelBase
    {

        public MainViewModel()
        {

        }
        public string MainWinVMString { get; set; } = "Hello from MainWindoViewModel";

        public ObservableCollection<NavigationItemModel> NavigationViewModelTypes { get; set; } = new ObservableCollection<NavigationItemModel>
        (
            new List<NavigationItemModel>
            {
                new NavigationItemModel{ Title="Status", IconGlyph="&#xe137;", VMType=typeof(StatusViewModel) },
                new NavigationItemModel{ Title="Syslog", IconGlyph="&#xe908;", VMType=typeof(SyslogViewModel) },
            }
        );

        private object currentViewModel;

        public object CurrentViewModel
        {
            get { return currentViewModel; }
            set { currentViewModel = value; RaisePropertyChanged(); }
        }
    }
}
