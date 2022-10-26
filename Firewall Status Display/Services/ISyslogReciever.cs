using System.Threading;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    // Inspired by Maguli Geci's YouTube video on simple UDP server
    // https://www.youtube.com/watch?v=DYKXJOAku7M
    public interface ISyslogReciever
    {
        event SyslogReciever.DataRecieved DataRecievedEvent;

        Task<bool> StartAsync(int syslogPort, CancellationToken cancellationToken = default);
        Task StopAsync();
    }
}