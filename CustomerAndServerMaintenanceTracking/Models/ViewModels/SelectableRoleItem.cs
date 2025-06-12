using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.Models.ViewModels
{
    public class SelectableRoleItem
    {
        public UserRole Role { get; }
        public bool IsSelected { get; set; }
        public int Id => Role.Id;
        public string Name => Role.RoleName;
        public string Description => Role.Description;
        public SelectableRoleItem(UserRole role, bool isSelected) { Role = role; IsSelected = isSelected; }

    }
}
