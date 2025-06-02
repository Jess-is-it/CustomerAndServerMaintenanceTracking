using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; } // Matched chkEnableSsl
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SenderEmail { get; set; } // 'From' address
        public string SenderDisplayName { get; set; }

        // Default constructor with some common defaults (e.g., for Gmail)
        public EmailSettings()
        {
            SmtpPort = 587;
            EnableSsl = true;
            SenderDisplayName = "CSMT Application"; // Default display name
        }
    }
}
