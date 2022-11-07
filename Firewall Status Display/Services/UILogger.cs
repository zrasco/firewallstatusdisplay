using Firewall_Status_Display.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Firewall_Status_Display.Services
{
    public enum StatusTextColor
    {
        Ok = 0,
        Warning = 1,
        Error = 2,
        Default = 3
    }

    public sealed class UILogger : ILogger
    {
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IServiceProvider _services;
        public UILogger(SettingsViewModel settingsViewModel, IServiceProvider services)
        {
            _settingsViewModel = settingsViewModel;
            _services = services;
        }
        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Log the event to the screen
            _settingsViewModel.AppendLogCommand.Execute($"[{logLevel.ToString()}] {formatter(state, exception)}{(exception != null ? $" - {exception.Message}" : "")}");

            // TODO: log event to file
        }

        public void ShowTrayNotification(string text)
        {
            var wnd = _services.GetRequiredService<MainWindow>();

            App.Current.Dispatcher.Invoke(() =>
            {
                wnd.icon.BalloonIconSource = wnd.icon.TrayIconSource;
                wnd.icon.BalloonTitle = "Firewall Status Display";
                wnd.icon.PopupShowDuration = 5000;
                wnd.icon.BalloonText = text;

                wnd.icon.ShowBalloonTip();
            });


        }


        public void SetStatusText(string statusText, StatusTextColor statusColor)
        {
            var vm = _services.GetRequiredService<MainViewModel>();

            Dictionary<StatusTextColor, Brush> colorDict = new Dictionary<StatusTextColor, Brush>();
            colorDict[StatusTextColor.Ok] = Brushes.Green;
            colorDict[StatusTextColor.Warning] = Brushes.YellowGreen;
            colorDict[StatusTextColor.Error] = Brushes.Red;
            colorDict[StatusTextColor.Default] = Brushes.Black;

            // Default to black
            if (!colorDict.ContainsKey(statusColor))
                statusColor = StatusTextColor.Default;

            // Invoke the command to set the color
            vm.SetStatusTextCommand.Execute(statusText);
            vm.SetStatusColorCommand.Execute(colorDict[statusColor]);

        }
    }
}
