using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display.ViewModels
{
    public class SyslogViewModel : ViewModelBase
    {
        public string Content { get; set; }
        public SyslogViewModel()
        {
            Content = "Syslog view";
        }

    }
}
