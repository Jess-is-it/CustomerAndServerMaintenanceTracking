using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Models
{
    public class DeviceIP
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }
        public string IPAddress { get; set; }
        public string Location { get; set; }
    }
}
