using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Services
{
    /// <summary>
    /// Stores geolocation data locally since we often have repeat IP addresses
    /// </summary>
    public class GeolocationCache : IGeolocationCache
    {
        private readonly int CACHE_LIMIT = 10000;

        private List<string> _ipEntries;
        private Dictionary<string, GeolocationEntry> _geoTable;
        private FirewallDataContext _context;
        public GeolocationCache(FirewallDataContext context)
        {
            _ipEntries = new List<string>();
            _geoTable = new Dictionary<string, GeolocationEntry>();
            _context = context;
        }

        public GeolocationEntry GetGeolocationInfo(string ipAddr)
        {
            if (_geoTable.ContainsKey(ipAddr))
            {
                Debug.Print($"Cache hit for {ipAddr}");
            }
            else
            {
                // Not in cache
                Debug.Print($"Cache miss for {ipAddr}");

                // Check if cache is full
                while (_ipEntries.Count >= CACHE_LIMIT)
                {
                    _geoTable.Remove(_ipEntries[0]);
                    _ipEntries.RemoveAt(0);
                }

                // Do DB lookup then add to cache
                GeolocationEntry entry = GetByIP(ipAddr);

                _ipEntries.Add(ipAddr);
                _geoTable[ipAddr] = entry;
            }

            return _geoTable[ipAddr];
        }

        // IP conversion functions
        // Source: https://stackoverflow.com/questions/461742/how-to-convert-an-ipv4-address-into-a-integer-in-c
        // User: Barry Kelly
        private uint ToInt(string addr)
        {
            // careful of sign extension: convert to uint first;
            // unsigned NetworkToHostOrder ought to be provided.
            return (uint)IPAddress.NetworkToHostOrder((int)IPAddress.Parse(addr).Address);
        }

        private string ToAddr(int address)
        {
            return IPAddress.Parse(address.ToString()).ToString();
            // This also works:
            // return new IPAddress((uint) IPAddress.HostToNetworkOrder(
            //    (int) address)).ToString();
        }

        private GeolocationEntry GetByIP(string ipAddr)
        {
            long ip = ToInt(ipAddr);

            // ~720ms for 255 entries
            Debug.Print($"Retrieval for geolocation started for ID {Thread.CurrentThread.ManagedThreadId}");
            var retval = _context.GeolocationEntries.Where(p => p.BeginningIP <= ip).OrderByDescending(p => p.BeginningIP).FirstOrDefault();
            Debug.Print($"Retrieval for geolocation ended for ID {Thread.CurrentThread.ManagedThreadId}");
            return retval;

            // ~800ms for 255
            //return _context.GeolocationEntries.Where(p => p.BeginningIP <= ip && p.BeginningIP >= ip-65535 && p.EndingIP >= ip).FirstOrDefault();
        }
    }
}
