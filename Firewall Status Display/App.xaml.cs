using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Set theme here
            StyleManager.ApplicationTheme = new Office2016Theme();

            this.InitializeComponent();
        }
    }
}
