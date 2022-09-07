using Firewall_Status_Display.Services;
using Firewall_Status_Display.ViewModels;
using Firewall_Status_Display.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _services;
        private readonly ISyslogReciever _syslogReciever;
        public MainWindow(IServiceProvider services,
                            ISyslogReciever syslogReciever)
        {
            var res = App.Current.Resources;

            InitializeComponent();

            // Initialize the ViewModel dictionary
            var vm = (MainViewModel)DataContext;

            _services = services;

            vm.ViewModelDict["Status"] = services.GetRequiredService<StatusView>();
            vm.ViewModelDict["Syslog"] = services.GetRequiredService<SyslogView>();
            vm.ViewModelDict["Firewall log"] = services.GetRequiredService<FirewallView>();
            vm.ViewModelDict["Settings"] = services.GetRequiredService<SettingsView>();

            // Set current view
            navigationView.SelectedIndex = 0;

            // Set up UDP listener for when window loads
            _syslogReciever = syslogReciever;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hide the window instead of closing it
            this.Hide();

            if (Application.Current.Properties["CloseNotificationShown"] == null)
            {
                Application.Current.Properties["CloseNotificationShown"] = true;

                // Show the systray notification once per run
                
                icon.BalloonIconSource = icon.TrayIconSource;
                icon.BalloonTitle = "Firewall Status Display";
                icon.PopupShowDuration = 5000;
                icon.BalloonText = "This program will continue to run in the background. You can close it by right-clicking the tray icon.";

                icon.ShowBalloonTip();
                
            }

            // Cancel the close event so we don't exit the program
            e.Cancel = true;
        }

        private void icon_TrayIconMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Show window
            this.Show();
        }

        private void ctxMenuShow_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            this.Show();
        }

        private void ctxMenuExit_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void navigationView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var view = (RadNavigationView)sender;
            var selectedItem = (RadNavigationViewItem)view.SelectedValue;
            var selectedItemText = selectedItem.Content.ToString();

            // Get the VM
            var vm = (MainViewModel)DataContext;

            // Change the current navigation content pane
            vm.CurrentView = vm.ViewModelDict[selectedItemText];

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _syslogReciever.StartAsync();

            _syslogReciever.DataRecievedEvent += _syslogReciever_DataRecievedEvent;
        }

        private void _syslogReciever_DataRecievedEvent(object sender, RecievedDataArgs args)
        {
            MessageBox.Show(System.Text.Encoding.UTF8.GetString(args.RecievedBytes, 0, args.RecievedBytes.Length));
        }
    }
}
