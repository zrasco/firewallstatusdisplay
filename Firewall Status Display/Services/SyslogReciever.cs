using Microsoft.Extensions.Logging;
using SharpDX.Direct3D10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Firewall_Status_Display.Services
{
    public class RecievedDataArgs
    {
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
        public byte[] RecievedBytes;

        public RecievedDataArgs(IPAddress iPAddress, int port, byte[] recievedBytes)
        {
            IPAddress = iPAddress ?? throw new ArgumentNullException(nameof(iPAddress));
            Port = port;
            RecievedBytes = recievedBytes ?? throw new ArgumentNullException(nameof(recievedBytes));
        }
    }

    public class SyslogReciever : ISyslogReciever
    {
        private Memory<byte> _buffer;
        private UdpClient _listener;
        private readonly UILogger _uiLogger;

        public delegate void DataRecieved(object sender, RecievedDataArgs args);
        public event DataRecieved DataRecievedEvent;

        public SyslogReciever(UILogger uiLogger)
        {
            _uiLogger = uiLogger;
        }

        public async Task<bool> StartAsync(int syslogPort, CancellationToken cancellationToken = new CancellationToken())
        {
            _buffer = new byte[65527];

            try
            {                
                _listener = new UdpClient(new IPEndPoint(IPAddress.Any, syslogPort));

                // If no exception we can log success
                _uiLogger.SetStatusText($"Ready. Listening on UDP port {syslogPort}.", StatusTextColor.Ok);
                _uiLogger.LogInformation($"Listening on UDP port {syslogPort}.");

                await RecieveLoopAsync(cancellationToken);

                return true;
            }
            catch(Exception ex)
            {
                _uiLogger.SetStatusText($"Unable to listen on UDP port {syslogPort}!", StatusTextColor.Error);
                _uiLogger.LogError(ex, $"Failed to create listening socket on UDP port {syslogPort}. Will try again once per minute.");
                return false;
            }


        }

        public async Task RecieveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var res = await _listener.ReceiveAsync(cancellationToken);

                // Raise event
                await RaiseDataRecievedAsync(new RecievedDataArgs(res.RemoteEndPoint.Address, res.RemoteEndPoint.Port, res.Buffer));
            }
        }

        public Task StopAsync()
        {
            _buffer = null;

            return null;
        }

        private async Task RaiseDataRecievedAsync(RecievedDataArgs args)
        {
            DataRecievedEvent?.Invoke(this, args);
            //Application.Current.Dispatcher.Invoke(() => DataRecievedEvent?.Invoke(this, args));
        }
    }
}
