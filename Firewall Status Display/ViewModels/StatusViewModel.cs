using Firewall_Status_Display.Data.Models;
using Firewall_Status_Display.Services;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.OData.Client;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Telerik.Windows.Controls;
using Telerik.Windows.Documents.Spreadsheet.Model.DataValidation;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class TrafficByCountryRowItem
    {
        public TrafficByCountryRowItem(string country = "", int packets = 0, string percentage = "0.0%")
        {
            Country = country;
            Packets = packets;
            Percentage = percentage;
        }
        public string Country { get; set; }
        public int Packets { get; set; }
        public string Percentage { get; set; }
    }
    public class TrafficByPortRowItem
    {
        public TrafficByPortRowItem(string serviceName = "unknown", int port = 0, int packets = 0, string percentage = "0.0%")
        {
            ServiceName = serviceName;
            Port = port;
            Packets = packets;
            Percentage = percentage;
        }

        public string ServiceName { get; set; }
        public int Port { get; set; }
        public int Packets { get; set; }
        public string Percentage { get; set; }
    }
    public class PortScanRowItem
    {
        public DateTime TimeStamp { get; set; }
        [Display(Name = "Src IP")]
        public string IPSrc { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public int Packets { get; set; }
    }

    public class PieChartItem : INotifyPropertyChanged
    {
        public PieChartItem(string title = null, double amount = 0.0)
        {
            Title = title;
            Value = amount;
        }

        private string title;
        private string label;
        private double itemValue;

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Title
        {
            get => title;
            set
            {
                title = value;
                NotifyPropertyChanged();
            }
                
        }
        public string Label
        {
            get => label;
            set
            {
                label = value;
                NotifyPropertyChanged();
            }
        }
        public double Value
        {
            get => itemValue;
            set
            {
                itemValue = value;
                NotifyPropertyChanged();
            }
        }
    }
    public class LineChartItem
    {
        public LineChartItem(string category = null, int value = 0)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            ItemValue = value;
        }

        private string category;
        private int itemValue;

        public string Category
        {
            get => category;
            set => category = value;
        }
        public int ItemValue
        {
            get => itemValue;
            set => itemValue = value;
        }
    }
    public class StatusViewModel : ViewModelBase
    {
        private readonly IDataRepoService _dataRepoService;
        private readonly IGeolocationCache _geolocationCache;
        private readonly IConfiguration _config;
        private readonly UILogger _uilogger;
        private bool _init;

        public StatusViewModel(IDataRepoService dataRepoService,
                                IGeolocationCache geolocationCache,
                                IConfiguration config,
                                IServiceProvider services,
                                UILogger uilogger) : this()
        {
            _dataRepoService = dataRepoService;
            _geolocationCache = geolocationCache;
            _config = config;
            _uilogger = uilogger;

            _init = true;

            PopulateDBPage();
            
        }
        public StatusViewModel()
        {

            // Test data for firewall chart
            LogChartItems = new ObservableCollection<LineChartItem>();
            LogChartItems.Add(new LineChartItem("10/2", 50));
            LogChartItems.Add(new LineChartItem("10/3", 60));
            LogChartItems.Add(new LineChartItem("10/4", 70));
            LogChartItems.Add(new LineChartItem("10/5", 40));
            LogChartItems.Add(new LineChartItem("10/6", 50));

            // Test data for country traffic pie chart
            GeoPieChartItems = new ObservableCollection<PieChartItem>();
            GeoPieChartItems.Add(new PieChartItem("Thing #1\n10%", 20));
            GeoPieChartItems.Add(new PieChartItem("Thing #2", 30));
            GeoPieChartItems.Add(new PieChartItem("Thing #3", 40));
            GeoPieChartItems.Add(new PieChartItem("Thing #4", 10));

            // Test data for port traffic pie chart
            PortPieChartItems = new ObservableCollection<PieChartItem>();
            PortPieChartItems.Add(new PieChartItem("22\n20%", 20));
            PortPieChartItems.Add(new PieChartItem("33\n20%", 20));
            PortPieChartItems.Add(new PieChartItem("44\n20%", 20));
            PortPieChartItems.Add(new PieChartItem("55\n20%", 20));
            PortPieChartItems.Add(new PieChartItem("66\n20%", 20));

            // Test data for port traffic ranking chart
            TrafficByPortListItems = new ObservableCollection<TrafficByPortRowItem>();
            TrafficByPortListItems.Add(new TrafficByPortRowItem("http", 80, 500, "20%"));
            TrafficByPortListItems.Add(new TrafficByPortRowItem("ftp", 80, 500, "20%"));
            TrafficByPortListItems.Add(new TrafficByPortRowItem("irc", 80, 500, "20%"));
            TrafficByPortListItems.Add(new TrafficByPortRowItem("dns", 80, 500, "20%"));
            TrafficByPortListItems.Add(new TrafficByPortRowItem("https", 80, 500, "20%"));

            // Test data for country traffic ranking chart
            TrafficByCountryListItems = new ObservableCollection<TrafficByCountryRowItem>();
            TrafficByCountryListItems.Add(new TrafficByCountryRowItem("USA", 500, "20%"));
            TrafficByCountryListItems.Add(new TrafficByCountryRowItem("Canada", 500, "20%"));
            TrafficByCountryListItems.Add(new TrafficByCountryRowItem("Mexico", 500, "20%"));
            TrafficByCountryListItems.Add(new TrafficByCountryRowItem("Brazil", 500, "20%"));
            TrafficByCountryListItems.Add(new TrafficByCountryRowItem("Russia", 500, "20%"));

            // Test data for port scan list
            PortScanEntryList = new ObservableCollection<PortScanRowItem>();
            PortScanChartItems = new ObservableCollection<LineChartItem>();

            for (int x = -10; x < 0; x++)
            {
                PortScanEntryList.Add(new PortScanRowItem() { IPSrc = "1.2.3.4", TimeStamp = DateTime.Now.AddDays(x) });
                PortScanChartItems.Add(new LineChartItem(DateTime.Now.AddDays(x).ToString("M/d"), 1));
            }


            UpdateDBPageCommand = new DelegateCommand(OnUpdateDBPageCommandExecute);
            AddPortScanItemCommand = new DelegateCommand(OnAddPortScanItemCommandExecute);
            ClearAllPortScanItemsCommand = new DelegateCommand(OnClearAllPortScanItemsCommandExecute);
        }

        private void OnClearAllPortScanItemsCommandExecute(object obj)
        {
            throw new NotImplementedException();
        }

        private void OnAddPortScanItemCommandExecute(object obj)
        {
            throw new NotImplementedException();
        }

        private void OnUpdateDBPageCommandExecute(object obj)
        {
            PopulateDBPage();
        }


        private async void PopulateDBPage()
        {
            var items = new ObservableCollection<LineChartItem>();
            var entriesUnordered = await _dataRepoService.GetAllFirewallEntriesAsync();
            var entriesByTime = entriesUnordered.OrderBy(x => x.TimeStamp);
            LineChartItem chartItem = null;

            // Add firewall packet log chart data
            foreach (var entry in entriesByTime)
            {
                var dateStr = entry.TimeStamp.ToString("M/d");

                if (chartItem == null || chartItem.Category != dateStr)
                {
                    if (chartItem != null)
                        items.Add(chartItem);
                    chartItem = new LineChartItem(dateStr);
                    chartItem.ItemValue++;
                }
                else
                    chartItem.ItemValue++;
            }

            // Add last log chart item
            items.Add(chartItem);

            // Set the line chart data
            LogChartItems = items;

            // Add port scan chart data
            var thresholdPkts = _config.GetValue<int>("Options:PortScanning:ThresholdPkts");
            var thresholdSecs = _config.GetValue<int>("Options:PortScanning:ThresholdSecs");

            var candidateEntries = entriesByTime.GroupBy(x => x.IPSrc).Where(y => y.Count() > thresholdPkts);
            var deleteMe = entriesByTime.Where(x => x.IPSrc == "192.241.153.165");

            var portScanHits = new[] {
                new  { IP = "", TimeStamp = DateTime.Now, DestPort = 0, Proto = "" }
            }.ToList();
            portScanHits.Clear();

            // First pass. Get our rough groups
            foreach (var entry in candidateEntries)
            {
                var entryList = entry.ToList();

                for (int x = 0; x < entryList.Count() - thresholdPkts; x++)
                {


                    if (entryList[x].TimeStamp.AddSeconds(thresholdSecs) >= entryList[x + thresholdPkts].TimeStamp)
                    {
                        portScanHits.Add(new {
                            IP = entryList[x].IPSrc, 
                            TimeStamp = entryList[x].TimeStamp,
                            DestPort = entryList[x].PortDest,
                            Proto = entryList[x].Proto
                        });
                    }
                }
            }

            // Second pass. Group & see if this is actually a port scan
            var groupedScanReviewList = portScanHits.GroupBy(x => x.IP);
            var lineItems = new List<PortScanRowItem>();
            
            foreach (var entry in groupedScanReviewList)
            {
                var entryList = entry.ToList();
                var first = entryList.First();
                var last = entryList.Last();

                var suspectRange = entriesByTime.Where(x => x.IPSrc == entry.Key && x.TimeStamp >= first.TimeStamp && x.TimeStamp <= last.TimeStamp.AddSeconds(thresholdSecs)).ToList();

                // Get distinct # of ports/protocols in this set

                // Group by Time groups
                int endRangeMarker = 0;
                var groups = new List<List<FirewallEntry>>();
                for (int x = 0; x < suspectRange.Count - 1; x++)
                {
                    if (suspectRange[x + 1].TimeStamp >= suspectRange[x].TimeStamp.AddSeconds(thresholdSecs))
                    {
                        // End of this group
                        groups.Add(suspectRange.GetRange(endRangeMarker, (x + 1) - endRangeMarker));
                        endRangeMarker = x + 1;
                    }
                    else if (x == suspectRange.Count - 2)
                    {
                        // Add final grouping
                        groups.Add(suspectRange.GetRange(endRangeMarker, suspectRange.Count - endRangeMarker));
                    }
                }

                // Only take groups over the threshold
                groups = groups.Where(x => x.Count >= thresholdPkts).ToList();

                foreach (var group in groups)
                {
                    var distinctEntries = group.Select(x => new { x.Proto, x.PortDest }).Distinct().Count();

                    if (distinctEntries >= 7)
                    {
                        // Most likely a port scan

                        // We may have multiple scans per IP
                        lineItems.Add(new PortScanRowItem() {
                            IPSrc = entry.Key,
                            TimeStamp = group[0].TimeStamp,
                            Country = _dataRepoService.GetCountryNameFrom2DigitCode(group[0].SrcCountryCode),
                            Region = group[0].SrcRegion,
                            Packets = group.Count()
                        });
                    }
                }
            }

            // Set the final list
            var newPortScanEntryList = new ObservableCollection<PortScanRowItem>(lineItems.OrderByDescending(x => x.TimeStamp));

            if (newPortScanEntryList.Count > PortScanEntryList.Count)
            {
                // Don't show the alert during the init/first refresh
                if (_init == true)
                {
                    _init = false;
                }
                else
                {
                    // We have a new port scan. Send notification
                    var notificationIPEntries = newPortScanEntryList.ToList().GetRange(0, newPortScanEntryList.Count - PortScanEntryList.Count);

                    string notification = "Detected port scan from the following IPs:\n";

                    foreach (var notifyentry in notificationIPEntries)
                    {
                        notification += $"{notifyentry.IPSrc}\n";
                    }

                    // Alert user.
                    _uilogger.ShowTrayNotification(notification);
                }
            }

            // Set display list of port scans to new list
            PortScanEntryList = newPortScanEntryList;

            // Update port scan chart
            chartItem = null;
            items = new ObservableCollection<LineChartItem>();
            foreach (var entry in PortScanEntryList.OrderBy(x => x.TimeStamp))
            {
                var dateStr = entry.TimeStamp.ToString("M/d");

                if (chartItem == null || chartItem.Category != dateStr)
                {
                    if (chartItem != null)
                        items.Add(chartItem);
                    chartItem = new LineChartItem(dateStr);
                    chartItem.ItemValue++;
                }
                else
                    chartItem.ItemValue++;
            }

            // Add last port scan chart item
            items.Add(chartItem);

            PortScanChartItems = items;

            // Set stat information
            FirewallEntryCount = entriesByTime.Count();
            FirewallEntryDays = LogChartItems.Count;
            CacheEntryTotal = _geolocationCache.Entries();
            CacheHits = _geolocationCache.Hits();
            CacheMisses = _geolocationCache.Misses();
            CacheLimit = _geolocationCache.Limit();

            PortScanTotal = PortScanEntryList.Count;
            PortScanTotalPackets = PortScanEntryList.Sum(x => x.Packets);
            PortScanUniqueIPs = PortScanEntryList?.Select(x => x.IPSrc)?.Distinct()?.Count() ?? 0;

            // Traffic by country tab
            TrafficByCountryListItems = new ObservableCollection<TrafficByCountryRowItem>();

            // Geolocation pie chart
            GeoPieChartItems = new ObservableCollection<PieChartItem>();
            var geoEntries = entriesUnordered.GroupBy(x => x.SrcCountryCode).OrderByDescending(x => x.Count()).ToList();

            for (int x = 0; x < geoEntries.Count; x++)
            {
                if (x < 10)
                    // Top 10 source countries
                    GeoPieChartItems.Add(new PieChartItem(_dataRepoService.GetCountryNameFrom2DigitCode(geoEntries[x].Key), geoEntries[x].Count()));

                // All entries go into country datagrid
                TrafficByCountryListItems.Add(new TrafficByCountryRowItem(
                    _dataRepoService.GetCountryNameFrom2DigitCode(geoEntries[x].Key),
                    geoEntries[x].Count(),
                    $"{Math.Round(((double)geoEntries[x].Count() / entriesUnordered.Count) * 100, 2)}%"));
            }

            // Traffic by port tab
            TrafficByPortListItems = new ObservableCollection<TrafficByPortRowItem>();

            // Port pie chart
            PortPieChartItems = new ObservableCollection<PieChartItem>();
            var portEntries = entriesUnordered
                .Where(x => x.PortDest > 0)
                .GroupBy(x => new { x.PortDest, x.Proto })
                .OrderByDescending(x => x.Count()).ToList();

            // Top 10 destination ports
            for (int x = 0; x < 100; x++)
            {
                var grp = portEntries[x].Key;
                int port = grp.PortDest;
                string proto = grp.Proto;
                string title;

                // Get the name of our service
                string svcName = _dataRepoService.GetServiceName(grp.Proto, (ushort)port);

                if (svcName == null)
                    title = $"{grp.Proto}/{port.ToString()}";
                else
                    title = svcName;

                if (x < 10)
                    PortPieChartItems.Add(new PieChartItem(title, portEntries[x].Count()));

                TrafficByPortListItems.Add(new TrafficByPortRowItem(svcName, port, portEntries[x].Count(),
                    $"{Math.Round(((double)portEntries[x].Count() / entriesUnordered.Count) * 100, 2)}%"));
            }

            // Create labels for pie charts

            // Geolocation pie chart
            var pieChartTotal = GeoPieChartItems.Select(x => x.Value).Sum();
            foreach (var item in GeoPieChartItems)
            {
                item.Label = $"{item.Title}\n{Math.Round((item.Value/ entriesUnordered.Count) * 100,2)}%";
            }

            // Port pie chart
            var portPieChartTotal = PortPieChartItems.Select(x => x.Value).Sum();
            foreach (var item in PortPieChartItems)
            {
                item.Label = $"{item.Title}\n{Math.Round((item.Value / entriesUnordered.Count) * 100, 2)}%";
            }

            // Get our traffic amount
            var amt = entriesUnordered.Sum(x => x.Length);
            var mb = (amt / 1024d) / 1024d;
            DroppedTraffic = mb.ToString("0.00MB");

            // Set average per day
            AvgPerDay = Convert.ToInt32(LogChartItems.Select(x => x.ItemValue).Average());

        }

        #region Variables
        private int avgPerDay;

        /// <summary>
        /// Average firewall entries per day
        /// </summary>
        public int AvgPerDay
        {
            get { return avgPerDay; }
            set
            {
                avgPerDay = value;
                RaisePropertyChanged();
            }
        }

        private int firewallEntryDays;
        /// <summary>
        /// # of days with firewall entries
        /// </summary>
        public int FirewallEntryDays
        {
            get { return firewallEntryDays; }
            set
            {
                firewallEntryDays = value;
                RaisePropertyChanged();
            }
        }

        private int firewallEntryCount;
        /// <summary>
        /// Number of firewall entries
        /// </summary>
        public int FirewallEntryCount
        {
            get { return firewallEntryCount; }
            set
            {
                firewallEntryCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Total number of misses in the cache
        /// </summary>
        private int cacheMisses;
        public int CacheMisses
        {
            get { return cacheMisses; }
            set
            {
                cacheMisses = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Total number of hits in the cache
        /// </summary>
        private int cacheHits;
        public int CacheHits
        {
            get { return cacheHits; }
            set
            {
                cacheHits = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Total number of entries in the cache
        /// </summary>
        private int cacheEntryTotal;
        public int CacheEntryTotal
        {
            get { return cacheEntryTotal; }
            set
            {
                cacheEntryTotal = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Limit of the cache
        /// </summary>
        private int cacheLimit;
        public int CacheLimit
        {
            get { return cacheLimit; }
            set
            {
                cacheLimit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Dropped traffic amount
        /// </summary>
        private string droppedTraffic;

        public string DroppedTraffic
        {
            get { return droppedTraffic; }
            set
            {
                droppedTraffic = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Total number of Port scans
        /// </summary>
        private int portScanTotal;

        public int PortScanTotal
        {
            get { return portScanTotal; }
            set
            {
                portScanTotal = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Unique IPs amongst all port scans
        /// </summary>
        private int portScanUniqueIPs;

        public int PortScanUniqueIPs
        {
            get { return portScanUniqueIPs; }
            set
            {
                portScanUniqueIPs = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Total number of port scan traffic packets
        /// </summary>
        private int portScanTotalPackets;
        public int PortScanTotalPackets
        {
            get { return portScanTotalPackets; }
            set
            {
                portScanTotalPackets = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of traffic by port
        /// </summary>
        private ObservableCollection<TrafficByPortRowItem> trafficByPortListItems;

        public ObservableCollection<TrafficByPortRowItem> TrafficByPortListItems
        {
            get => trafficByPortListItems;
            set
            {
                trafficByPortListItems = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of traffic by country
        /// </summary>
        private ObservableCollection<TrafficByCountryRowItem> trafficByCountryListItems;
        public ObservableCollection<TrafficByCountryRowItem> TrafficByCountryListItems
        {
            get => trafficByCountryListItems;
            set
            {
                trafficByCountryListItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<LineChartItem> logChartItems;

        /// <summary>
        /// Firewall chart data
        /// </summary>
        public ObservableCollection<LineChartItem> LogChartItems
        {
            get => logChartItems;
            set
            {
                logChartItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<LineChartItem> portScanChartItems;

        /// <summary>
        /// Firewall chart data
        /// </summary>
        public ObservableCollection<LineChartItem> PortScanChartItems
        {
            get => portScanChartItems;
            set
            {
                portScanChartItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<PieChartItem> geoPieChartItems;

        /// <summary>
        /// Firewall chart data
        /// </summary>
        public ObservableCollection<PieChartItem> GeoPieChartItems
        {
            get => geoPieChartItems;
            set
            {
                geoPieChartItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<PieChartItem> portPieChartItems;

        /// <summary>
        /// Firewall chart data
        /// </summary>
        public ObservableCollection<PieChartItem> PortPieChartItems
        {
            get => portPieChartItems;
            set
            {
                portPieChartItems = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<PortScanRowItem> portScanEntryList { get; set; }

        public ObservableCollection<PortScanRowItem> PortScanEntryList
        {
            get { return portScanEntryList; }
            set { portScanEntryList = value; RaisePropertyChanged(); }
        }

        #endregion
        #region ICommands
        public ICommand AddPortScanItemCommand { get; set; }
        public ICommand ClearAllPortScanItemsCommand { get; set; }
        public ICommand UpdateDBPageCommand { get; set; }
        #endregion
    }
}
