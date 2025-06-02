using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class UserRole
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }

        // Navigation property for permissions
        public virtual ICollection<RolePermission> Permissions { get; set; }
        // For easier use after loading:
        // [NotMapped] // If using EF Core and don't want to map this directly
        public List<string> PermissionKeys { get; set; }


        public UserRole()
        {
            DateCreated = DateTime.Now;
            Permissions = new HashSet<RolePermission>();
            PermissionKeys = new List<string>();
        }
    }
}
