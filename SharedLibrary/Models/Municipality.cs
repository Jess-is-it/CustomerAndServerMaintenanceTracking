using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class Municipality
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }
        public List<Barangay> Barangays { get; set; } // To hold associated barangays

        public Municipality()
        {
            Barangays = new List<Barangay>();
            DateAdded = DateTime.Now;
        }
    }
}
