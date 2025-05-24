using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class NetwatchConfigDisplay
    {
        public int Id { get; set; } // NetwatchConfig.Id
        public string NetwatchName { get; set; }
        public string SourceType { get; set; } // e.g., "NetworkCluster", "Customer"
        public string TargetSourceName { get; set; } // e.g., Cluster Name, Customer Name
        public string Type { get; set; } // To store the Netwatch type, e.g., ICMP
        public int TargetId { get; set; }
        public List<string> MonitoredTagNames { get; set; } = new List<string>();
        public string MonitoredTagsDisplay => MonitoredTagNames != null ? string.Join(", ", MonitoredTagNames) : string.Empty; // For easy DGV binding
        public int IntervalSeconds { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public bool IsEnabled { get; set; }
        public string LastStatus { get; set; }
        public DateTime? LastChecked { get; set; }
        public bool RunUponSave { get; set; } // You might want to display this too
        public DateTime CreatedDate { get; set; }
    }
}
