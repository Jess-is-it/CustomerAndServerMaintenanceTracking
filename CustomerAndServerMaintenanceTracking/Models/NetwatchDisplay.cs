using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Models
{
    public class NetwatchDisplay
    {
        public string TagName { get; set; }
        public string Entity { get; set; }
        public int RtoEntitiesToday { get; set; }
        public string Status { get; set; }
    }

}
