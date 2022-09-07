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

        public delegate void DataRecieved(object sender, RecievedDataArgs args);
        public event DataRecieved DataRecievedEvent;

        public SyslogReciever()
        {

        }

        public async Task<bool> StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _buffer = new byte[65527];

            try
            {
                _listener = new UdpClient(new IPEndPoint(IPAddress.Any, 514));

                await RecieveLoopAsync(cancellationToken);

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }


        }

        public async Task RecieveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var res = await _listener.ReceiveAsync(cancellationToken);

                // Raise event
                RaiseDataRecieved(new RecievedDataArgs(res.RemoteEndPoint.Address, res.RemoteEndPoint.Port, res.Buffer));
            }
        }

        public Task StopAsync()
        {
            _buffer = null;

            return null;
        }

        private void RaiseDataRecieved(RecievedDataArgs args)
        {
            if (DataRecievedEvent != null)
                DataRecievedEvent(this, args);

        }
    }
}
