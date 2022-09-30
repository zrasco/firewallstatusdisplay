using Firewall_Status_Display.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    public interface IDataRepoService
    {
        Task<bool> AddFirewallEntryAsync(string rawLogLine);
        Task<bool> ImportGeolocationCSV(string pathName);
        GeolocationEntry GetByIP(string ipAddr);
    }
}