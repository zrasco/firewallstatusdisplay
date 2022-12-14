using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display.ViewModels
{
    public class SyslogViewModel : ViewModelBase
    {
        public SyslogViewModel()
        {
            Content = "";

            AppendLogCommand = new DelegateCommand(OnAppendLogCommandExecute);
        }

        private void OnAppendLogCommandExecute(object logEntry)
        {
            var logLine = logEntry.ToString().Trim();
            Content += $"{logLine}\r\n";
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
        #endregion
    }
}
