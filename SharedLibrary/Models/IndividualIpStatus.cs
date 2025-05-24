using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class IndividualIpStatus
    {
        public string EntityName { get; set; } // Customer Name or Device Name
        public string IpAddress { get; set; }
        public string LastPingStatus { get; set; } // e.g., "Success", "TimedOut", "HostUnreachable"
        public long? RoundtripTimeMs { get; set; } // Nullable for non-success
        public DateTime LastPingAttemptDateTime { get; set; }

        // Read-only property for display in DataGridView if RTT is null
        public string RoundtripTimeDisplay
        {
            get
            {
                return RoundtripTimeMs.HasValue ? RoundtripTimeMs.Value.ToString() + " ms" : "N/A";
            }
        }
    }
}
