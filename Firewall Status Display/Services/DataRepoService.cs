using CsvHelper;
using CsvHelper.Configuration;
using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using MediaFoundation.ReadWrite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.Samples.ObjectDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Core;
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

    public class PortInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class DataRepoService : IDataRepoService
    {
        private readonly FirewallDataContext _fwContext;
        private readonly HttpClient _httpClient;
        private readonly IGeolocationCache _geolocationCache;
        private readonly Dictionary<string, string> _countryList;
        private Queue<string> _entryQueue;
        private readonly Dictionary<string, PortInfo> _portList;
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

            // Create the country list
            _countryList = GetCountryDict();

            // Create the port/services list
            var json = File.ReadAllText("ports.json");
            _portList  = JsonConvert.DeserializeObject<Dictionary<string, PortInfo>>(json);

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
                    var inStr = item.Split("=")[1];

                    retval.In = inStr;

                    /*
                    if (inStr.Contains("wan") || inStr.Contains("vlan"))
                        // Log incoming packets only
                        retval.In = item.Split("=")[1];
                    else
                        // Discard 
                        return null
                    */
                }
                else if (item.Contains("OUT="))
                {
                    var outStr = item.Split("=")[1];

                    if (String.IsNullOrEmpty(outStr))
                        retval.Out = outStr;
                    else
                        // Discard dropped outgoing packets
                        return null;

                    //retval.Out = item.Split("=")[1];
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

        public string GetServiceName(string protocol, ushort port)
        {
            var keyName = $"{port}/{protocol.ToLower()}";

            if (_portList.ContainsKey(keyName))
                return _portList[keyName].Name;
            else
                return null;
        }

        public string GetDBAddress()
        {
            var sqldb = new SqlConnectionStringBuilder(_fwContext.Database.GetConnectionString());

            return sqldb.DataSource.Split(",")[0];
        }

        public int GetDBPort()
        {
            var sqldb = new SqlConnectionStringBuilder(_fwContext.Database.GetConnectionString());
            var arr = sqldb.DataSource.Split(",");

            // Check for non-standard port. Otherwise return 1433
            if (arr.Count() > 1)
                return Int32.Parse(arr[1]);
            else
                return 1433;

        }

        private Dictionary<string, string> GetCountryDict()
        {
            // Source: http://country.io/data/
            return new Dictionary<string, string>()
            {
                { "BD", "Bangladesh" },
                { "BE", "Belgium" },
                { "BF", "Burkina Faso" },
                { "BG", "Bulgaria" },
                { "BA", "Bosnia and Herzegovina" },
                { "BB", "Barbados" },
                { "WF", "Wallis and Futuna" },
                { "BL", "Saint Barthelemy" },
                { "BM", "Bermuda" },
                { "BN", "Brunei" },
                { "BO", "Bolivia" },
                { "BH", "Bahrain" },
                { "BI", "Burundi" },
                { "BJ", "Benin" },
                { "BT", "Bhutan" },
                { "JM", "Jamaica" },
                { "BV", "Bouvet Island" },
                { "BW", "Botswana" },
                { "WS", "Samoa" },
                { "BQ", "Bonaire, Saint Eustatius and Saba " },
                { "BR", "Brazil" },
                { "BS", "Bahamas" },
                { "JE", "Jersey" },
                { "BY", "Belarus" },
                { "BZ", "Belize" },
                { "RU", "Russia" },
                { "RW", "Rwanda" },
                { "RS", "Serbia" },
                { "TL", "East Timor" },
                { "RE", "Reunion" },
                { "TM", "Turkmenistan" },
                { "TJ", "Tajikistan" },
                { "RO", "Romania" },
                { "TK", "Tokelau" },
                { "GW", "Guinea-Bissau" },
                { "GU", "Guam" },
                { "GT", "Guatemala" },
                { "GS", "South Georgia and the South Sandwich Islands" },
                { "GR", "Greece" },
                { "GQ", "Equatorial Guinea" },
                { "GP", "Guadeloupe" },
                { "JP", "Japan" },
                { "GY", "Guyana" },
                { "GG", "Guernsey" },
                { "GF", "French Guiana" },
                { "GE", "Georgia" },
                { "GD", "Grenada" },
                { "GB", "United Kingdom" },
                { "GA", "Gabon" },
                { "SV", "El Salvador" },
                { "GN", "Guinea" },
                { "GM", "Gambia" },
                { "GL", "Greenland" },
                { "GI", "Gibraltar" },
                { "GH", "Ghana" },
                { "OM", "Oman" },
                { "TN", "Tunisia" },
                { "JO", "Jordan" },
                { "HR", "Croatia" },
                { "HT", "Haiti" },
                { "HU", "Hungary" },
                { "HK", "Hong Kong" },
                { "HN", "Honduras" },
                { "HM", "Heard Island and McDonald Islands" },
                { "VE", "Venezuela" },
                { "PR", "Puerto Rico" },
                { "PS", "Palestinian Territory" },
                { "PW", "Palau" },
                { "PT", "Portugal" },
                { "SJ", "Svalbard and Jan Mayen" },
                { "PY", "Paraguay" },
                { "IQ", "Iraq" },
                { "PA", "Panama" },
                { "PF", "French Polynesia" },
                { "PG", "Papua New Guinea" },
                { "PE", "Peru" },
                { "PK", "Pakistan" },
                { "PH", "Philippines" },
                { "PN", "Pitcairn" },
                { "PL", "Poland" },
                { "PM", "Saint Pierre and Miquelon" },
                { "ZM", "Zambia" },
                { "EH", "Western Sahara" },
                { "EE", "Estonia" },
                { "EG", "Egypt" },
                { "ZA", "South Africa" },
                { "EC", "Ecuador" },
                { "IT", "Italy" },
                { "VN", "Vietnam" },
                { "SB", "Solomon Islands" },
                { "ET", "Ethiopia" },
                { "SO", "Somalia" },
                { "ZW", "Zimbabwe" },
                { "SA", "Saudi Arabia" },
                { "ES", "Spain" },
                { "ER", "Eritrea" },
                { "ME", "Montenegro" },
                { "MD", "Moldova" },
                { "MG", "Madagascar" },
                { "MF", "Saint Martin" },
                { "MA", "Morocco" },
                { "MC", "Monaco" },
                { "UZ", "Uzbekistan" },
                { "MM", "Myanmar" },
                { "ML", "Mali" },
                { "MO", "Macao" },
                { "MN", "Mongolia" },
                { "MH", "Marshall Islands" },
                { "MK", "Macedonia" },
                { "MU", "Mauritius" },
                { "MT", "Malta" },
                { "MW", "Malawi" },
                { "MV", "Maldives" },
                { "MQ", "Martinique" },
                { "MP", "Northern Mariana Islands" },
                { "MS", "Montserrat" },
                { "MR", "Mauritania" },
                { "IM", "Isle of Man" },
                { "UG", "Uganda" },
                { "TZ", "Tanzania" },
                { "MY", "Malaysia" },
                { "MX", "Mexico" },
                { "IL", "Israel" },
                { "FR", "France" },
                { "IO", "British Indian Ocean Territory" },
                { "SH", "Saint Helena" },
                { "FI", "Finland" },
                { "FJ", "Fiji" },
                { "FK", "Falkland Islands" },
                { "FM", "Micronesia" },
                { "FO", "Faroe Islands" },
                { "NI", "Nicaragua" },
                { "NL", "Netherlands" },
                { "NO", "Norway" },
                { "NA", "Namibia" },
                { "VU", "Vanuatu" },
                { "NC", "New Caledonia" },
                { "NE", "Niger" },
                { "NF", "Norfolk Island" },
                { "NG", "Nigeria" },
                { "NZ", "New Zealand" },
                { "NP", "Nepal" },
                { "NR", "Nauru" },
                { "NU", "Niue" },
                { "CK", "Cook Islands" },
                { "XK", "Kosovo" },
                { "CI", "Ivory Coast" },
                { "CH", "Switzerland" },
                { "CO", "Colombia" },
                { "CN", "China" },
                { "CM", "Cameroon" },
                { "CL", "Chile" },
                { "CC", "Cocos Islands" },
                { "CA", "Canada" },
                { "CG", "Republic of the Congo" },
                { "CF", "Central African Republic" },
                { "CD", "Democratic Republic of the Congo" },
                { "CZ", "Czech Republic" },
                { "CY", "Cyprus" },
                { "CX", "Christmas Island" },
                { "CR", "Costa Rica" },
                { "CW", "Curacao" },
                { "CV", "Cape Verde" },
                { "CU", "Cuba" },
                { "SZ", "Swaziland" },
                { "SY", "Syria" },
                { "SX", "Sint Maarten" },
                { "KG", "Kyrgyzstan" },
                { "KE", "Kenya" },
                { "SS", "South Sudan" },
                { "SR", "Suriname" },
                { "KI", "Kiribati" },
                { "KH", "Cambodia" },
                { "KN", "Saint Kitts and Nevis" },
                { "KM", "Comoros" },
                { "ST", "Sao Tome and Principe" },
                { "SK", "Slovakia" },
                { "KR", "South Korea" },
                { "SI", "Slovenia" },
                { "KP", "North Korea" },
                { "KW", "Kuwait" },
                { "SN", "Senegal" },
                { "SM", "San Marino" },
                { "SL", "Sierra Leone" },
                { "SC", "Seychelles" },
                { "KZ", "Kazakhstan" },
                { "KY", "Cayman Islands" },
                { "SG", "Singapore" },
                { "SE", "Sweden" },
                { "SD", "Sudan" },
                { "DO", "Dominican Republic" },
                { "DM", "Dominica" },
                { "DJ", "Djibouti" },
                { "DK", "Denmark" },
                { "VG", "British Virgin Islands" },
                { "DE", "Germany" },
                { "YE", "Yemen" },
                { "DZ", "Algeria" },
                { "US", "United States" },
                { "UY", "Uruguay" },
                { "YT", "Mayotte" },
                { "UM", "United States Minor Outlying Islands" },
                { "LB", "Lebanon" },
                { "LC", "Saint Lucia" },
                { "LA", "Laos" },
                { "TV", "Tuvalu" },
                { "TW", "Taiwan" },
                { "TT", "Trinidad and Tobago" },
                { "TR", "Turkey" },
                { "LK", "Sri Lanka" },
                { "LI", "Liechtenstein" },
                { "LV", "Latvia" },
                { "TO", "Tonga" },
                { "LT", "Lithuania" },
                { "LU", "Luxembourg" },
                { "LR", "Liberia" },
                { "LS", "Lesotho" },
                { "TH", "Thailand" },
                { "TF", "French Southern Territories" },
                { "TG", "Togo" },
                { "TD", "Chad" },
                { "TC", "Turks and Caicos Islands" },
                { "LY", "Libya" },
                { "VA", "Vatican" },
                { "VC", "Saint Vincent and the Grenadines" },
                { "AE", "United Arab Emirates" },
                { "AD", "Andorra" },
                { "AG", "Antigua and Barbuda" },
                { "AF", "Afghanistan" },
                { "AI", "Anguilla" },
                { "VI", "U.S. Virgin Islands" },
                { "IS", "Iceland" },
                { "IR", "Iran" },
                { "AM", "Armenia" },
                { "AL", "Albania" },
                { "AO", "Angola" },
                { "AQ", "Antarctica" },
                { "AS", "American Samoa" },
                { "AR", "Argentina" },
                { "AU", "Australia" },
                { "AT", "Austria" },
                { "AW", "Aruba" },
                { "IN", "India" },
                { "AX", "Aland Islands" },
                { "AZ", "Azerbaijan" },
                { "IE", "Ireland" },
                { "ID", "Indonesia" },
                { "UA", "Ukraine" },
                { "QA", "Qatar" },
                { "MZ", "Mozambique" }
            };
        }
    }
}
