using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Models.ViewModels
{
    public class NotificationRecipientDisplayItem
    {
        public bool IsSelected { get; set; } // For the selection checkbox in the grid
        public string RecipientType { get; set; }
        public string Identifier { get; set; }
        public string Details { get; set; }
        public object OriginalSource { get; set; } // Stores the original ID (int) or string (email/phone)
        public string SourceListKey { get; set; } // e.g., "Role", "User"
    }
}
