using CsvHelper;
using CsvHelper.Configuration;
using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using MediaFoundation.ReadWrite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
    // Source for ServiceInfo class/code:
    // https://stackoverflow.com/questions/13246099/using-c-sharp-to-reference-a-port-number-to-service-name
    // Author: Mohammad Banisaeid
    public class ServiceInfo
    {
        public ushort Port { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
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
        private readonly FirewallDataContext _fwContext;
        private readonly HttpClient _httpClient;
        private readonly IGeolocationCache _geolocationCache;
        private readonly Dictionary<string, string> _countryList;
        private readonly List<ServiceInfo> _serviceList;
        private Queue<string> _entryQueue;
        public DataRepoService(FirewallDataContext fwContext,
                                GeolocationDataContext geoContext,
                                HttpClient httpClient,
                                IGeolocationCache geolocationCache)
        {
            _fwContext = fwContext;
            _httpClient = httpClient;

            // Make sure DB is created
            fwContext.Database.EnsureCreated();

            // Ensure table was created
            RelationalDatabaseCreator databaseCreator =
                (RelationalDatabaseCreator)fwContext.Database.GetService<IDatabaseCreator>();

            try
            {
                databaseCreator.CreateTables();
            }
            catch { }
            

            _httpClient.BaseAddress = new Uri("http://ip-api.com/batch");
            _geolocationCache = geolocationCache;

            // Create service list
            _serviceList = ReadServicesFile();

            // Create the country list
            _countryList = new Dictionary<string, string>();
            CultureInfo[] getCultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (CultureInfo getCulture in getCultureInfo)
            {
                RegionInfo getRegionInfo = new RegionInfo(getCulture.LCID);

                //add each country into the Dictionary of code/name - no duplicate keys
                if (!(_countryList.ContainsKey(getRegionInfo.Name)))
                {
                    _countryList.Add(getRegionInfo.TwoLetterISORegionName, getRegionInfo.EnglishName);
                }
            }

            // Create entry queue for failures
            _entryQueue = new Queue<string>();
        }

        public async Task<int> CommitChangesAsync()
        {
            int commits = _fwContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).Count();
            
            if (commits > 0)
            {
                Debug.Print($"Committing {commits} firewall entries to database");
                await _fwContext.SaveChangesAsync();
                Debug.Print($"Done committing");
            }
            return commits;
        }

        public async Task<List<FirewallEntry>> AddFirewallEntryAsync(string rawLogLine)
        {
            List<FirewallEntry> entries = new List<FirewallEntry>();
            FirewallEntry fwEntry = null;

            try
            {
                // Add to queue
                _entryQueue.Enqueue(rawLogLine);

                // Process enqueued items
                while (_entryQueue.TryPeek(out string result))
                {
                    // Check if this is a firewall line. If not, skip
                    fwEntry = ParseIntoFirewallEntry(result);

                    // If this fails it will throw an exception
                    if (fwEntry != null)
                    {
                        await _fwContext.FirewallEntries.AddAsync(fwEntry);
                        entries.Add(fwEntry);
                    }

                    
                    _entryQueue.Dequeue();
                }

            }
            catch
            {
                // Error in parsing (probably geolocation being updated or DB issue)
                return entries;
            }



            return entries;
        }

        private FirewallEntry ParseIntoFirewallEntry(string rawData)
        {
            var retval = new FirewallEntry();

            // Split into whitespace
            var splits = rawData.Split(null);

            foreach (var item in splits)
            {
                // String fields
                if (item.Contains("SRC="))
                {
                    // Source IP address
                    retval.IPSrc = item.Split("=")[1];
                }
                else if (item.Contains("DST="))
                {
                    // Destination IP address
                    retval.IPDest = item.Split("=")[1];
                }
                else if (item.Contains("MAC="))
                {
                    // MAC address
                    retval.MacAddr = item.Split("=")[1];
                }
                else if (item.Contains("IN="))
                {
                    retval.In = item.Split("=")[1];
                }
                else if (item.Contains("OUT="))
                {
                    retval.Out = item.Split("=")[1];
                }
                else if (item.Contains("PROTO="))
                {
                    retval.Proto = item.Split("=")[1];
                }
                // All integer fields
                else if (item.Contains("SPT="))
                {
                    // Source 
                    if (Int32.TryParse(item.Split("=")[1], out int sPrt))
                        retval.PortSrc = sPrt;
                }
                else if (item.Contains("DPT="))
                {
                    if (Int32.TryParse(item.Split("=")[1], out int dPrt))
                        retval.PortDest = dPrt;
                }
                else if (item.Contains("ID="))
                {
                    // Packet ID
                    if (Int32.TryParse(item.Split("=")[1], out int pId))
                        retval.PacketId = pId;
                }
                else if (item.Contains("LEN="))
                {
                    // Packet length
                    if (Int32.TryParse(item.Split("=")[1], out int len))
                        retval.Length = len;
                }
                // Bytes
                else if (item.Contains("TOS="))
                {
                    // TOS (Time of Service)
                    if (Byte.TryParse(item.Split("=")[1].Substring(2), NumberStyles.HexNumber, null, out byte tos))
                        retval.TOS = tos;
                }
                else if (item.Contains("PREC="))
                {
                    // Prec
                    if (Byte.TryParse(item.Split("=")[1].Substring(2), NumberStyles.HexNumber, null, out byte prec))
                        retval.Prec = prec;
                }
                else if (item.Contains("RES="))
                {
                    // Reserved
                    if (Int32.TryParse(item.Split("=")[1], out int res))
                        retval.Res = (byte)res;
                }
                else if (item.Contains("TTL="))
                {
                    // Time-To-Live
                    if (Int32.TryParse(item.Split("=")[1], out int ttl))
                        retval.TTL = (byte)ttl;
                }
                else if (item.Contains("WINDOW="))
                {
                    // Window
                    if (Int32.TryParse(item.Split("=")[1], out int wdw))
                        retval.Window = wdw;
                }
                else if (item == "DROP" || item == "ACCEPT" || item == "REJECT")
                {
                    retval.RuleName = item;
                }
                else
                {
                    Dictionary<string, int> flagDict = new Dictionary<string, int>();
                    flagDict["URGP"] = 0x00100000;
                    flagDict["ACK"] = 0x00010000;
                    flagDict["PSH"] = 0x00001000;
                    flagDict["RST"] = 0x00000100;
                    flagDict["SYN"] = 0x00000010;
                    flagDict["FIN"] = 0x00000001;

                    // Check for flags
                    if (flagDict.ContainsKey(item))
                    {
                        retval.Flags = retval.Flags | flagDict[item];
                    }
                    else if (item.Contains("UGRP"))
                    {
                        if (Int32.TryParse(item.Split("=")[1], out int ugrp))
                            if (ugrp > 0)
                                retval.Flags = retval.Flags | flagDict["URGP"];
                    }
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

        public async Task<List<FirewallEntry>> GetAllFirewallEntriesAsync()
        {
            return await _fwContext.FirewallEntries.ToListAsync();
        }

        public string GetCountryNameFrom2DigitCode(string countryCode)
        {
            if (_countryList.ContainsKey(countryCode))
                return _countryList[countryCode];
            else
                return null;
        }

        // Source for ServiceInfo class/code:
        // https://stackoverflow.com/questions/13246099/using-c-sharp-to-reference-a-port-number-to-service-name
        // Author: Mohammad Banisaeid

        private List<ServiceInfo> ReadServicesFile()
        {
            var sysFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!sysFolder.EndsWith("\\"))
                sysFolder += "\\";

            var svcFileName = sysFolder + "drivers\\etc\\services";

            var lines = File.ReadAllLines(svcFileName);

            var result = new List<ServiceInfo>();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                var info = new ServiceInfo();

                var index = 0;

                // Name
                info.Name = line.Substring(index, 16).Trim();
                index += 16;

                // Port number and type
                var temp = line.Substring(index, 9).Trim();
                var tempSplitted = temp.Split('/');

                info.Port = ushort.Parse(tempSplitted[0]);
                info.Type = tempSplitted[1].ToLower();

                result.Add(info);
            }

            return result;
        }

        public string GetServiceName(string protocol, ushort port)
        {
            string retval = null;
            var svcResult = _serviceList.Where(x => x.Port == port && x.Type.Equals(protocol, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (svcResult != null)
                retval = svcResult.Name;

            return retval;
        }
    }
}
