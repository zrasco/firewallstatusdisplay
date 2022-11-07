using Firewall_Status_Display.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IServiceProvider _services;
        private readonly IDataRepoService _dataRepoService;

        public SettingsViewModel(IServiceProvider serviceProvider,
                                    IDataRepoService dataRepoService) : this()
        {
            _services = serviceProvider;
            _dataRepoService = dataRepoService;

        }
        public SettingsViewModel()
        {
            Content = "";

            AppendLogCommand = new DelegateCommand(OnAppendLogCommandExecute);
            ImportDBCommand = new DelegateCommand(async (obj) => await OnImportDBCommandExecute(obj));
        }

        private async Task OnImportDBCommandExecute(object obj)
        {
            var firewallVM = _services.GetRequiredService<FirewallViewModel>();
            var entries = await _dataRepoService.GetAllFirewallEntriesAsync();

            // Clear the current firewall entries
            firewallVM.ClearAllItemsCommand.Execute(null);

            foreach (var entry in entries)
            {
                var fwEntry = entry;

                // Add to firewall view
                if (fwEntry != null)
                {
                    var rowEntry = new FirewallViewRowItem()
                    {
                        MacAddr = fwEntry.MacAddr,
                        IPDest = fwEntry.IPDest,
                        IPSrc = fwEntry.IPSrc,
                        In = fwEntry.In,
                        Out = fwEntry.Out,
                        PortDest = fwEntry.PortDest,
                        PortSrc = fwEntry.PortSrc,
                        Flags = fwEntry.Flags,
                        Length = fwEntry.Length,
                        PacketId = fwEntry.PacketId,
                        Proto = fwEntry.Proto,
                        RuleName = fwEntry.RuleName,
                        Prec = fwEntry.Prec,
                        Res = fwEntry.Res,
                        SendingHost = fwEntry.SendingHost,
                        TimeStamp = fwEntry.TimeStamp,
                        TOS = fwEntry.TOS,
                        TTL = fwEntry.TTL,
                        Window = fwEntry.TTL,
                        DestCity = fwEntry.DestCity,
                        DestRegion = fwEntry.DestRegion,
                        DestCountryCode = fwEntry.DestCountryCode,
                        SrcCity = fwEntry.SrcCity,
                        SrcRegion = fwEntry.SrcRegion,
                        SrcCountryCode = fwEntry.SrcCountryCode
                    };

                    firewallVM.AddItemCommand.Execute(rowEntry);
                }
            }

            
        }

        private void OnAppendLogCommandExecute(object logEntry)
        {
            Content += $"{logEntry.ToString()}\r\n";
        }

        private string content;

        /// <summary>
        /// Log content buffer
        /// </summary>
        public string Content
        {
            get { return content; }
            set { content = value; RaisePropertyChanged(); }
        }

        #region Commands
        public ICommand AppendLogCommand { get; set; }
        public ICommand ImportDBCommand { get; set; }
        #endregion
    }
}
