using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{// Enum to represent the source type in a strongly-typed way
    public enum NetwatchSourceType
    {
        NetworkCluster,
        Customer,
        DeviceIP
        // Add other types here if needed in the future
    }

    public class NetwatchConfig
    {
        public int Id { get; set; }
        public string NetwatchName { get; set; }
        public string Type { get; set; }
        public int IntervalSeconds { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public NetwatchSourceType SourceType { get; set; }
        public int TargetId { get; set; } // e.g., NetworkClusterId
        public bool IsEnabled { get; set; }
        public bool RunUponSave { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastChecked { get; set; }
        public string LastStatus { get; set; }
        public List<int> MonitoredTagIds { get; set; } = new List<int>();

        // Constructor to set defaults
        public NetwatchConfig()
        {
            Type = "ICMP";
            IsEnabled = true;
            CreatedDate = DateTime.Now;
        }
    }
}
