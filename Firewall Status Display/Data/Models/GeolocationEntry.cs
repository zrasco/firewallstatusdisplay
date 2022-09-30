using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;

namespace Firewall_Status_Display.Data.Models
{
    [Index(nameof(BeginningIP), nameof(EndingIP))]
    public class GeolocationEntry
    {
        public int Id { get; set; }
        public uint BeginningIP { get; set; }
        public uint EndingIP { get; set; }
        public string EntryType { get; set; } // AS, OS, etc...
        public string Iso2DigitCountryCode { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
