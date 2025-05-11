using System;

namespace CustomerAndServerMaintenanceTracking.Models
{
    // This model represents the many-to-many relationship between Tags and Hierarchy Groups.
    public class TagHierarchyMapping
    {
        public int TagId { get; set; }
        public int TagHierarchyGroupId { get; set; }
    }
}
