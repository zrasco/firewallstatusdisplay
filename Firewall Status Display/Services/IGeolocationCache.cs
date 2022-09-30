using Firewall_Status_Display.Data.Models;

namespace Firewall_Status_Display.Services
{
    public interface IGeolocationCache
    {
        GeolocationEntry GetGeolocationInfo(string ipAddr);
    }
}