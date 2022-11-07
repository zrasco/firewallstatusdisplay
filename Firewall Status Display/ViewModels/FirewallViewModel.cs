using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class FirewallViewRowItem
    {
        public DateTime TimeStamp { get; set; }
        [Display(Name = "Src IP")]
        public string IPSrc { get; set; }
        [Display(Name = "Src port")]
        public int PortSrc { get; set; }
        [Display(Name = "Dst IP")]
        public string IPDest { get; set; }
        [Display(Name = "Dst port")]
        public int PortDest { get; set; }

        [Display(Name = "Protocol")]
        public string Proto { get; set; }
        
        public string SendingHost { get; set; }
        public string In { get; set; }       // IN=
        public string Out { get; set; }      // OUT=
        public string RuleName { get; set; } // DROP, ACCEPT, REJECT, etc...
        public string MacAddr { get; set; }

        public int Length { get; set; }
        public byte? TOS { get; set; }       // Deprecated
        public byte? Prec { get; set; }      // Deprecated
        public byte TTL { get; set; }
        public int PacketId { get; set; }

        public int Window { get; set; }
        public byte Res { get; set; }
        public int Flags { get; set; }

        // Geolocation info
        public string SrcCity { get; set; }
        public string SrcRegion { get; set; }
        public string SrcCountryCode { get; set; }
        public string DestCity { get; set; }
        public string DestRegion { get; set; }
        public string DestCountryCode { get; set; }
    }
    public class FirewallViewModel : ViewModelBase
    {
        private readonly IServiceProvider _services;

        // Design-time ctor
        public FirewallViewModel()
        {
            entryList = new ObservableCollection<FirewallViewRowItem>();
            AddItemCommand = new DelegateCommand(OnAddItemCommandExecute);
            ClearAllItemsCommand = new DelegateCommand(OnClearAllItemsCommandExecute);
        }

        private void OnClearAllItemsCommandExecute(object obj)
        {
            EntryList.Clear();
        }

        // Run-time ctor
        public FirewallViewModel(IServiceProvider services) : this()
        {
            _services = services;
        }

        private void OnAddItemCommandExecute(object obj)
        {
            var item = (FirewallViewRowItem)obj;

            if (item != null)
            {
                EntryList.Add(item);
            }
            else
            {
                // Not the correct object type
                throw new NotImplementedException();
            }
            
        }

        private ObservableCollection<FirewallViewRowItem> entryList { get; set; }

        public ObservableCollection<FirewallViewRowItem> EntryList
        {
            get { return entryList; }
            set { entryList = value; RaisePropertyChanged(); }
        }

        public ICommand AddItemCommand { get; set; }
        public ICommand ClearAllItemsCommand { get; set; }
    }
}
