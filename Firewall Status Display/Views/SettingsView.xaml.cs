using Firewall_Status_Display.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Diagrams.Core;
using Path = System.IO.Path;

namespace Firewall_Status_Display.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private readonly IGeolocationCache _geolocationCache;
        private readonly UILogger _uiLogger;
        public SettingsView(IGeolocationCache geolocationCache, UILogger uiLogger) : this()
        {
            _geolocationCache = geolocationCache;
            _uiLogger = uiLogger;
        }
        public SettingsView()
        {
            InitializeComponent();
        }

        private void btnOpenGeoSite_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", "/c start https://db-ip.com/db/download/ip-to-city-lite") { CreateNoWindow = true });
        }

        private async void btnImportGeoSite_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to import the DB

            var fileDialog = new OpenFileDialog();

            fileDialog.Filter = @"Compressed db (*.gz)|*.gz|Comma Separated Values (*.csv)|*.csv|All files (*.*)|*.*";

            if (fileDialog.ShowDialog() == true)
            {
                // Get the filename
                var filePath = fileDialog.FileName;
                var fileExt = System.IO.Path.GetExtension(filePath).ToLower();

                // Sanity check
                if (fileExt == ".csv" || fileExt == ".gz")
                {
                    bool tempFile = false;

                    // Proceed
                    try
                    {
                        if (fileExt == ".gz")
                        {
                            // Decompress the file first

                            // Create temp file for CSV
                            var compressedFilePath = filePath;
                            filePath = GetTempFilePathWithExtension("csv");

                            // Decompress the gz file from the website
                            using FileStream compressedFileStream = File.Open(compressedFilePath, FileMode.Open);
                            using FileStream outputFileStream = File.Create(filePath);
                            using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
                            decompressor.CopyTo(outputFileStream);

                            _uiLogger.LogInformation($"Temp file {filePath} created.");
                            tempFile = true;
                        }

                        // Do the actual import with a CSV file
                        await _geolocationCache.ImportGeolocationCSVAsync(filePath, tempFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Unable to import data. Error: {ex.Message}", "Error importing database", MessageBoxButton.OK, MessageBoxImage.Error);
                        _uiLogger.LogError(ex, "Unable to import geolocation data.");
                    }

                }
                else
                {
                    // Invalid extension. Must be CSV or GZ
                    MessageBox.Show("Please select either a csv or gz file", "Error importing database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Path.ChangeExtension(Guid.NewGuid().ToString(), extension);
            return Path.Combine(path, fileName);
        }
    }
}
