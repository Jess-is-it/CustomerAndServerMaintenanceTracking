using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class MonitoredIpDetail
    {
        public string IpAddress { get; set; }
        public string EntityName { get; set; } // Customer Name or Device Name
        public string EntityType { get; set; } // "Customer" or "DeviceIP"
    }
}
