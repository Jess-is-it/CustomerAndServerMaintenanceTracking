using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerAndServerMaintenanceTracking.Models
{
    public class TagDisplayModel
    {
        public int Id { get; set; }
        public string TagName { get; set; }
        public string TagDescription { get; set; }
        public string NetworkCluster { get; set; }

        // Add this property to match your DB field
        public bool IsParent { get; set; }
        public string TagType { get; set; }

    }
}
