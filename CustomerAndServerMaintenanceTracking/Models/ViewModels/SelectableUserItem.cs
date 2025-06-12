using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace CustomerAndServerMaintenanceTracking.Models.ViewModels
{
    public class SelectableUserItem
    {
        public UserAccount User { get; }
        public bool IsSelected { get; set; }
        public int Id => User.Id;
        public string FullName => User.FullName;
        public string Username => User.Username;
        public string RoleName => User.Role?.RoleName ?? "N/A";
        public SelectableUserItem(UserAccount user, bool isSelected) { User = user; IsSelected = isSelected; }

    }
}
