﻿using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Services;
using Firewall_Status_Display.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public string Contents { get; set; }
        private readonly ISyslogReciever _syslogReciever;
        private readonly IServiceProvider _services;
        private readonly IDataRepoService _dataRepoService;
        private readonly CancellationTokenSource _commitCancellationTokenSource;

        public MainViewModel()
        {
            Contents = "Hello (from designer)";

            // Set up commands not requiring dependencies
            NavCommand = new DelegateCommand(OnNavBadgeCommandExecute);
            CancelDBLoopCommand = new DelegateCommand(OnCancelDBLoopCommandExecute);
            SetStatusTextCommand = new DelegateCommand(OnSetStatusTextCommandExecute);
            SetStatusColorCommand = new DelegateCommand(OnSetStatusColorCommandExecute);
        }

        private void OnCancelDBLoopCommandExecute(object obj)
        {
            _commitCancellationTokenSource.Cancel();
        }

        public MainViewModel(ISyslogReciever syslogReciever,
                                IServiceProvider services,
                                IDataRepoService dataRepoService) : this()
        {
            // Start syslog reciever
            _syslogReciever = syslogReciever;
            _syslogReciever.DataRecievedEvent += SyslogDataRecieved;
            
            // Start in background
            Task.Run(() => _syslogReciever.StartAsync());

            // Inject other dependencies
            _services = services;
            _dataRepoService = dataRepoService;

            // TODO: Add DelegateCommand when app is shut down
            _commitCancellationTokenSource = new CancellationTokenSource();
            // Run our commit every minute in background
            Task.Run(async () =>
            {
                bool exit = false;

                // Keep trying this during outages etc
                while (!exit)
                {
                    try
                    {
                        await Task.Delay(60000, _commitCancellationTokenSource.Token);
                        await _dataRepoService.CommitChangesAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        exit = true;
                    }
                }

                // Done here
                // Program is shutting down. Commit changes
                Debug.WriteLine("Program is shutting down. Making last DB commit...");
                await _dataRepoService.CommitChangesAsync();
                _commitCancellationTokenSource.Dispose();


            }, _commitCancellationTokenSource.Token);

        }

        private async void SyslogDataRecieved(object sender, RecievedDataArgs args)
        {
            var syslogVM = _services.GetRequiredService<SyslogViewModel>();
            var firewallVM = _services.GetRequiredService<FirewallViewModel>();
            var rawData = Encoding.Default.GetString(args.RecievedBytes);

            // Add to log output
            syslogVM.AppendLogCommand.Execute(rawData);
            Debug.Print($"Syslog recieved event for thread ID {Thread.CurrentThread.ManagedThreadId}");

            // Add to database
            var fwEntry = await _dataRepoService.AddFirewallEntryAsync(rawData);

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
            


            //await _dataRepoService.AddFirewallEntryAsync(rawData);
                     
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

        /// <summary>
        /// Status bar text. This will show on the bottom of the screen.
        /// </summary>
        private string statusText;

        public string StatusText
        {
            get { return statusText; }
            set { statusText = value; RaisePropertyChanged(); }
        }

        private Brush statusColor;

        public Brush StatusColor
        {
            get { return statusColor; }
            set { statusColor = value; RaisePropertyChanged(); }
        }

        #region Commands
        public ICommand SetStatusTextCommand { get; set; }
        public ICommand CancelDBLoopCommand { get; set; }

        private void OnSetStatusTextCommandExecute(object text)
        {
            StatusText = text as string;
        }

        public ICommand SetStatusColorCommand { get; set; }
        private void OnSetStatusColorCommandExecute(object color)
        {
            StatusColor = color as Brush;
        }

        public ICommand NavCommand { get; set; }

        private void OnNavBadgeCommandExecute(object targetView)
        {
            CurrentView = targetView;
        }

        #endregion
    }
}
