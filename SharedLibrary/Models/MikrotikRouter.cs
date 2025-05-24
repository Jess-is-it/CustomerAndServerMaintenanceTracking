using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class MikrotikRouter
    {
        public int Id { get; set; }
        public string RouterName { get; set; }
        public string HostIPAddress { get; set; }

        // NEW
        public int ApiPort { get; set; }  // for the custom API port

        public string Username { get; set; }
        public string Password { get; set; }
    }

   
}
