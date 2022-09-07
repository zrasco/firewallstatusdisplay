using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public string Content { get; set; }

        public SettingsViewModel()
        {
            Content = "Settings view";
        }
    }
}
