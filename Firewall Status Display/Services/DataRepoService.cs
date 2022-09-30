﻿using CsvHelper;
using CsvHelper.Configuration;
using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using MediaFoundation.ReadWrite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Samples.ObjectDataReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Printing;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Diagrams.Core;
using Telerik.Windows.Documents.Spreadsheet.Model;
using Index = CsvHelper.Configuration.Attributes.IndexAttribute;

namespace Firewall_Status_Display.Services
{
    public class GeolocationCSVEntry
    {
        [@Index(0)]
        public string BeginningIPStr { get; set; }
        [@Index(1)]
        public string EndingIPStr { get; set; }
        [@Index(2)]
        public string EntryType { get; set; } // AS, OS, etc...
        [@Index(3)]
        public string Iso2DigitCountryCode { get; set; }
        [@Index(4)]
        public string City { get; set; }
        [@Index(5)]
        public string Region { get; set; }
        [@Index(6)]
        public double Latitude { get; set; }
        [@Index(7)]
        public double Longitude { get; set; }
    }
    public class IpAPIRequestWrapper
    {
        public FirewallEntry Entity { get; set; }
        public IpAPIRequest Request { get; set; }

    }
    public class IpAPIRequest
    {
        public string lang { get; set; }
        public string query { get; set; }
        public string fields { get; set; }
    }
    public class IpAPIResponse
    {
        //status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,query
        public string status { get; set; }
        public string message { get; set; }
        public string country { get; set; }
        public string countryCode { get; set; }
        public string region { get; set; }
        public string regionName { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public float lat { get; set; }
        public float lon { get; set; }
        public string timezone { get; set; }
        public string isp { get; set; }
        public string org { get; set; }
        [JsonPropertyName("as")]
        public string AS { get; set; }
        public string query { get; set; }
    }
    public class DataRepoService : IDataRepoService
    {
        // Must be 50 or less for ip-api
        private const int ENTRIES_BEFORE_GEOLOCATION = 4;

        private readonly FirewallDataContext _fwContext;
        private readonly HttpClient _httpClient;
        private readonly IGeolocationCache _geolocationCache;
        public DataRepoService(FirewallDataContext context,
                                HttpClient httpClient,
                                IGeolocationCache geolocationCache)
        {
            _fwContext = context;
            _httpClient = httpClient;

            // Make sure DB is created
            var created = context.Database.EnsureCreated();

            _httpClient.BaseAddress = new Uri("http://ip-api.com/batch");
            _geolocationCache = geolocationCache;
        }

        public async Task<int> CommitChangesAsync()
        {
            int commits = _fwContext.ChangeTracker.Entries().Count();
            
            if (commits > 0)
            {
                Debug.Print($"Committing {commits} firewall entries to database");
                await _fwContext.SaveChangesAsync();
                Debug.Print($"Done committing");
            }
            return commits;
        }

        public async Task<bool> AddFirewallEntryAsync(string rawLogLine)
        {
            try
            {
                // Check if this is a firewall line. If not, skip
                var fwEntry = ParseIntoFirewallEntry(rawLogLine);
                if (fwEntry != null)
                {
                    _fwContext.FirewallEntries.Add(fwEntry);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private FirewallEntry ParseIntoFirewallEntry(string rawData)
        {
            var retval = new FirewallEntry();

            // Split into whitespace
            var splits = rawData.Split(null);

            foreach (var item in splits)
            {
                if (item.Contains("SRC="))
                {
                    retval.IPSrc = item.Split("=")[1];
                }
                else if (item.Contains("DST="))
                {
                    retval.IPDest = item.Split("=")[1];
                }
            }

            if (retval.IPSrc == null)
            {
                // Need a source IP to consider this valid. Otherwise we discard it
                return null;
            }
            else
            {
                retval.TimeStamp = DateTime.Now;

                // Additional processing
                if (!IsIPPrivate(retval.IPSrc))
                {
                    // Not private. Look up geolocation info
                    var srcGeoInfo = _geolocationCache.GetGeolocationInfo(retval.IPSrc);
                    retval.SrcCountryCode = srcGeoInfo.Iso2DigitCountryCode;
                    retval.SrcCity = srcGeoInfo.City;
                    retval.SrcRegion = srcGeoInfo.Region;
                }
                if (!IsIPPrivate(retval.IPDest))
                {
                    // Not private. Look up geolocation info
                    var destGeoInfo = _geolocationCache.GetGeolocationInfo(retval.IPDest);
                    retval.DestCountryCode = destGeoInfo.Iso2DigitCountryCode;
                    retval.DestCity = destGeoInfo.City;
                    retval.DestRegion = destGeoInfo.Region;
                }

                return retval;
            }
                
        }

        // IP conversion functions
        // Source: https://stackoverflow.com/questions/461742/how-to-convert-an-ipv4-address-into-a-integer-in-c
        // User: Barry Kelly
        static uint ToInt(string addr)
        {
            // careful of sign extension: convert to uint first;
            // unsigned NetworkToHostOrder ought to be provided.
            return (uint)IPAddress.NetworkToHostOrder((int)IPAddress.Parse(addr).Address);
        }

        static string ToAddr(int address)
        {
            return IPAddress.Parse(address.ToString()).ToString();
            // This also works:
            // return new IPAddress((uint) IPAddress.HostToNetworkOrder(
            //    (int) address)).ToString();
        }

        public GeolocationEntry GetByIP(string ipAddr)
        {
            return _geolocationCache.GetGeolocationInfo(ipAddr);

        }

        // Used from: https://stackoverflow.com/questions/8113546/how-to-determine-whether-an-ip-address-is-private
        // User: Gabriel Graves
        private bool IsIPPrivate(string ipAddress)
        {
            int[] ipParts = ipAddress.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => int.Parse(s)).ToArray();
            // in private ip range
            if (ipParts[0] == 10 ||
                (ipParts[0] == 192 && ipParts[1] == 168) ||
                (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31)))
            {
                return true;
            }

            // IP Address is probably public.
            // This doesn't catch some VPN ranges like OpenVPN and Hamachi.
            return false;
        }
    }
}
