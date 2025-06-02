using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class Barangay
    {
        public int Id { get; set; }
        public int MunicipalityId { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }

        public Barangay()
        {
            DateAdded = DateTime.Now;
        }
    }
}
