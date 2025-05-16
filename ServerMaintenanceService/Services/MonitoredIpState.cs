using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomerAndServerMaintenanceTracking.Models;

namespace ServerMaintenanceService.Services
{
    internal class MonitoredIpState
    {
        public MonitoredIpDetail IpDetail { get; set; }
        public DateTime LastPingAttemptTime { get; set; }
        public DateTime NextPingTime { get; set; }
        public bool IsPinging { get; set; } // To prevent re-pinging if already in progress
        public string LastKnownStatus { get; set; } // For outage logging (e.g., "Success", "TimedOut")

        public MonitoredIpState(MonitoredIpDetail detail)
        {
            IpDetail = detail;
            // Initialize NextPingTime to ensure it pings soon after starting
            NextPingTime = DateTime.MinValue;
            IsPinging = false;
            LastKnownStatus = "Pending"; // Initial state before first ping
        }
    }
}
