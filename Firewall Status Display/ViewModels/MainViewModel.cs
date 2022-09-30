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

        public MainViewModel()
        {
            Contents = "Hello (from designer)";

            // Set up commands not requiring dependencies
            NavCommand = new DelegateCommand(OnNavBadgeCommandExecute);
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

            // Run our commit every minute in background
            Task.Run(async () =>
            {
                await Task.Delay(60000);
                await _dataRepoService.CommitChangesAsync();
            });

            //_dataRepoService.ImportGeolocationCSV(@"D:\Users\zrasco\Downloads\dbip-city-lite-2022-09.csv\dbip-city-lite-2022-09.csv");

        }

        private async void SyslogDataRecieved(object sender, RecievedDataArgs args)
        {
            var vm = _services.GetRequiredService<SyslogViewModel>();
            var rawData = Encoding.Default.GetString(args.RecievedBytes);

            // Add to log output
            vm.AppendLogCommand.Execute(rawData);
            Debug.Print($"Syslog recieved event for thread ID {Thread.CurrentThread.ManagedThreadId}");


            await _dataRepoService.AddFirewallEntryAsync(rawData);
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
