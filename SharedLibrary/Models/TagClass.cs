using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class TagClass
    {
        public int Id { get; set; }
        public string TagName { get; set; }
        public string TagDescription { get; set; }
        // NEW: Flag to indicate if this tag is intended as a parent (for network hierarchy)
        public bool IsParent { get; set; }
        public int AssignedCustomerCount { get; set; }
        public string TagType { get; set; }
    }
}
