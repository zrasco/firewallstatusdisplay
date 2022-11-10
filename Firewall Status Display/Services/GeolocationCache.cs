using CsvHelper.Configuration;
using CsvHelper;
using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        private GeolocationDataContext _geoContext;
        private IServiceProvider _services;

        // Statistics
        private int _misses;
        private int _hits;

        public GeolocationCache(GeolocationDataContext context, IServiceProvider services)
        {
            _ipEntries = new List<string>();
            _geoTable = new Dictionary<string, GeolocationEntry>();
            _geoContext = context;

            // Set statistics
            _misses = 0;
            _hits = 0;

            // Ensure table was created
            RelationalDatabaseCreator databaseCreator =
                (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

            try
            {
                databaseCreator.CreateTables();
            }
            catch { }

            _services = services;

            // Import!!
            //_ = ImportGeolocationCSVAsync(@"D:\Users\zrasco\Downloads\dbip-city-lite-2022-09.csv\dbip-city-lite-2022-09.csv").Result;
        }

        public GeolocationEntry GetGeolocationInfo(string ipAddr)
        {
            if (_geoTable.ContainsKey(ipAddr))
            {
                _hits++;
                //Debug.Print($"Cache hit for {ipAddr}");
            }
            else
            {
                _misses++;
                // Not in cache
                //Debug.Print($"Cache miss for {ipAddr}");

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
            //Debug.Print($"Retrieval for geolocation started for ID {Thread.CurrentThread.ManagedThreadId}");
            var retval = _geoContext.GeolocationEntries.Where(p => p.BeginningIP <= ip).OrderByDescending(p => p.BeginningIP).FirstOrDefault();
            //Debug.Print($"Retrieval for geolocation ended for ID {Thread.CurrentThread.ManagedThreadId}");
            return retval;

            // ~800ms for 255
            //return _context.GeolocationEntries.Where(p => p.BeginningIP <= ip && p.BeginningIP >= ip-65535 && p.EndingIP >= ip).FirstOrDefault();
        }

        public async Task<bool> ImportGeolocationCSVAsync(string pathName, bool tempFile)
        {
            var uiLogger = _services.GetRequiredService<UILogger>();

            uiLogger.LogInformation("Beginning Geolocation DB import. Reading CSV data...");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,

            };
            using (var reader = new StreamReader(pathName))
            {
                using (var csv = new CsvReader(reader, config))
                {
                    // Skip IPv6
                    var recordsAsyncIterator = csv.GetRecordsAsync<GeolocationCSVEntry>();
                    List<GeolocationCSVEntry> recordList = new List<GeolocationCSVEntry>();
                        //.TakeWhile(p => p.BeginningIPStr != "::");

                    // Delete all existing rows in the table
                    //await _geoContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE [{nameof(_geoContext.GeolocationEntries)}]");

                    await foreach (var record in recordsAsyncIterator)
                    {
                        if (record.BeginningIPStr.Contains("::"))
                            break;
                        recordList.Add(record);
                    }

                    if (tempFile)
                    {
                        uiLogger.LogInformation($"Temp file {pathName} deleted.");
                        File.Delete(pathName);
                    }

                    uiLogger.LogInformation("All CSV data read.");

                    uiLogger.LogInformation("Updating DB...");
                    _geoContext.GeolocationEntries.RemoveRange(_geoContext.GeolocationEntries);
                    await _geoContext.GeolocationEntries.AddRangeAsync(recordList.Select(x => new GeolocationEntry()
                    {
                        BeginningIP = ToInt(x.BeginningIPStr),
                        EndingIP = ToInt(x.EndingIPStr),
                        City = x.City,
                        EntryType = x.EntryType,
                        Iso2DigitCountryCode = x.Iso2DigitCountryCode,
                        Region = x.Region,
                        Latitude = x.Latitude,
                        Longitude = x.Longitude
                    }));
                }
            }

            // Async way
            await _geoContext.SaveChangesAsync();
            uiLogger.LogInformation($"Geolocation import complete. {_geoContext.GeolocationEntries.Count()} records imported");

            return true;
        }

        public int Entries()
        {
            return _ipEntries.Count;
        }

        public int Hits()
        {
            return _hits;
        }

        public int Misses()
        {
            return _misses;
        }

        public int Limit()
        {
            return CACHE_LIMIT;
        }
    }
}
