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

namespace Firewall_Status_Display
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hide the window instead of closing it
            this.Hide();

            // Show the systray notification
            icon.BalloonIconSource = icon.TrayIconSource;
            icon.BalloonTitle = "Firewall Status Display";
            icon.PopupShowDuration = 5000;
            icon.BalloonText = "This program will continue to run in the background. You can close it by right-clicking the tray icon.";

            icon.ShowBalloonTip();

            e.Cancel = true;
        }
    }
}
