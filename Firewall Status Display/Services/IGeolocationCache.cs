using Firewall_Status_Display.Data.Models;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    public interface IGeolocationCache
    {
        Task<bool> ImportGeolocationCSV(string pathName);
        GeolocationEntry GetGeolocationInfo(string ipAddr);

        // Statistic functions
        int Entries();
        int Limit();
        int Hits();
        int Misses();
    }
}