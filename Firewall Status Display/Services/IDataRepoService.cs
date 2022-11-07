using Firewall_Status_Display.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    public interface IDataRepoService
    {
        Task<int> CommitChangesAsync();
        Task<FirewallEntry> AddFirewallEntryAsync(string rawLogLine);
        GeolocationEntry GetByIP(string ipAddr);
        Task<List<FirewallEntry>> GetAllFirewallEntriesAsync();
        string GetCountryNameFrom2DigitCode(string countryCode);

        string GetServiceName(string protocol, ushort port);
    }
}