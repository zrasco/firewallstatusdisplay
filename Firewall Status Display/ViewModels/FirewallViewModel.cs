using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class FirewallViewModel : ViewModelBase
    {
        private readonly IServiceProvider _services;
        public string Content { get; set; }

        // Design-time ctor
        public FirewallViewModel()
        {
            Content = "Firewall view (design time)";
        }

        // Run-time ctor
        public FirewallViewModel(IServiceProvider services)
        {
            Content = "Firewall view (runtime)";
            _services = services;
        }
    }
}
