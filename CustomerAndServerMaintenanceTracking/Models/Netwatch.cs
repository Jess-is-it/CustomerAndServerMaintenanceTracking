using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Models
{
    public class Netwatch
    {
        public int Id { get; set; }
        public string PingName { get; set; }
        public string TargetIP { get; set; }
        public int PingIntervalMs { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }

        // NEW: TagId to indicate which tag this ping task belongs to
        public int TagId { get; set; }
    }
}
