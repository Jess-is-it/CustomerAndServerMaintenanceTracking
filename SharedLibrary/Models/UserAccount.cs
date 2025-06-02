using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; } // Store hashed passwords, never plain text
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string DeactivationReason { get; set; }

        // Navigation property (optional, for convenience)
        public virtual UserRole Role { get; set; }

        public UserAccount()
        {
            IsActive = true;
            DateCreated = DateTime.Now;
        }
    }
}
