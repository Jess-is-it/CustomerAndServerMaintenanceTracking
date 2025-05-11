using System;

namespace CustomerAndServerMaintenanceTracking.Models
{
    public class TagAssignment
    {
        public int ParentTagId { get; set; }
        public int ChildTagId { get; set; }
    }
}
