using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    public interface IDataRepoService
    {
        Task<bool> AddFirewallEntry(string rawLogLine);
    }
}