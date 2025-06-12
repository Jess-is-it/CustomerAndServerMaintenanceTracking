using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class EmailSettings
    {
        public int Id { get; set; }
        public string SettingName { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderDisplayName { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool EnableSsl { get; set; }
        public bool IsDefault { get; set; }
    }
}
