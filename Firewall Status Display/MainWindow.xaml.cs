using Firewall_Status_Display.Services;
using Firewall_Status_Display.ViewModels;
using Firewall_Status_Display.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        private readonly UILogger _uiLogger;
        public MainWindow(IServiceProvider services, UILogger uiLogger) : this()
        {
            _services = services;
            _uiLogger = uiLogger;

            // Set current view
            var vm = (MainViewModel)DataContext;
            navigationView.SelectedItem = navigationView.Items[0];
            vm.NavCommand.Execute(_services.GetRequiredService<StatusView>());

            _uiLogger.SetStatusText("Ready", StatusTextColor.Ok);
        }

        public MainWindow()
        {
            InitializeComponent();

            // Set status text
            var vm = (MainViewModel)DataContext;
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
            // Wait for DB commit
            ((MainViewModel)DataContext).CancelDBLoopCommand.Execute(null);
            Task.Delay(5000);

            this.icon.Dispose();
            this.icon = null;
            Application.Current.Properties["CloseNotificationShown"] = true;
            Application.Current.Shutdown();
        }
    }
}
