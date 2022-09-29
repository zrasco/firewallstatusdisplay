using Firewall_Status_Display.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firewall_Status_Display.Data.Contexts
{
    public class FirewallDataContext : DbContext
    {
        private readonly string _connectionString;
        public FirewallDataContext(IConfiguration config, DbContextOptions options) : base(options)
        {
            _connectionString = config.GetConnectionString("DefaultConnectionString");
        }

        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }
        
        public DbSet<FirewallEntry> FirewallEntries { get; set; }
    }
}
