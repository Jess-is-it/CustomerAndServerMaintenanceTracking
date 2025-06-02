using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public string PermissionKey { get; set; } // e.g., "CUSTOMER_VIEW_ACTIVE"

        // Navigation properties (optional)
        public virtual UserRole Role { get; set; }
    }
}
