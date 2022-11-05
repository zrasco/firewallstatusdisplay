using Firewall_Status_Display.Services;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.OData.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
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
        private IDataRepoService _dataRepoService;
        private IGeolocationCache _geolocationCache;
        public StatusViewModel(IDataRepoService dataRepoService,
                                IGeolocationCache geolocationCache) : this()
        {
            _dataRepoService = dataRepoService;
            _geolocationCache = geolocationCache;

            PopulateDBPage();

        }
        public StatusViewModel()
        {

            // Test data for chart
            LogChartItems = new ObservableCollection<LineChartItem>();
            LogChartItems.Add(new LineChartItem("10/2", 50));
            LogChartItems.Add(new LineChartItem("10/3", 60));
            LogChartItems.Add(new LineChartItem("10/4", 70));
            LogChartItems.Add(new LineChartItem("10/5", 40));
            LogChartItems.Add(new LineChartItem("10/6", 50));

            // Test data for pie chart
            GeoPieChartItems = new ObservableCollection<PieChartItem>();
            GeoPieChartItems.Add(new PieChartItem("Thing #1\n10%", 20));
            GeoPieChartItems.Add(new PieChartItem("Thing #2", 30));
            GeoPieChartItems.Add(new PieChartItem("Thing #3", 40));
            GeoPieChartItems.Add(new PieChartItem("Thing #4", 10));


            UpdateDBPageCommand = new DelegateCommand(OnUpdateDBPageCommandExecute);
        }

        private void OnUpdateDBPageCommandExecute(object obj)
        {
            PopulateDBPage();
        }

        private async void PopulateDBPage()
        {
            var items = new ObservableCollection<LineChartItem>();
            var entries = (await _dataRepoService.GetAllFirewallEntriesAsync()).OrderBy(x => x.TimeStamp);
            LineChartItem logChartItem = null;

            foreach (var entry in entries)
            {
                var dateStr = entry.TimeStamp.ToString("M/d");

                if (logChartItem == null || logChartItem.Category != dateStr)
                {
                    if (logChartItem != null)
                        items.Add(logChartItem);
                    logChartItem = new LineChartItem(dateStr);
                }
                else
                    logChartItem.ItemValue++;
            }

            // Add last log chart item
            items.Add(logChartItem);

            // Set information
            FirewallEntryCount = entries.Count();
            FirewallEntryDays = items.Count;
            CacheEntryTotal = _geolocationCache.Entries();
            CacheHits = _geolocationCache.Hits();
            CacheMisses = _geolocationCache.Misses();
            CacheLimit = _geolocationCache.Limit();

            // Set the line chart data
            LogChartItems = items;

            // Set the pie chart data
            GeoPieChartItems = new ObservableCollection<PieChartItem>();
            var geoEntries = (await _dataRepoService.GetAllFirewallEntriesAsync()).GroupBy(x => x.SrcCountryCode).OrderByDescending(x => x.Count()).ToList();

            // Top 10 source countries
            for (int x = 0; x < 10; x++)
            {
                GeoPieChartItems.Add(new PieChartItem(_dataRepoService.GetCountryNameFrom2DigitCode(geoEntries[x].Key), geoEntries[x].Count()));
            }

            // Create labels for pie chart
            var pieChartTotal = GeoPieChartItems.Select(x => x.Value).Sum();
            foreach (var item in GeoPieChartItems)
            {
                item.Label = $"{item.Title}\n{Math.Round((item.Value/pieChartTotal) * 100,2)}%";
            }

            // Set average per day
            AvgPerDay = Convert.ToInt32(LogChartItems.Select(x => x.ItemValue).Average());

        }

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

        public ICommand UpdateDBPageCommand;
    }
}
