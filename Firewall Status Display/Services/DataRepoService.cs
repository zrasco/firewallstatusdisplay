using Firewall_Status_Display.Data.Contexts;
using Firewall_Status_Display.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Printing;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Diagrams.Core;

namespace Firewall_Status_Display.Services
{
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

        private readonly FirewallDataContext _context;
        private readonly HttpClient _httpClient;
        public DataRepoService(FirewallDataContext context,
                                HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;

            // Make sure DB is created
            var created = context.Database.EnsureCreated();

            // Saving changes event to add country codes
            context.SavingChanges += Context_SavingChanges;

            _httpClient.BaseAddress = new Uri("http://ip-api.com/batch");
        }

        private void Context_SavingChanges(object sender, SavingChangesEventArgs e)
        {
            var changedEntries = _context.ChangeTracker.Entries<FirewallEntry>();
            var count = changedEntries.Count();
            var cancelToken = new CancellationToken();

            for (int x = 0; x < count; x += ENTRIES_BEFORE_GEOLOCATION)
            {
                // Grab 100 (or less) at a time
                var srcIPList = changedEntries.ToList().GetRange(x, ENTRIES_BEFORE_GEOLOCATION).Select(p => new IpAPIRequestWrapper()
                {
                    Entity = p.Entity,
                    Request = new IpAPIRequest()
                    {
                        query = p.Entity.IPSrc,
                        lang = "en",
                        fields = "status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,query"
                    }
                }).ToList();

                var dstIPList = changedEntries.ToList().GetRange(x, ENTRIES_BEFORE_GEOLOCATION).Select(p => new IpAPIRequestWrapper()
                {
                    Entity = p.Entity,
                    Request = new IpAPIRequest()
                    {
                        query = p.Entity.IPDest,
                        lang = "en",
                        fields = "status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,query"
                    }
                }).ToList();

                // Combine the two lists into one request
                var ipList = srcIPList.Clone();
                ipList.AddRange(dstIPList);

                var dstJson = JsonConvert.SerializeObject(ipList.Select(x => x.Request));

                // Send request for IPs
                var response = _httpClient.PostAsJsonAsync(new Uri("http://ip-api.com/batch"), ipList, cancelToken).Result;
                var content = response.Content.ReadFromJsonAsync<List<IpAPIResponse>>().Result;
                var strContent = response.Content.ReadAsStringAsync().Result;
                var mine = response;

                //var dstIPList = changedEntries.ToList().GetRange(x, ENTRIES_BEFORE_GEOLOCATION).Select(p => p.Entity.IPDest).ToList();
            }
        }

        public async Task<bool> AddFirewallEntry(string rawLogLine)
        {
            try
            {
                // Check if this is a firewall line. If not, skip
                var fwEntry = ParseIntoFirewallEntry(rawLogLine);

                if (fwEntry != null)
                {
                    _context.FirewallEntries.Add(fwEntry);

                    if (_context.ChangeTracker.Entries().Count() >= ENTRIES_BEFORE_GEOLOCATION)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                //await _context.SaveChangesAsync();
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
                return retval;
        }
    }
}
