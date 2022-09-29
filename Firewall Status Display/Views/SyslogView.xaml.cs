using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Firewall_Status_Display.Views
{
    /// <summary>
    /// Interaction logic for SyslogView.xaml
    /// </summary>
    public partial class SyslogView : UserControl
    {
        public SyslogView()
        {
            InitializeComponent();
        }

        private void txtBoxSyslog_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Automatically scroll to end when log is appended
            //txtBoxSyslog.ScrollToEnd();
        }
    }
}
